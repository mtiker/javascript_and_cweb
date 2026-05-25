using SharedKernel.Exceptions;
using Modules.Memberships.Application.Mappers;
using App.BLL.Contracts.Services;
using Modules.Gyms.Application.Authorization;
using Modules.Gyms.Application.Platform;
using Modules.Memberships.Application;
using WebApp.Areas.Admin.Queries;
using WebApp.Areas.Client.Queries;
using App.DAL.EF;
using App.DAL.EF.Repositories;
using SharedKernel.Persistence;
using Modules.Maintenance.Infrastructure;
using Modules.Memberships.Infrastructure;
using Modules.Training.Infrastructure;
using Modules.Users.Infrastructure;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Members;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class MemberWorkflowServiceTests
{
    [Fact]
    public async Task CreateMemberAsync_RejectsDuplicateMemberCode()
    {
        var (dbContext, gymId) = await NewContextAsync();
        SeedExistingMember(dbContext, gymId, memberCode: "MEM-DUP", personalCode: "49901010001");
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateMemberAsync("test-gym", new MemberUpsertRequest
            {
                FirstName = "New",
                LastName = "Member",
                MemberCode = "MEM-DUP",
                PersonalCode = "49901010002",
                Status = MemberStatus.Active
            }));
    }

    [Fact]
    public async Task CreateMemberAsync_RejectsDuplicatePersonalCode()
    {
        var (dbContext, gymId) = await NewContextAsync();
        SeedExistingMember(dbContext, gymId, memberCode: "MEM-A", personalCode: "49901020001");
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateMemberAsync("test-gym", new MemberUpsertRequest
            {
                FirstName = "Other",
                LastName = "Member",
                MemberCode = "MEM-B",
                PersonalCode = "49901020001",
                Status = MemberStatus.Active
            }));
    }

    [Fact]
    public async Task CreateMemberAsync_PersistsThroughUnitOfWork()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var service = CreateService(dbContext, gymId);

        var detail = await service.CreateMemberAsync("test-gym", new MemberUpsertRequest
        {
            FirstName = "  Pat  ",
            LastName = "  Riley  ",
            MemberCode = "  MEM-NEW  ",
            PersonalCode = "  49901030003  ",
            Status = MemberStatus.Active
        });

        Assert.Equal("Pat", detail.FirstName);
        Assert.Equal("Riley", detail.LastName);
        Assert.Equal("MEM-NEW", detail.MemberCode);
        Assert.Equal("49901030003", detail.PersonalCode);

        var stored = await dbContext.Members
            .Include(member => member.Person)
            .SingleAsync(member => member.MemberCode == "MEM-NEW");
        Assert.Equal(gymId, stored.GymId);
        Assert.Equal("Pat", stored.Person!.FirstName);
    }

    [Fact]
    public async Task GetMemberAsync_ReturnsNotFound_ForForeignGymId()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var foreignGymId = Guid.NewGuid();
        var foreignMember = new Member
        {
            GymId = foreignGymId,
            Person = new Person { FirstName = "Other", LastName = "Tenant" },
            MemberCode = "FOR-001",
            Status = MemberStatus.Active
        };
        dbContext.Members.Add(foreignMember);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetMemberAsync("test-gym", foreignMember.Id));
    }

    [Fact]
    public async Task GetMemberAsync_RejectsCrossMemberAccess_ForMemberRole()
    {
        var (dbContext, gymId) = await NewContextAsync();
        var someoneElse = new Member
        {
            GymId = gymId,
            Person = new Person { FirstName = "Stranger", LastName = "Member" },
            MemberCode = "MEM-OTHER",
            Status = MemberStatus.Active
        };
        dbContext.Members.Add(someoneElse);
        await dbContext.SaveChangesAsync();

        var authorization = new TestAuthorizationService(gymId)
        {
            EnsureMemberSelfAccessThrows = true
        };
        var service = CreateService(dbContext, gymId, authorization);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetMemberAsync("test-gym", someoneElse.Id));
    }

    [Fact]
    public async Task GetMembersAsync_OrdersByLastNameThenFirstName()
    {
        var (dbContext, gymId) = await NewContextAsync();
        SeedExistingMember(dbContext, gymId, "MEM-3", "11111", first: "Anna", last: "Zorn");
        SeedExistingMember(dbContext, gymId, "MEM-2", "22222", first: "Beth", last: "Adams");
        SeedExistingMember(dbContext, gymId, "MEM-1", "33333", first: "Alex", last: "Adams");
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, gymId);

        var members = (await service.GetMembersAsync("test-gym")).ToArray();

        Assert.Collection(
            members,
            first => Assert.Equal("Alex Adams", first.FullName),
            second => Assert.Equal("Beth Adams", second.FullName),
            third => Assert.Equal("Anna Zorn", third.FullName));
    }

    private static async Task<(AppDbContext dbContext, Guid gymId)> NewContextAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        var gymId = Guid.NewGuid();
        var dbContext = new AppDbContext(options, new TestGymContext(gymId));

        dbContext.Gyms.Add(new Gym
        {
            Id = gymId,
            Name = "Test Gym",
            Code = "test-gym",
            AddressLine = "Street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        });
        await dbContext.SaveChangesAsync();

        return (dbContext, gymId);
    }

    private static void SeedExistingMember(
        AppDbContext dbContext,
        Guid gymId,
        string memberCode,
        string personalCode,
        string first = "Existing",
        string last = "Member")
    {
        dbContext.Members.Add(new Member
        {
            GymId = gymId,
            Person = new Person
            {
                FirstName = first,
                LastName = last,
                PersonalCode = personalCode
            },
            MemberCode = memberCode,
            Status = MemberStatus.Active
        });
    }

    private static IMemberWorkflowService CreateService(
        AppDbContext dbContext,
        Guid gymId,
        IAuthorizationService? authorization = null)
    {
        var unitOfWork = new AppUOW(dbContext);
        var mapper = new MemberMapper();
        return new MemberWorkflowService(
            dbContext,
            new EfMemberRepository(dbContext),
            authorization ?? new TestAuthorizationService(gymId),
            new TestSubscriptionTierLimitService(),
            mapper);
    }

    private sealed class TestGymContext(Guid gymId) : IGymContext
    {
        public Guid? GymId => gymId;
        public string? GymCode => "test-gym";
        public string? ActiveRole => SharedKernel.RoleNames.GymAdmin;
        public bool IgnoreGymFilter => false;
    }

    private sealed class TestAuthorizationService(Guid gymId) : IAuthorizationService
    {
        public bool EnsureMemberSelfAccessThrows { get; set; }

        public Task<Guid> EnsureTenantAccessAsync(string gymCode, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Guid> EnsureTenantAccessAsync(string gymCode, CancellationToken cancellationToken, params string[] allowedRoles) => Task.FromResult(gymId);
        public Task<Member?> GetCurrentMemberAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Member?>(null);
        public Task<Staff?> GetCurrentStaffAsync(Guid gymIdValue, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);

        public Task EnsureMemberSelfAccessAsync(Guid gymIdValue, Guid memberId, CancellationToken cancellationToken = default)
        {
            if (EnsureMemberSelfAccessThrows)
            {
                throw new ForbiddenException("Members can only access their own profile.");
            }
            return Task.CompletedTask;
        }

        public Task EnsureBookingAccessAsync(Booking booking, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureBookingAccessAsync(Guid gymIdValue, Guid memberId, Guid trainingSessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureTrainingAttendanceAccessAsync(TrainingSession trainingSession, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureMaintenanceTaskAccessAsync(MaintenanceTask task, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestSubscriptionTierLimitService : ISubscriptionTierLimitService
    {
        public Task EnsureCanCreateMemberAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureCanCreateStaffAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureCanCreateTrainingSessionAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureCanCreateEquipmentAsync(Guid gymId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
