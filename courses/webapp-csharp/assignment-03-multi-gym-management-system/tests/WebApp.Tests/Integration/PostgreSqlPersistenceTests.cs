using App.Domain.Common;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Tests.Integration;

[Collection(PostgreSqlIntegrationCollection.CollectionName)]
public class PostgreSqlPersistenceTests(PostgreSqlTestDatabaseFixture fixture)
{
    [RequiresDockerFact]
    public async Task TenantQueryFilter_ReturnsOnlyRowsForActiveGym()
    {
        await fixture.ResetDatabaseAsync();

        var gymA = CreateGym("alpha");
        var gymB = CreateGym("beta");

        await using (var arrangeContext = fixture.CreateDbContext(ignoreGymFilter: true))
        {
            arrangeContext.Gyms.AddRange(gymA, gymB);
            arrangeContext.Members.AddRange(
                CreateMember(gymA.Id, "MEM-ALPHA"),
                CreateMember(gymB.Id, "MEM-BETA"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var sut = fixture.CreateDbContext(gymId: gymA.Id, gymCode: gymA.Code, ignoreGymFilter: false);
        var visibleMembers = await sut.Members
            .Select(member => new { member.GymId, member.MemberCode })
            .ToArrayAsync();

        Assert.Single(visibleMembers);
        Assert.Equal(gymA.Id, visibleMembers[0].GymId);
        Assert.Equal("MEM-ALPHA", visibleMembers[0].MemberCode);
    }

    [RequiresDockerFact]
    public async Task UniqueIndex_RejectsDuplicateMemberCodeInSameGym()
    {
        await fixture.ResetDatabaseAsync();

        var gym = CreateGym("unique");

        await using (var arrangeContext = fixture.CreateDbContext(ignoreGymFilter: true))
        {
            arrangeContext.Gyms.Add(gym);
            arrangeContext.Members.Add(CreateMember(gym.Id, "MEM-001"));
            await arrangeContext.SaveChangesAsync();
        }

        await using var sut = fixture.CreateDbContext(gymId: gym.Id, gymCode: gym.Code, ignoreGymFilter: false);
        sut.Members.Add(CreateMember(gym.Id, "MEM-001"));

        await Assert.ThrowsAsync<DbUpdateException>(() => sut.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task LangStr_UsesJsonbColumnAndRoundTripsTranslations()
    {
        await fixture.ResetDatabaseAsync();

        var gym = CreateGym("langstr");
        var category = new TrainingCategory
        {
            GymId = gym.Id,
            Name = new LangStr("Strength", "en")
        };
        category.Name.SetTranslation("Joud", "et");

        await using (var arrangeContext = fixture.CreateDbContext(ignoreGymFilter: true))
        {
            arrangeContext.Gyms.Add(gym);
            arrangeContext.TrainingCategories.Add(category);
            await arrangeContext.SaveChangesAsync();
        }

        await using var sut = fixture.CreateDbContext(ignoreGymFilter: true);
        var reloaded = await sut.TrainingCategories.SingleAsync(entity => entity.Id == category.Id);

        Assert.Equal("Strength", reloaded.Name.Translate("en"));
        Assert.Equal("Joud", reloaded.Name.Translate("et-EE"));

        await using var connection = sut.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select udt_name
            from information_schema.columns
            where table_schema = 'public'
              and table_name = 'TrainingCategories'
              and column_name = 'Name'
            """;

        var udtName = (string?)await command.ExecuteScalarAsync();
        Assert.Equal("jsonb", udtName);
    }

    private static Gym CreateGym(string code)
    {
        return new Gym
        {
            Name = $"Gym {code}",
            Code = code,
            AddressLine = "Demo street 1",
            City = "Tallinn",
            PostalCode = "10111",
            Country = "Estonia"
        };
    }

    private static Member CreateMember(Guid gymId, string memberCode)
    {
        return new Member
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
    }
}
