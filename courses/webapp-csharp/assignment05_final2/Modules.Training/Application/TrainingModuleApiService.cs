using App.DAL.EF;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.ModuleApis;

namespace Modules.Training.Application;

/// <summary>
/// Phase 7 implementation of <see cref="ITrainingModuleApi"/>. It uses the
/// shared <see cref="AppDbContext"/> while persistence is still transitional
/// (Phase 7-9) and returns shared projections only.
/// </summary>
internal sealed class TrainingModuleApiService(AppDbContext dbContext) : ITrainingModuleApi
{
    public async Task<StaffSummary?> GetStaffSummaryAsync(
        Guid gymId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.Staff
            .AsNoTracking()
            .Where(staff => staff.GymId == gymId && staff.Id == staffId)
            .Select(staff => new
            {
                staff.Id,
                staff.GymId,
                staff.StaffCode,
                FirstName = staff.Person!.FirstName,
                LastName = staff.Person!.LastName,
                staff.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new StaffSummary(
                row.Id,
                row.GymId,
                row.StaffCode,
                BuildFullName(row.FirstName, row.LastName),
                row.Status.ToString());
    }

    public async Task<TrainingSessionSummary?> GetTrainingSessionSummaryAsync(
        Guid gymId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await dbContext.TrainingSessions
            .AsNoTracking()
            .Where(session => session.GymId == gymId && session.Id == sessionId)
            .FirstOrDefaultAsync(cancellationToken);

        return session is null
            ? null
            : new TrainingSessionSummary(
                session.Id,
                session.GymId,
                session.CategoryId,
                session.TrainerStaffId,
                session.Name.ToString(),
                session.StartAtUtc,
                session.EndAtUtc,
                session.Capacity,
                session.Status.ToString());
    }

    public async Task<BookingSummary?> GetBookingSummaryAsync(
        Guid gymId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.GymId == gymId && booking.Id == bookingId)
            .Select(booking => new BookingSummary(
                booking.Id,
                booking.GymId,
                booking.MemberId,
                booking.TrainingSessionId))
            .FirstOrDefaultAsync(cancellationToken);

        return booking;
    }

    public async Task<IReadOnlyList<Guid>> ListBookingIdsForMemberAsync(
        Guid gymId,
        Guid memberId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.GymId == gymId && booking.MemberId == memberId)
            .Select(booking => booking.Id)
            .ToArrayAsync(cancellationToken);
    }

    private static string BuildFullName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();
}
