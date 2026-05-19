using System.Globalization;
using App.BLL.Contracts.Services;
using App.BLL.Contracts.Services.Client;
using App.Domain;
using App.Domain.Enums;
using App.DTO.v1.Bookings;
using WebApp.Models;

namespace WebApp.Areas.Client.Services;

public enum ClientSessionsPageStatus
{
    Success,
    MissingGymContext,
    Forbidden
}

public sealed record ClientSessionsPageResult<TViewModel>(
    ClientSessionsPageStatus Status,
    TViewModel? ViewModel)
{
    public static ClientSessionsPageResult<TViewModel> Success(TViewModel viewModel) =>
        new(ClientSessionsPageStatus.Success, viewModel);

    public static ClientSessionsPageResult<TViewModel> MissingGymContext { get; } =
        new(ClientSessionsPageStatus.MissingGymContext, default);

    public static ClientSessionsPageResult<TViewModel> Forbidden { get; } =
        new(ClientSessionsPageStatus.Forbidden, default);
}

public enum ClientSessionCommandStatus
{
    Success,
    MissingGymContext,
    MemberRequired,
    Failed
}

public sealed record ClientSessionCommandResult(
    ClientSessionCommandStatus Status,
    string? Message)
{
    public static ClientSessionCommandResult Success(string message) =>
        new(ClientSessionCommandStatus.Success, message);

    public static ClientSessionCommandResult MissingGymContext { get; } =
        new(ClientSessionCommandStatus.MissingGymContext, null);

    public static ClientSessionCommandResult MemberRequired(string message) =>
        new(ClientSessionCommandStatus.MemberRequired, message);

    public static ClientSessionCommandResult Failed(string message) =>
        new(ClientSessionCommandStatus.Failed, message);
}

public interface IClientSessionsPageService
{
    Task<ClientSessionsPageResult<SessionsPageViewModel>> BuildIndexAsync(CancellationToken cancellationToken = default);

    Task<ClientSessionsPageResult<SessionDetailPageViewModel>> BuildDetailsAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<ClientSessionsPageResult<TrainerRosterPageViewModel>> BuildRosterAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<ClientSessionCommandResult> BookAsync(Guid sessionId, string? paymentReference, CancellationToken cancellationToken = default);

    Task<ClientSessionCommandResult> CancelBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<ClientSessionCommandResult> UpdateAttendanceAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken = default);
}

public sealed class ClientSessionsPageService(
    IUserContextService userContextService,
    IAuthorizationService authorizationService,
    ITrainingWorkflowService trainingWorkflowService,
    IClientSessionsQueryService sessionsQueryService) : IClientSessionsPageService
{
    public async Task<ClientSessionsPageResult<SessionsPageViewModel>> BuildIndexAsync(CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!HasActiveGymContext(context))
        {
            return ClientSessionsPageResult<SessionsPageViewModel>.MissingGymContext;
        }

        return ClientSessionsPageResult<SessionsPageViewModel>.Success(new SessionsPageViewModel
        {
            GymCode = context.ActiveGymCode!,
            Sessions = await trainingWorkflowService.GetSessionsAsync(context.ActiveGymCode!, cancellationToken: cancellationToken)
        });
    }

    public async Task<ClientSessionsPageResult<SessionDetailPageViewModel>> BuildDetailsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!HasActiveGymContext(context))
        {
            return ClientSessionsPageResult<SessionDetailPageViewModel>.MissingGymContext;
        }

        var gymId = context.ActiveGymId!.Value;
        var gymCode = context.ActiveGymCode!;
        var session = await trainingWorkflowService.GetSessionAsync(gymCode, sessionId, cancellationToken);
        var currentMember = await authorizationService.GetCurrentMemberAsync(gymId, cancellationToken);
        var currentStaffId = await ResolveCurrentRosterStaffIdAsync(context, gymId, cancellationToken);
        var snapshot = await sessionsQueryService.GetDetailSnapshotAsync(
            gymId,
            sessionId,
            currentMember?.Id,
            currentStaffId,
            cancellationToken);

        var culture = CultureInfo.CurrentUICulture.Name;

        return ClientSessionsPageResult<SessionDetailPageViewModel>.Success(new SessionDetailPageViewModel
        {
            GymCode = gymCode,
            Session = session,
            CategoryName = snapshot.CategoryName?.Translate(culture) ?? string.Empty,
            TrainerNames = snapshot.TrainerNames,
            CurrentMemberId = currentMember?.Id,
            CurrentBookingId = snapshot.CurrentBooking?.BookingId,
            CurrentBookingStatus = snapshot.CurrentBooking?.Status,
            CanManageRoster = CanManageRoster(context, snapshot.CurrentStaffCanManageRoster)
        });
    }

    public async Task<ClientSessionsPageResult<TrainerRosterPageViewModel>> BuildRosterAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!HasActiveGymContext(context))
        {
            return ClientSessionsPageResult<TrainerRosterPageViewModel>.MissingGymContext;
        }

        if (!await CanManageRosterAsync(context, sessionId, cancellationToken))
        {
            return ClientSessionsPageResult<TrainerRosterPageViewModel>.Forbidden;
        }

        var gymId = context.ActiveGymId!.Value;
        var gymCode = context.ActiveGymCode!;
        var session = await trainingWorkflowService.GetSessionAsync(gymCode, sessionId, cancellationToken);
        var rosterRows = await sessionsQueryService.GetRosterBookingsAsync(gymId, sessionId, cancellationToken);

        return ClientSessionsPageResult<TrainerRosterPageViewModel>.Success(new TrainerRosterPageViewModel
        {
            GymCode = gymCode,
            SessionId = sessionId,
            SessionName = session.Name,
            StartAtUtc = session.StartAtUtc,
            Bookings = rosterRows
                .Select(row => new TrainerRosterBookingViewModel
                {
                    BookingId = row.BookingId,
                    MemberName = $"{row.MemberFirstName} {row.MemberLastName}".Trim(),
                    Status = row.Status,
                    ChargedPrice = row.ChargedPrice,
                    PaymentRequired = row.PaymentRequired
                })
                .ToArray()
        });
    }

    public async Task<ClientSessionCommandResult> BookAsync(
        Guid sessionId,
        string? paymentReference,
        CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (!HasActiveGymContext(context))
        {
            return ClientSessionCommandResult.MissingGymContext;
        }

        var currentMember = await authorizationService.GetCurrentMemberAsync(context.ActiveGymId!.Value, cancellationToken);
        if (currentMember == null)
        {
            return ClientSessionCommandResult.MemberRequired("Only member accounts can book training sessions.");
        }

        try
        {
            await trainingWorkflowService.CreateBookingAsync(context.ActiveGymCode!, new BookingCreateRequest
            {
                TrainingSessionId = sessionId,
                MemberId = currentMember.Id,
                PaymentReference = paymentReference
            }, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ClientSessionCommandResult.Failed(exception.Message);
        }

        return ClientSessionCommandResult.Success("Booking confirmed.");
    }

    public async Task<ClientSessionCommandResult> CancelBookingAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return ClientSessionCommandResult.MissingGymContext;
        }

        await trainingWorkflowService.CancelBookingAsync(context.ActiveGymCode, bookingId, cancellationToken);
        return ClientSessionCommandResult.Success("Booking cancelled.");
    }

    public async Task<ClientSessionCommandResult> UpdateAttendanceAsync(
        Guid bookingId,
        BookingStatus status,
        CancellationToken cancellationToken = default)
    {
        var context = userContextService.GetCurrent();
        if (string.IsNullOrWhiteSpace(context.ActiveGymCode))
        {
            return ClientSessionCommandResult.MissingGymContext;
        }

        await trainingWorkflowService.UpdateAttendanceAsync(context.ActiveGymCode, bookingId, new AttendanceUpdateRequest
        {
            Status = status
        }, cancellationToken);

        return ClientSessionCommandResult.Success("Attendance updated.");
    }

    private static bool HasActiveGymContext(UserExecutionContext context) =>
        context.ActiveGymId.HasValue && !string.IsNullOrWhiteSpace(context.ActiveGymCode);

    private static bool CanManageRoster(UserExecutionContext context, bool currentStaffCanManageRoster)
    {
        if (context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin))
        {
            return true;
        }

        return context.HasRole(RoleNames.Trainer) && currentStaffCanManageRoster;
    }

    private async Task<bool> CanManageRosterAsync(
        UserExecutionContext context,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!context.ActiveGymId.HasValue)
        {
            return false;
        }

        if (context.HasRole(RoleNames.GymOwner) || context.HasRole(RoleNames.GymAdmin))
        {
            return true;
        }

        if (!context.HasRole(RoleNames.Trainer))
        {
            return false;
        }

        var staff = await authorizationService.GetCurrentStaffAsync(context.ActiveGymId.Value, cancellationToken);
        return staff is not null &&
               await sessionsQueryService.HasTrainerAssignmentAsync(
                   context.ActiveGymId.Value,
                   sessionId,
                   staff.Id,
                   cancellationToken);
    }

    private async Task<Guid?> ResolveCurrentRosterStaffIdAsync(
        UserExecutionContext context,
        Guid gymId,
        CancellationToken cancellationToken)
    {
        if (!context.HasRole(RoleNames.Trainer) ||
            context.HasRole(RoleNames.GymOwner) ||
            context.HasRole(RoleNames.GymAdmin))
        {
            return null;
        }

        var staff = await authorizationService.GetCurrentStaffAsync(gymId, cancellationToken);
        return staff?.Id;
    }
}
