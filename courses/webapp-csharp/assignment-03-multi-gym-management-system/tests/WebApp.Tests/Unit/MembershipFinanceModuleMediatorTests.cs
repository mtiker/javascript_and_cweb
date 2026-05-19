using App.BLL.Contracts.Persistence;
using App.BLL.Mapping;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using BuildingBlocks;
using BuildingBlocks.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modules.MembershipFinance;
using Modules.MembershipFinance.Contracts;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class MembershipFinanceModuleMediatorTests
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task Mediator_DispatchesMembershipPackageCrudMessagesThroughModuleOwnedHandlers()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var (mediator, authorization) = CreatePackageMediator(dbContext, gymId);
        var createRequest = NewPackageRequest(" Monthly ");
        createRequest.CurrencyCode = "eur";

        var listedBeforeCreate = await mediator.SendAsync(new ListMembershipPackagesQuery(GymCode));
        var created = await mediator.SendAsync(new CreateMembershipPackageCommand(GymCode, createRequest));
        var listedAfterCreate = await mediator.SendAsync(new ListMembershipPackagesQuery(GymCode));
        var updateRequest = NewPackageRequest("Monthly Plus");
        updateRequest.BasePrice = 59m;

        var updated = await mediator.SendAsync(new UpdateMembershipPackageCommand(GymCode, created.Id, updateRequest));
        await mediator.SendAsync(new DeleteMembershipPackageCommand(GymCode, created.Id));

        var deleted = await dbContext.MembershipPackages
            .IgnoreQueryFilters()
            .SingleAsync(package => package.Id == created.Id);

        Assert.Empty(listedBeforeCreate);
        Assert.Contains(listedAfterCreate, package => package.Id == created.Id);
        Assert.Equal("Monthly", created.Name);
        Assert.Equal("EUR", created.CurrencyCode);
        Assert.Equal("Monthly Plus", updated.Name);
        Assert.Equal(59m, updated.BasePrice);
        Assert.True(deleted.IsDeleted);
        Assert.Contains(authorization.RoleChecks, roles => roles.SequenceEqual([RoleNames.GymOwner, RoleNames.GymAdmin, RoleNames.Member]));
        Assert.Contains(authorization.RoleChecks, roles => roles.SequenceEqual([RoleNames.GymOwner, RoleNames.GymAdmin]));
    }

    [Fact]
    public async Task Mediator_DispatchesMembershipStatusAndPaymentMessages()
    {
        var (mediator, membership) = CreateWorkflowMediator();
        var membershipId = Guid.NewGuid();

        await mediator.SendAsync(new ListMembershipsQuery(GymCode));
        await mediator.SendAsync(new UpdateMembershipStatusCommand(GymCode, membershipId, new MembershipStatusUpdateRequest { Status = MembershipStatus.Paused }));
        await mediator.SendAsync(new ListPaymentsQuery(GymCode));
        await mediator.SendAsync(new CreatePaymentCommand(GymCode, new PaymentCreateRequest
        {
            MembershipId = membershipId,
            Amount = 49m,
            CurrencyCode = "EUR",
            Reference = "PAY-MEM-1"
        }));

        Assert.Contains("memberships:list", membership.Calls);
        Assert.Contains($"memberships:status:{membershipId}:Paused", membership.Calls);
        Assert.Contains("payments:list", membership.Calls);
        Assert.Contains("payments:create:PAY-MEM-1", membership.Calls);
    }

    private static (IMediator Mediator, RecordingAuthorizationService Authorization) CreatePackageMediator(AppDbContext dbContext, Guid gymId)
    {
        var authorization = new RecordingAuthorizationService(gymId);
        var services = new ServiceCollection();
        services.AddBuildingBlocks();
        services.AddMembershipFinanceModule();
        services.AddScoped(_ => dbContext);
        services.AddScoped<IAppUnitOfWork, EfAppUnitOfWork>();
        services.AddScoped<IMembershipFinanceMapper, MembershipFinanceMapper>();
        services.AddSingleton(authorization);
        services.AddSingleton<IAuthorizationService>(authorization);

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return (
            scope.ServiceProvider.GetRequiredService<IMediator>(),
            scope.ServiceProvider.GetRequiredService<RecordingAuthorizationService>());
    }

    private static AppDbContext CreateDbContext(out Guid gymId)
    {
        gymId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var dbContext = new AppDbContext(options, new TestGymContext(gymId));
        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Test Gym",
            Code = GymCode,
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });
        dbContext.SaveChanges();
        return dbContext;
    }

    private static MembershipPackageUpsertRequest NewPackageRequest(string name)
    {
        return new MembershipPackageUpsertRequest
        {
            Name = name,
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 49m,
            CurrencyCode = "EUR",
            TrainingDiscountPercent = null,
            IsTrainingFree = false,
            Description = "Mediator package test."
        };
    }

    private sealed class RecordingMembershipWorkflowService : DelegatingMembershipWorkflowService
    {
        public List<string> Calls { get; } = [];

        public RecordingMembershipWorkflowService()
        {
            GetPackagesAsyncHandler = (_, _) =>
            {
                Calls.Add("packages:list");
                return Task.FromResult<IReadOnlyCollection<MembershipPackageResponse>>([]);
            };
            CreatePackageAsyncHandler = (_, request, _) =>
            {
                Calls.Add($"packages:create:{request.Name}");
                return Task.FromResult(new MembershipPackageResponse { Id = Guid.NewGuid(), Name = request.Name ?? string.Empty });
            };
            UpdatePackageAsyncHandler = (_, id, request, _) =>
            {
                Calls.Add($"packages:update:{id}:{request.Name}");
                return Task.FromResult(new MembershipPackageResponse { Id = id, Name = request.Name ?? string.Empty });
            };
            DeletePackageAsyncHandler = (_, id, _) =>
            {
                Calls.Add($"packages:delete:{id}");
                return Task.CompletedTask;
            };
            GetMembershipsAsyncHandler = (_, _) =>
            {
                Calls.Add("memberships:list");
                return Task.FromResult<IReadOnlyCollection<MembershipResponse>>([]);
            };
            UpdateMembershipStatusAsyncHandler = (_, id, request, _) =>
            {
                Calls.Add($"memberships:status:{id}:{request.Status}");
                return Task.FromResult(new MembershipResponse { Id = id, Status = request.Status });
            };
            GetPaymentsAsyncHandler = (_, _) =>
            {
                Calls.Add("payments:list");
                return Task.FromResult<IReadOnlyCollection<PaymentResponse>>([]);
            };
            CreatePaymentAsyncHandler = (_, request, _) =>
            {
                Calls.Add($"payments:create:{request.Reference}");
                return Task.FromResult(new PaymentResponse { Id = Guid.NewGuid(), Amount = request.Amount, Reference = request.Reference });
            };
        }
    }

    private static (IMediator Mediator, RecordingMembershipWorkflowService Membership) CreateWorkflowMediator()
    {
        var membership = new RecordingMembershipWorkflowService();
        return (new MembershipFinanceMediatorAdapter(membership), membership);
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId => gymId;
        public string? GymCode => GymCode;
        public string? ActiveRole => RoleNames.GymAdmin;
        public bool IgnoreGymFilter => false;
    }

    private sealed class RecordingAuthorizationService(Guid gymId) : IAuthorizationService
    {
        public List<string[]> RoleChecks { get; } = [];

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles)
        {
            RoleChecks.Add(allowedRoles);
            return Task.FromResult(gymId);
        }

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles)
        {
            RoleChecks.Add(allowedRoles);
            return Task.FromResult(gymId);
        }

        public Task<Member?> GetCurrentMemberAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Member?>(null);
        public Task<Staff?> GetCurrentStaffAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);
        public Task EnsureMemberSelfAccessAsync(Guid gymIdValue, Guid memberId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
