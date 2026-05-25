using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain.Entities;
using Base.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.Identity;
using Shared.Contracts.Enums;
using Shared.Contracts.Mediator.Diagnostics;

namespace WebApp.Tests.Integration;

/// <summary>
/// Exercises the Phase-3 mediator path in production code: the Training module
/// publishes <c>BookingConfirmedNotification</c> after a booking is persisted,
/// and the Memberships module's <c>BookingConfirmedHandler</c> reacts by
/// incrementing <c>Membership.SessionsConsumed</c>. Proves a real cross-module
/// workflow flows through <c>IMediator</c> rather than a direct project ref.
/// </summary>
public class CrossModuleMediatorTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const string GymCode = "peak-forge";

    [Fact]
    public async Task ConfirmingABooking_PublishesNotification_AndMembershipsIncrementsConsumption()
    {
        var (sessionId, memberId, membershipId) = await CreateSessionMemberAndActiveMembershipAsync();

        var client = await CreateAuthenticatedAdminClientAsync();

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/{GymCode}/bookings",
            new BookingCreateRequest
            {
                TrainingSessionId = sessionId,
                MemberId = memberId
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var booking = (await createResponse.Content.ReadFromJsonAsync<BookingResponse>())!;

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var refreshed = await dbContext.Memberships.AsNoTracking().FirstAsync(m => m.Id == membershipId);
        Assert.Equal(1, refreshed.SessionsConsumed);

        var recorder = scope.ServiceProvider.GetRequiredService<IModuleEventRecorder>();
        Assert.Contains(
            recorder.Snapshot(),
            entry => entry == $"Modules.Memberships<-BookingConfirmed:{booking.Id}");
    }

    [Fact]
    public async Task ConfirmingABooking_ForMemberWithoutActiveMembership_RecordsNoActiveMarker_AndDoesNotThrow()
    {
        var (sessionId, memberId) = await CreatePaidSessionAndUncoveredMemberAsync();
        var client = await CreateAuthenticatedAdminClientAsync();

        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/{GymCode}/bookings",
            new BookingCreateRequest
            {
                TrainingSessionId = sessionId,
                MemberId = memberId,
                PaymentReference = $"PAY-{Guid.NewGuid():N}"[..16]
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var booking = (await createResponse.Content.ReadFromJsonAsync<BookingResponse>())!;

        using var scope = factory.Services.CreateScope();
        var recorder = scope.ServiceProvider.GetRequiredService<IModuleEventRecorder>();
        Assert.Contains(
            recorder.Snapshot(),
            entry => entry == $"Modules.Memberships<-BookingConfirmed:no-active:{booking.Id}");
    }

    private async Task<HttpClient> CreateAuthenticatedAdminClientAsync()
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/account/login",
            new LoginRequest { Email = "admin@peakforge.local", Password = "GymStrong123!" });

        loginResponse.EnsureSuccessStatusCode();
        var payload = (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Jwt);
        return client;
    }

    private async Task<(Guid SessionId, Guid MemberId, Guid MembershipId)> CreateSessionMemberAndActiveMembershipAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == GymCode);
        var category = await dbContext.TrainingCategories.FirstAsync(entity => entity.GymId == gym.Id);

        var package = new MembershipPackage
        {
            GymId = gym.Id,
            Name = new LangStr("Mediator Test Package", "en"),
            PackageType = MembershipPackageType.Monthly,
            DurationValue = 30,
            DurationUnit = DurationUnit.Day,
            BasePrice = 0m,
            CurrencyCode = "EUR",
            IsTrainingFree = true,
            ValidFrom = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1))
        };

        var person = new Person
        {
            FirstName = "Mediator",
            LastName = "Member",
            PersonalCode = $"MED-{Guid.NewGuid():N}"[..16]
        };

        var member = new Member
        {
            GymId = gym.Id,
            PersonId = person.Id,
            Person = person,
            MemberCode = $"MED-{Guid.NewGuid():N}"[..12],
            Status = MemberStatus.Active
        };

        var membership = new Membership
        {
            GymId = gym.Id,
            MemberId = member.Id,
            Member = member,
            MembershipPackageId = package.Id,
            MembershipPackage = package,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30)),
            PriceAtPurchase = 0m,
            CurrencyCode = "EUR",
            Status = MembershipStatus.Active
        };

        var session = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = category.Id,
            Name = new LangStr("Mediator Booking Session", "en"),
            StartAtUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(10),
            EndAtUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(11),
            Capacity = 8,
            BasePrice = 0m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        dbContext.AddRange(package, person, member, membership, session);
        await dbContext.SaveChangesAsync();
        return (session.Id, member.Id, membership.Id);
    }

    private async Task<(Guid SessionId, Guid MemberId)> CreatePaidSessionAndUncoveredMemberAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == GymCode);
        var category = await dbContext.TrainingCategories.FirstAsync(entity => entity.GymId == gym.Id);

        var person = new Person
        {
            FirstName = "Mediator",
            LastName = "NoMembership",
            PersonalCode = $"NOM-{Guid.NewGuid():N}"[..16]
        };

        var member = new Member
        {
            GymId = gym.Id,
            PersonId = person.Id,
            Person = person,
            MemberCode = $"NOM-{Guid.NewGuid():N}"[..12],
            Status = MemberStatus.Active
        };

        var session = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = category.Id,
            Name = new LangStr("Mediator No-Membership Session", "en"),
            StartAtUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(10),
            EndAtUtc = DateTime.UtcNow.Date.AddDays(3).AddHours(11),
            Capacity = 6,
            BasePrice = 18m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        dbContext.AddRange(person, member, session);
        await dbContext.SaveChangesAsync();
        return (session.Id, member.Id);
    }
}
