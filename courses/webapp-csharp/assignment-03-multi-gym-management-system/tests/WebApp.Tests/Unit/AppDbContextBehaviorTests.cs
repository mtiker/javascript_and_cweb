using System.Security.Claims;
using App.DAL.EF;
using App.DAL.EF.Tenant;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Unit;

public class AppDbContextBehaviorTests
{
    [Fact]
    public async Task SaveChangesAsync_WritesAuditLogs_ForTenantEntityCreateAndUpdate()
    {
        var actorUserId = Guid.NewGuid();
        var gymContext = new TestGymContext
        {
            GymId = null,
            IgnoreGymFilter = true
        };

        await using var dbContext = CreateDbContext(gymContext, actorUserId);
        var gym = CreateGym("audit-gym");
        var member = CreateMember(gym.Id, "MEM-AUDIT");

        dbContext.Gyms.Add(gym);
        dbContext.Members.Add(member);
        await dbContext.SaveChangesAsync();

        member.Status = MemberStatus.Suspended;
        await dbContext.SaveChangesAsync();

        var logs = await dbContext.AuditLogs
            .Where(log => log.EntityName == nameof(Member))
            .OrderBy(log => log.ChangedAtUtc)
            .ToArrayAsync();

        Assert.Equal(2, logs.Length);
        Assert.Equal("Added", logs[0].Action);
        Assert.Equal("Modified", logs[1].Action);
        Assert.All(logs, log =>
        {
            Assert.Equal(actorUserId, log.ActorUserId);
            Assert.Equal(gym.Id, log.GymId);
            Assert.Equal(member.Id, log.EntityId);
            Assert.False(string.IsNullOrWhiteSpace(log.ChangesJson));
        });
    }

    [Fact]
    public async Task SoftDeleteQueryFilter_HidesDeletedTenantRows()
    {
        var gym = CreateGym("soft-delete-gym");
        var gymContext = new TestGymContext
        {
            GymId = gym.Id,
            GymCode = gym.Code,
            IgnoreGymFilter = false
        };

        await using var dbContext = CreateDbContext(gymContext);
        var activeMember = CreateMember(gym.Id, "MEM-ACTIVE");
        var deletedMember = CreateMember(gym.Id, "MEM-DELETED");

        dbContext.Gyms.Add(gym);
        dbContext.Members.AddRange(activeMember, deletedMember);
        await dbContext.SaveChangesAsync();

        dbContext.Members.Remove(deletedMember);
        await dbContext.SaveChangesAsync();

        var visibleMembers = await dbContext.Members
            .Select(member => member.Id)
            .ToArrayAsync();

        Assert.DoesNotContain(deletedMember.Id, visibleMembers);
        Assert.Contains(activeMember.Id, visibleMembers);

        var storedDeletedMember = await dbContext.Members
            .IgnoreQueryFilters()
            .SingleAsync(member => member.Id == deletedMember.Id);

        Assert.True(storedDeletedMember.IsDeleted);
        Assert.NotNull(storedDeletedMember.DeletedAtUtc);
    }

    private static AppDbContext CreateDbContext(TestGymContext gymContext, Guid? actorUserId = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DbBehavior-{Guid.NewGuid():N}")
            .Options;

        var httpContext = new DefaultHttpContext();
        if (actorUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, actorUserId.Value.ToString())
            ], "Test"));
        }

        return new AppDbContext(
            options,
            gymContext,
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });
    }

    private static Gym CreateGym(string code) =>
        new()
        {
            Name = $"Gym {code}",
            Code = code,
            AddressLine = "Demo street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        };

    private static Member CreateMember(Guid gymId, string memberCode) =>
        new()
        {
            GymId = gymId,
            MemberCode = memberCode,
            Person = new Person
            {
                FirstName = "Test",
                LastName = memberCode,
                PersonalCode = $"{Guid.NewGuid():N}"[..11]
            }
        };

    private sealed class TestGymContext : IGymContext
    {
        public Guid? GymId { get; set; }
        public string? GymCode { get; set; }
        public string? ActiveRole => null;
        public bool IgnoreGymFilter { get; set; }
    }
}
