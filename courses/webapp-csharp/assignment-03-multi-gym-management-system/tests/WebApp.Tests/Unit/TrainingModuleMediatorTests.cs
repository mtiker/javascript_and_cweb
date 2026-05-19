using App.BLL.Contracts.Persistence;
using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.BLL.Services;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Bookings;
using App.DTO.v1.TrainingCategories;
using App.DTO.v1.TrainingSessions;
using BuildingBlocks;
using BuildingBlocks.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Modules.Training;
using Modules.Training.Contracts;

namespace WebApp.Tests.Unit;

public class TrainingModuleMediatorTests
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task Mediator_DispatchesTrainingCategoryCrudMessagesThroughModuleOwnedWorkflow()
    {
        var fixture = CreateMediator();
        var foreign = new TrainingCategory
        {
            GymId = Guid.NewGuid(),
            Name = new LangStr("Foreign", "en")
        };
        fixture.Categories.Entities.Add(foreign);

        var created = await fixture.Mediator.SendAsync(new CreateTrainingCategoryCommand(
            GymCode,
            new TrainingCategoryUpsertRequest
            {
                Name = "  Strength  ",
                Description = "  Coach-led barbell  "
            }));
        var listed = await fixture.Mediator.SendAsync(new ListTrainingCategoriesQuery(GymCode));
        var updated = await fixture.Mediator.SendAsync(new UpdateTrainingCategoryCommand(
            GymCode,
            created.Id,
            new TrainingCategoryUpsertRequest
            {
                Name = "Conditioning",
                Description = "Intervals"
            }));
        await fixture.Mediator.SendAsync(new DeleteTrainingCategoryCommand(GymCode, created.Id));

        Assert.Equal("Strength", created.Name);
        Assert.Equal("Coach-led barbell", created.Description);
        Assert.Equal("Conditioning", updated.Name);
        Assert.Equal("Intervals", updated.Description);
        Assert.DoesNotContain(listed, category => category.Id == foreign.Id);
        Assert.Contains(listed, category => category.Id == created.Id);
        Assert.DoesNotContain(fixture.Categories.Entities, category => category.Id == created.Id);
        Assert.Equal(3, fixture.UnitOfWork.SaveChangesCount);
        Assert.DoesNotContain(fixture.Workflow.Calls, call => call.StartsWith("categories:", StringComparison.Ordinal));
        Assert.Contains(fixture.Authorization.Calls, call => call.AllowedRoles.Contains(App.Domain.RoleNames.Member));
        Assert.Contains(fixture.Authorization.Calls, call => call.AllowedRoles.SequenceEqual([App.Domain.RoleNames.GymOwner, App.Domain.RoleNames.GymAdmin]));
    }

    [Fact]
    public async Task Mediator_TrainingCategoryUpdateAndDeleteRemainTenantScoped()
    {
        var fixture = CreateMediator();
        var foreign = new TrainingCategory
        {
            GymId = Guid.NewGuid(),
            Name = new LangStr("Foreign", "en")
        };
        fixture.Categories.Entities.Add(foreign);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.Mediator.SendAsync(new UpdateTrainingCategoryCommand(
                GymCode,
                foreign.Id,
                new TrainingCategoryUpsertRequest { Name = "Blocked" })));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.Mediator.SendAsync(new DeleteTrainingCategoryCommand(GymCode, foreign.Id)));

        Assert.Contains(fixture.Categories.Entities, category => category.Id == foreign.Id);
    }

    [Fact]
    public async Task Mediator_DispatchesSessionListDetailAndUpsertMessages()
    {
        var fixture = CreateMediator();
        var mediator = fixture.Mediator;
        var workflow = fixture.Workflow;
        var sessionId = Guid.NewGuid();
        var request = new TrainingSessionUpsertRequest
        {
            CategoryId = Guid.NewGuid(),
            Name = "Upper Body",
            StartAtUtc = DateTime.UtcNow.AddDays(1),
            EndAtUtc = DateTime.UtcNow.AddDays(1).AddHours(1),
            Capacity = 8,
            BasePrice = 10m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published,
        };

        await mediator.SendAsync(new ListTrainingSessionsQuery(GymCode));
        await mediator.SendAsync(new GetTrainingSessionQuery(GymCode, sessionId));
        await mediator.SendAsync(new CreateTrainingSessionCommand(GymCode, request));
        await mediator.SendAsync(new UpdateTrainingSessionCommand(GymCode, sessionId, request));
        await mediator.SendAsync(new DeleteTrainingSessionCommand(GymCode, sessionId));

        Assert.Contains("sessions:list", workflow.Calls);
        Assert.Contains($"sessions:get:{sessionId}", workflow.Calls);
        Assert.Contains("sessions:create:Upper Body", workflow.Calls);
        Assert.Contains($"sessions:update:{sessionId}:Upper Body", workflow.Calls);
        Assert.Contains($"sessions:delete:{sessionId}", workflow.Calls);
    }

    [Fact]
    public async Task Mediator_DispatchesBookingAndAttendanceMessages()
    {
        var fixture = CreateMediator();
        var mediator = fixture.Mediator;
        var workflow = fixture.Workflow;
        var sessionId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        await mediator.SendAsync(new ListBookingsQuery(GymCode));
        await mediator.SendAsync(new CreateBookingCommand(GymCode, new BookingCreateRequest
        {
            TrainingSessionId = sessionId,
            MemberId = memberId,
        }));
        await mediator.SendAsync(new UpdateBookingAttendanceCommand(GymCode, bookingId, new AttendanceUpdateRequest
        {
            Status = BookingStatus.Attended,
        }));
        await mediator.SendAsync(new CancelBookingCommand(GymCode, bookingId));

        Assert.Contains("bookings:list", workflow.Calls);
        Assert.Contains($"bookings:create:{sessionId}:{memberId}", workflow.Calls);
        Assert.Contains($"bookings:attendance:{bookingId}:Attended", workflow.Calls);
        Assert.Contains($"bookings:cancel:{bookingId}", workflow.Calls);
    }

    private static TrainingMediatorFixture CreateMediator()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocks();
        services.AddTrainingModule();
        services.AddScoped<RecordingAuthorizationService>();
        services.AddScoped<IAuthorizationService>(provider => provider.GetRequiredService<RecordingAuthorizationService>());
        services.AddScoped<RecordingTrainingCategoryRepository>();
        services.AddScoped<ITrainingCategoryRepository>(provider => provider.GetRequiredService<RecordingTrainingCategoryRepository>());
        services.AddScoped<RecordingAppUnitOfWork>();
        services.AddScoped<IAppUnitOfWork>(provider => provider.GetRequiredService<RecordingAppUnitOfWork>());
        services.AddScoped<ITrainingMapper, TrainingMapper>();
        services.AddScoped<RecordingTrainingWorkflowService>();
        services.AddScoped<ITrainingWorkflowService>(provider => provider.GetRequiredService<RecordingTrainingWorkflowService>());

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return new TrainingMediatorFixture(
            scope.ServiceProvider.GetRequiredService<IMediator>(),
            scope.ServiceProvider.GetRequiredService<RecordingTrainingWorkflowService>(),
            scope.ServiceProvider.GetRequiredService<RecordingTrainingCategoryRepository>(),
            scope.ServiceProvider.GetRequiredService<RecordingAuthorizationService>(),
            scope.ServiceProvider.GetRequiredService<RecordingAppUnitOfWork>());
    }

    private sealed record TrainingMediatorFixture(
        IMediator Mediator,
        RecordingTrainingWorkflowService Workflow,
        RecordingTrainingCategoryRepository Categories,
        RecordingAuthorizationService Authorization,
        RecordingAppUnitOfWork UnitOfWork);

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public Guid GymId { get; } = Guid.NewGuid();

        public List<(string GymCode, string[] AllowedRoles)> Calls { get; } = [];

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles)
        {
            Calls.Add((gymCode, allowedRoles));
            return Task.FromResult(GymId);
        }

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles)
        {
            Calls.Add((gymCode, allowedRoles));
            return Task.FromResult(GymId);
        }

        public Task<Member?> GetCurrentMemberAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.FromResult<Member?>(null);

        public Task<Staff?> GetCurrentStaffAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);

        public Task EnsureMemberSelfAccessAsync(Guid gymId, Guid memberId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingTrainingCategoryRepository : ITrainingCategoryRepository
    {
        public List<TrainingCategory> Entities { get; } = [];

        public Task<IReadOnlyList<TrainingCategory>> ListByGymAsync(Guid gymId, CancellationToken cancellationToken = default)
        {
            var categories = Entities
                .Where(category => category.GymId == gymId)
                .OrderBy(category => category.ValidFrom)
                .ToArray();

            return Task.FromResult<IReadOnlyList<TrainingCategory>>(categories);
        }

        public Task<TrainingCategory?> FindAsync(Guid gymId, Guid categoryId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Entities.FirstOrDefault(category => category.GymId == gymId && category.Id == categoryId));
        }

        public Task AddAsync(TrainingCategory category, CancellationToken cancellationToken = default)
        {
            Entities.Add(category);
            return Task.CompletedTask;
        }

        public void Remove(TrainingCategory category)
        {
            Entities.Remove(category);
        }
    }

    private sealed class RecordingAppUnitOfWork(ITrainingCategoryRepository trainingCategories) : IAppUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public IMemberRepository Members => throw new NotSupportedException();
        public ITrainingCategoryRepository TrainingCategories { get; } = trainingCategories;
        public ITrainingSessionRepository TrainingSessions => throw new NotSupportedException();
        public IBookingRepository Bookings => throw new NotSupportedException();
        public IMembershipPackageRepository MembershipPackages => throw new NotSupportedException();
        public IMembershipRepository Memberships => throw new NotSupportedException();
        public IPaymentRepository Payments => throw new NotSupportedException();
        public IMaintenanceRepository Maintenance => throw new NotSupportedException();

        public IRepository<TEntity, Guid> Repository<TEntity>()
            where TEntity : class
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class RecordingTrainingWorkflowService : ITrainingWorkflowService
    {
        public List<string> Calls { get; } = [];

        public Task<IReadOnlyCollection<TrainingCategoryResponse>> GetCategoriesAsync(string gymCode, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Training category workflow must be handled by Modules.Training.Application, not ITrainingWorkflowService.");
        }

        public Task<TrainingCategoryResponse> CreateCategoryAsync(string gymCode, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Training category workflow must be handled by Modules.Training.Application, not ITrainingWorkflowService.");
        }

        public Task<TrainingCategoryResponse> UpdateCategoryAsync(string gymCode, Guid id, TrainingCategoryUpsertRequest request, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Training category workflow must be handled by Modules.Training.Application, not ITrainingWorkflowService.");
        }

        public Task DeleteCategoryAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Training category workflow must be handled by Modules.Training.Application, not ITrainingWorkflowService.");
        }

        public Task<IReadOnlyCollection<TrainingSessionResponse>> GetSessionsAsync(string gymCode, CancellationToken cancellationToken = default)
        {
            Calls.Add("sessions:list");
            return Task.FromResult<IReadOnlyCollection<TrainingSessionResponse>>(Array.Empty<TrainingSessionResponse>());
        }

        public Task<TrainingSessionResponse> GetSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
        {
            Calls.Add($"sessions:get:{id}");
            return Task.FromResult(new TrainingSessionResponse { Id = id, Name = "Session" });
        }

        public Task<TrainingSessionResponse> UpsertTrainingSessionAsync(string gymCode, Guid? sessionId, TrainingSessionUpsertRequest request, CancellationToken cancellationToken = default)
        {
            Calls.Add(sessionId.HasValue ? $"sessions:update:{sessionId.Value}:{request.Name}" : $"sessions:create:{request.Name}");
            return Task.FromResult(new TrainingSessionResponse { Id = sessionId ?? Guid.NewGuid(), Name = request.Name });
        }

        public Task DeleteSessionAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
        {
            Calls.Add($"sessions:delete:{id}");
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<BookingResponse>> GetBookingsAsync(string gymCode, CancellationToken cancellationToken = default)
        {
            Calls.Add("bookings:list");
            return Task.FromResult<IReadOnlyCollection<BookingResponse>>(Array.Empty<BookingResponse>());
        }

        public Task<BookingResponse> CreateBookingAsync(string gymCode, BookingCreateRequest request, CancellationToken cancellationToken = default)
        {
            Calls.Add($"bookings:create:{request.TrainingSessionId}:{request.MemberId}");
            return Task.FromResult(new BookingResponse { Id = Guid.NewGuid(), TrainingSessionId = request.TrainingSessionId, MemberId = request.MemberId });
        }

        public Task<BookingResponse> UpdateAttendanceAsync(string gymCode, Guid bookingId, AttendanceUpdateRequest request, CancellationToken cancellationToken = default)
        {
            Calls.Add($"bookings:attendance:{bookingId}:{request.Status}");
            return Task.FromResult(new BookingResponse { Id = bookingId, Status = request.Status });
        }

        public Task CancelBookingAsync(string gymCode, Guid id, CancellationToken cancellationToken = default)
        {
            Calls.Add($"bookings:cancel:{id}");
            return Task.CompletedTask;
        }
    }
}
