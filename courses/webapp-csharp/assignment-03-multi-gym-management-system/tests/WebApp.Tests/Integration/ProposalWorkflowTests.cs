using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain.Common;
using App.Domain.Entities;
using App.Domain.Enums;
using App.DTO.v1.Identity;
using App.DTO.v1.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class ProposalWorkflowTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData("/client")]
    [InlineData("/client/members")]
    public async Task ReactClientFallback_ServesClientShell(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Multi-Gym Admin Client", html);
        Assert.Contains("id=\"root\"", html);
    }

    [Fact]
    public async Task TrainingSessions_HandleNullableDescriptionInListAndDetail()
    {
        Guid sessionId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == "peak-forge");
            var category = await dbContext.TrainingCategories.FirstAsync(entity => entity.GymId == gym.Id);

            var session = new TrainingSession
            {
                GymId = gym.Id,
                CategoryId = category.Id,
                Name = new LangStr("Null Description Regression", "en"),
                Description = null,
                StartAtUtc = DateTime.UtcNow.Date.AddDays(7).AddHours(16),
                EndAtUtc = DateTime.UtcNow.Date.AddDays(7).AddHours(17),
                Capacity = 8,
                BasePrice = 15m,
                CurrencyCode = "EUR",
                Status = TrainingSessionStatus.Published
            };

            dbContext.TrainingSessions.Add(session);
            await dbContext.SaveChangesAsync();
            sessionId = session.Id;
        }

        var client = await CreateAuthenticatedClientAsync("admin@peakforge.local");

        var listResponse = await client.GetAsync("/api/v1/peak-forge/training-sessions");
        listResponse.EnsureSuccessStatusCode();
        var sessions = await listResponse.Content.ReadFromJsonAsync<TrainingSessionResponse[]>();
        var listedSession = Assert.Single(sessions!, session => session.Id == sessionId);
        Assert.Null(listedSession.Description);

        var detailResponse = await client.GetAsync($"/api/v1/peak-forge/training-sessions/{sessionId}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<TrainingSessionResponse>();
        Assert.NotNull(detail);
        Assert.Null(detail!.Description);
    }

    [Fact]
    public async Task MemberBooking_RequiresPaymentReferenceWhenPaymentIsDue()
    {
        var (sessionId, memberId) = await CreatePaidSessionAndUncoveredMemberAsync();
        var client = await CreateAuthenticatedClientAsync("admin@peakforge.local");

        var response = await client.PostAsJsonAsync("/api/v1/peak-forge/bookings", new BookingCreateRequest
        {
            TrainingSessionId = sessionId,
            MemberId = memberId
        });

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Payment reference is required when payment is due.", content);
    }

    [Fact]
    public async Task TrainerAttendance_UpdateIsLimitedToAssignedTrainerSessions()
    {
        Guid assignedBookingId;
        Guid unassignedBookingId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == "peak-forge");
            var category = await dbContext.TrainingCategories.FirstAsync(entity => entity.GymId == gym.Id);
            var member = await dbContext.Members.FirstAsync(entity => entity.GymId == gym.Id);

            assignedBookingId = await dbContext.Bookings
                .Where(entity => entity.GymId == gym.Id)
                .Select(entity => entity.Id)
                .FirstAsync();

            var unassignedSession = new TrainingSession
            {
                GymId = gym.Id,
                CategoryId = category.Id,
                Name = new LangStr("Unassigned Trainer Regression", "en"),
                StartAtUtc = DateTime.UtcNow.Date.AddDays(8).AddHours(18),
                EndAtUtc = DateTime.UtcNow.Date.AddDays(8).AddHours(19),
                Capacity = 10,
                BasePrice = 20m,
                CurrencyCode = "EUR",
                Status = TrainingSessionStatus.Published
            };

            var unassignedBooking = new Booking
            {
                GymId = gym.Id,
                TrainingSession = unassignedSession,
                MemberId = member.Id,
                Status = BookingStatus.Booked,
                ChargedPrice = 0m,
                CurrencyCode = "EUR",
                PaymentRequired = false
            };

            dbContext.AddRange(unassignedSession, unassignedBooking);
            await dbContext.SaveChangesAsync();
            unassignedBookingId = unassignedBooking.Id;
        }

        var client = await CreateAuthenticatedClientAsync("trainer@peakforge.local");

        var allowedResponse = await client.PutAsJsonAsync(
            $"/api/v1/peak-forge/bookings/{assignedBookingId}/attendance",
            new AttendanceUpdateRequest { Status = BookingStatus.Attended });

        allowedResponse.EnsureSuccessStatusCode();
        var updated = await allowedResponse.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(BookingStatus.Attended, updated!.Status);

        var forbiddenResponse = await client.PutAsJsonAsync(
            $"/api/v1/peak-forge/bookings/{unassignedBookingId}/attendance",
            new AttendanceUpdateRequest { Status = BookingStatus.Attended });

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    [Fact]
    public async Task CaretakerStatus_UpdateIsLimitedToAssignedTasks()
    {
        Guid assignedTaskId;
        Guid unassignedTaskId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == "peak-forge");
            var equipment = await dbContext.Equipment.FirstAsync(entity => entity.GymId == gym.Id);
            var adminStaff = await dbContext.Staff.FirstAsync(entity => entity.GymId == gym.Id && entity.StaffCode == "STF-AD-001");

            assignedTaskId = await dbContext.MaintenanceTasks
                .Where(entity => entity.GymId == gym.Id && entity.AssignedStaffId != null)
                .Select(entity => entity.Id)
                .FirstAsync();

            var unassignedTask = new MaintenanceTask
            {
                GymId = gym.Id,
                EquipmentId = equipment.Id,
                AssignedStaffId = adminStaff.Id,
                CreatedByStaffId = adminStaff.Id,
                TaskType = MaintenanceTaskType.Scheduled,
                Priority = MaintenancePriority.High,
                Status = MaintenanceTaskStatus.Open,
                DueAtUtc = DateTime.UtcNow.Date.AddDays(9).AddHours(12),
                Notes = "Assigned to another staff member for authorization regression."
            };

            dbContext.MaintenanceTasks.Add(unassignedTask);
            await dbContext.SaveChangesAsync();
            unassignedTaskId = unassignedTask.Id;
        }

        var client = await CreateAuthenticatedClientAsync("caretaker@peakforge.local");

        var allowedResponse = await client.PutAsJsonAsync(
            $"/api/v1/peak-forge/maintenance-tasks/{assignedTaskId}/status",
            new MaintenanceStatusUpdateRequest
            {
                Status = MaintenanceTaskStatus.InProgress,
                Notes = "Started from integration test."
            });

        allowedResponse.EnsureSuccessStatusCode();
        var updated = await allowedResponse.Content.ReadFromJsonAsync<MaintenanceTaskResponse>();
        Assert.Equal(MaintenanceTaskStatus.InProgress, updated!.Status);
        Assert.NotNull(updated.StartedAtUtc);

        var forbiddenResponse = await client.PutAsJsonAsync(
            $"/api/v1/peak-forge/maintenance-tasks/{unassignedTaskId}/status",
            new MaintenanceStatusUpdateRequest { Status = MaintenanceTaskStatus.Done });

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = "Gym123!"
        });

        loginResponse.EnsureSuccessStatusCode();
        var loginPayload = (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.Jwt);
        return client;
    }

    private async Task<(Guid SessionId, Guid MemberId)> CreatePaidSessionAndUncoveredMemberAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var gym = await dbContext.Gyms.FirstAsync(entity => entity.Code == "peak-forge");
        var category = await dbContext.TrainingCategories.FirstAsync(entity => entity.GymId == gym.Id);
        var person = new Person
        {
            FirstName = "Payment",
            LastName = "Required",
            PersonalCode = $"PAY-{Guid.NewGuid():N}"[..16]
        };
        var member = new Member
        {
            GymId = gym.Id,
            PersonId = person.Id,
            Person = person,
            MemberCode = $"PAY-{Guid.NewGuid():N}"[..12],
            Status = MemberStatus.Active
        };
        var session = new TrainingSession
        {
            GymId = gym.Id,
            CategoryId = category.Id,
            Name = new LangStr("Paid Booking Regression", "en"),
            StartAtUtc = DateTime.UtcNow.Date.AddDays(6).AddHours(17),
            EndAtUtc = DateTime.UtcNow.Date.AddDays(6).AddHours(18),
            Capacity = 6,
            BasePrice = 25m,
            CurrencyCode = "EUR",
            Status = TrainingSessionStatus.Published
        };

        dbContext.AddRange(person, member, session);
        await dbContext.SaveChangesAsync();
        return (session.Id, member.Id);
    }
}
