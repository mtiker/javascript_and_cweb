using App.BLL.Exceptions;
using App.BLL.Mapping;
using App.BLL.Services;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using App.DAL.EF.Tenant;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Finance;
using App.DTO.v1.MembershipPackages;
using App.DTO.v1.Memberships;
using App.DTO.v1.Payments;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class MembershipFinanceCleanSliceTests
{
    private const string GymCode = "test-gym";

    [Fact]
    public async Task PackageCrud_UsesRepositoryBackedService()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var service = CreatePackageService(dbContext, new TestAuthorizationService(gymId));

        var created = await service.CreatePackageAsync(GymCode, NewPackageRequest("Monthly Clean Slice"));
        var listed = await service.GetPackagesAsync(GymCode);
        var updateRequest = NewPackageRequest("Monthly Clean Slice Updated");
        updateRequest.BasePrice = 89m;

        var updated = await service.UpdatePackageAsync(GymCode, created.Id, updateRequest);
        await service.DeletePackageAsync(GymCode, created.Id);

        var deleted = await dbContext.MembershipPackages.IgnoreQueryFilters().SingleAsync(package => package.Id == created.Id);
        Assert.Contains(listed, package => package.Id == created.Id);
        Assert.Equal("Monthly Clean Slice Updated", updated.Name);
        Assert.Equal(89m, updated.BasePrice);
        Assert.True(deleted.IsDeleted);
    }

    [Fact]
    public async Task PackageValidation_RejectsInvalidValues()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var service = CreatePackageService(dbContext, new TestAuthorizationService(gymId));
        var request = NewPackageRequest("Invalid Package");
        request.DurationValue = 0;
        request.BasePrice = -1m;
        request.CurrencyCode = "EURO";
        request.TrainingDiscountPercent = 101;

        var exception = await Assert.ThrowsAsync<ValidationAppException>(() => service.CreatePackageAsync(GymCode, request));

        Assert.Contains(exception.Errors, error => error.Contains("Duration value", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exception.Errors, error => error.Contains("Base price", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exception.Errors, error => error.Contains("Currency code", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(exception.Errors, error => error.Contains("Training discount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeleteUsedPackage_ThrowsConflictAndKeepsPackage()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var (member, package) = SeedMemberAndPackage(dbContext, gymId);
        dbContext.Memberships.Add(new Membership
        {
            GymId = gymId,
            Member = member,
            MembershipPackage = package,
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 31),
            PriceAtPurchase = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            Status = MembershipStatus.Active
        });
        await dbContext.SaveChangesAsync();

        var service = CreatePackageService(dbContext, new TestAuthorizationService(gymId));

        await Assert.ThrowsAsync<ConflictAppException>(() => service.DeletePackageAsync(GymCode, package.Id));
        var persistedPackage = await dbContext.MembershipPackages.SingleAsync(entity => entity.Id == package.Id);
        Assert.False(persistedPackage.IsDeleted);
    }

    [Fact]
    public async Task MembershipStatusTransitions_AllowValidAndRejectInvalidTransitions()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var (member, package) = SeedMemberAndPackage(dbContext, gymId);
        var membership = new Membership
        {
            GymId = gymId,
            Member = member,
            MembershipPackage = package,
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 31),
            PriceAtPurchase = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            Status = MembershipStatus.Active
        };
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();

        var service = CreateMembershipService(dbContext, new TestAuthorizationService(gymId));
        var paused = await service.UpdateMembershipStatusAsync(GymCode, membership.Id, new MembershipStatusUpdateRequest { Status = MembershipStatus.Paused });

        Assert.Equal(MembershipStatus.Paused, paused.Status);
        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.UpdateMembershipStatusAsync(GymCode, membership.Id, new MembershipStatusUpdateRequest { Status = MembershipStatus.Refunded }));
    }

    [Fact]
    public async Task InvoicePaymentRefundAndWorkspaceBalance_AreCalculatedFromLedgerEntries()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var (member, _) = SeedMemberAndPackage(dbContext, gymId);
        await dbContext.SaveChangesAsync();

        var service = CreateFinanceService(
            dbContext,
            new TestAuthorizationService(gymId),
            new TestUserContextService(gymId, RoleNames.GymAdmin));

        var invoice = await service.CreateInvoiceAsync(GymCode, new InvoiceCreateRequest
        {
            MemberId = member.Id,
            DueAtUtc = DateTime.UtcNow.AddDays(7),
            CurrencyCode = "eur",
            Lines =
            [
                new InvoiceLineRequest { Description = "Membership", Quantity = 1m, UnitPrice = 100m },
                new InvoiceLineRequest { Description = "Credit", Quantity = 1m, UnitPrice = 20m, IsCredit = true }
            ]
        });

        var afterPayment = await service.AddInvoicePaymentAsync(GymCode, invoice.Id, new InvoicePaymentRequest
        {
            Amount = 40m,
            Reference = "PAY-1"
        });

        var afterRefund = await service.AddInvoiceRefundAsync(GymCode, invoice.Id, new InvoicePaymentRequest
        {
            Amount = 10m,
            Reference = "REF-1"
        });

        var workspace = await service.GetWorkspaceAsync(GymCode, member.Id);

        Assert.Equal(100m, invoice.SubtotalAmount);
        Assert.Equal(20m, invoice.CreditAmount);
        Assert.Equal(80m, invoice.TotalAmount);
        Assert.Equal("EUR", invoice.CurrencyCode);
        Assert.Equal(40m, afterPayment.PaidAmount);
        Assert.Equal(40m, afterPayment.OutstandingAmount);
        Assert.Equal(30m, afterRefund.PaidAmount);
        Assert.Equal(50m, afterRefund.OutstandingAmount);
        Assert.Equal(50m, workspace.OutstandingBalance);
        Assert.Equal(10m, workspace.TotalRefundCredits);
        Assert.Contains(workspace.PaymentHistory, payment => payment.IsRefund && payment.Reference == "REF-1");
    }

    [Fact]
    public async Task PaymentPosting_RequiresAccessibleMembershipOrBooking()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var (member, package) = SeedMemberAndPackage(dbContext, gymId);
        var membership = new Membership
        {
            GymId = gymId,
            Member = member,
            MembershipPackage = package,
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 5, 31),
            PriceAtPurchase = package.BasePrice,
            CurrencyCode = package.CurrencyCode,
            Status = MembershipStatus.Active
        };
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();

        var service = CreatePaymentService(dbContext, new TestAuthorizationService(gymId));
        var payment = await service.CreatePaymentAsync(GymCode, new PaymentCreateRequest
        {
            MembershipId = membership.Id,
            Amount = 79m,
            CurrencyCode = "EUR",
            Reference = "MEM-PAY-1"
        });

        Assert.Equal(membership.Id, payment.MembershipId);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("MEM-PAY-1", payment.Reference);
    }

    [Fact]
    public async Task MemberFinanceWorkspace_RejectsAnotherMembersWorkspace()
    {
        await using var dbContext = CreateDbContext(out var gymId);
        var firstMember = new Member { GymId = gymId, Person = new Person { FirstName = "First", LastName = "Member" }, MemberCode = "MEM-1" };
        var secondMember = new Member { GymId = gymId, Person = new Person { FirstName = "Second", LastName = "Member" }, MemberCode = "MEM-2" };
        dbContext.Members.AddRange(firstMember, secondMember);
        await dbContext.SaveChangesAsync();

        var service = CreateFinanceService(
            dbContext,
            new TestAuthorizationService(gymId, firstMember),
            new TestUserContextService(gymId, RoleNames.Member));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetWorkspaceAsync(GymCode, secondMember.Id));
    }

    private static MembershipPackageUpsertRequest NewPackageRequest(string name)
    {
        return new MembershipPackageUpsertRequest
        {
            Name = name,
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 79m,
            CurrencyCode = "EUR",
            TrainingDiscountPercent = null,
            IsTrainingFree = false,
            Description = "Clean slice package."
        };
    }

    private static AppDbContext CreateDbContext(out Guid gymId)
    {
        gymId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var dbContext = new AppDbContext(options, new TestGymContext(gymId), new HttpContextAccessor());
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

    private static (Member Member, MembershipPackage Package) SeedMemberAndPackage(AppDbContext dbContext, Guid gymId)
    {
        var member = new Member
        {
            GymId = gymId,
            Person = new Person { FirstName = "Test", LastName = "Member" },
            MemberCode = $"MEM-{Guid.NewGuid():N}"[..12]
        };
        var package = new MembershipPackage
        {
            GymId = gymId,
            Name = "Monthly Pass",
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 1,
            DurationUnit = DurationUnit.Month,
            BasePrice = 79m,
            CurrencyCode = "EUR"
        };
        dbContext.Members.Add(member);
        dbContext.MembershipPackages.Add(package);
        return (member, package);
    }

    private static IMembershipPackageService CreatePackageService(AppDbContext dbContext, IAuthorizationService authorizationService)
    {
        return new MembershipPackageService(
            new EfAppUnitOfWork(dbContext),
            authorizationService,
            new MembershipFinanceMapper());
    }

    private static IMembershipService CreateMembershipService(AppDbContext dbContext, IAuthorizationService authorizationService)
    {
        return new MembershipService(
            new EfAppUnitOfWork(dbContext),
            authorizationService,
            new MembershipFinanceMapper());
    }

    private static IPaymentService CreatePaymentService(AppDbContext dbContext, IAuthorizationService authorizationService)
    {
        return new PaymentService(
            new EfAppUnitOfWork(dbContext),
            authorizationService,
            new MembershipFinanceMapper());
    }

    private static IFinanceWorkspaceService CreateFinanceService(
        AppDbContext dbContext,
        IAuthorizationService authorizationService,
        IUserContextService userContextService)
    {
        return new FinanceWorkspaceService(
            new EfAppUnitOfWork(dbContext),
            authorizationService,
            userContextService,
            new MembershipFinanceMapper());
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId => gymId;
        public string? GymCode => GymCode;
        public string? ActiveRole => RoleNames.GymAdmin;
        public bool IgnoreGymFilter => false;
    }

    private sealed class TestUserContextService(Guid gymId, string roleName) : IUserContextService
    {
        public UserExecutionContext GetCurrent()
        {
            return new UserExecutionContext(
                Guid.NewGuid(),
                null,
                gymId,
                GymCode,
                roleName,
                [roleName],
                []);
        }
    }

    private sealed class TestAuthorizationService(Guid gymId, Member? currentMember = null) : IAuthorizationService
    {
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Member?> GetCurrentMemberAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult(currentMember);
        public Task<Staff?> GetCurrentStaffAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);

        public Task EnsureMemberSelfAccessAsync(Guid gymIdValue, Guid memberId, CancellationToken cancellationToken = default)
        {
            if (currentMember is not null && currentMember.Id != memberId)
            {
                throw new ForbiddenException("Members can access only their own finance workspace.");
            }

            return Task.CompletedTask;
        }

        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
