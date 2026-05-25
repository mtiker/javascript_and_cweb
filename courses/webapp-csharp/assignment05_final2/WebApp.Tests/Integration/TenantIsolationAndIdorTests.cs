using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain.Entities;
using Shared.Contracts.Enums;
using Shared.Contracts.Dtos.v1.Bookings;
using Shared.Contracts.Dtos.v1.Identity;
using Shared.Contracts.Dtos.v1.MaintenanceTasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class TenantIsolationAndIdorTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    // ── Scenario 1: URL gymCode mismatch ──────────────────────────────────────

    [Fact]
    public async Task Trainer_CannotAccess_DifferentGym_TrainingSessions()
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, "trainer@peakforge.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        Assert.Equal("peak-forge", token.ActiveGymCode);

        var response = await client.GetAsync("/api/v1/north-star/training-sessions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Caretaker_CannotAccess_DifferentGym_MaintenanceTasks()
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, "caretaker@peakforge.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        Assert.Equal("peak-forge", token.ActiveGymCode);

        var response = await client.GetAsync("/api/v1/north-star/maintenance-tasks");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Scenario 2: Member cannot access another member's workspace ───────────

    [Fact]
    public async Task Member_CannotAccess_AnotherMembersWorkspace()
    {
        Guid otherMemberId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var memberPersonId = await db.Users
                .Where(u => u.Email == "member@peakforge.local")
                .Select(u => u.PersonId)
                .FirstAsync();
            otherMemberId = await db.Members
                .Where(m => m.PersonId != memberPersonId)
                .Select(m => m.Id)
                .FirstAsync();
        }

        var client = factory.CreateClient();
        var token = await LoginAsync(client, "member@peakforge.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var response = await client.GetAsync($"/api/v1/peak-forge/member-workspace/members/{otherMemberId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_CanAccess_OwnWorkspace()
    {
        Guid ownMemberId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var memberPersonId = await db.Users
                .Where(u => u.Email == "member@peakforge.local")
                .Select(u => u.PersonId)
                .FirstAsync();
            ownMemberId = await db.Members
                .Where(m => m.PersonId == memberPersonId)
                .Select(m => m.Id)
                .FirstAsync();
        }

        var client = factory.CreateClient();
        var token = await LoginAsync(client, "member@peakforge.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var response = await client.GetAsync($"/api/v1/peak-forge/member-workspace/members/{ownMemberId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Scenario 3: Trainer cannot update attendance for an unassigned session ─

    [Fact]
    public async Task Trainer_CannotUpdateAttendance_ForUnassignedSession()
    {
        // Find a booking on a session that does NOT have a training shift for the seed trainer
        Guid unassignedSessionBookingId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var trainerPersonId = await db.Users
                .Where(u => u.Email == "trainer@peakforge.local")
                .Select(u => u.PersonId)
                .FirstAsync();
            var trainerStaffId = await db.Staff
                .Where(s => s.PersonId == trainerPersonId)
                .Select(s => s.Id)
                .FirstAsync();

            unassignedSessionBookingId = await db.Bookings
                .Where(b => b.TrainingSession!.TrainerStaffId != trainerStaffId)
                .Select(b => b.Id)
                .FirstAsync();
        }

        var client = factory.CreateClient();
        var token = await LoginAsync(client, "trainer@peakforge.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/peak-forge/bookings/{unassignedSessionBookingId}/attendance",
            new AttendanceUpdateRequest { Status = BookingStatus.Attended });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Scenario 4: Caretaker cannot update an unassigned maintenance task ─────

    [Fact]
    public async Task Caretaker_CannotUpdateStatus_ForUnassignedTask()
    {
        Guid unassignedTaskId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var gym = await db.Gyms.FirstAsync(g => g.Code == "peak-forge");
            var equipment = await db.Equipment.FirstAsync(e => e.GymId == gym.Id);

            // Create a task assigned to adminStaff (personal code 49102020002 = Marek Mets)
            var adminStaffId = await db.Staff
                .Where(s => s.GymId == gym.Id && s.Person!.PersonalCode == "49102020002")
                .Select(s => s.Id)
                .FirstAsync();

            var task = new MaintenanceTask
            {
                GymId = gym.Id,
                EquipmentId = equipment.Id,
                AssignedStaffId = adminStaffId,
                TaskType = MaintenanceTaskType.Scheduled,
                Priority = MaintenancePriority.Low,
                Status = MaintenanceTaskStatus.Open,
                Notes = "IDOR verification – assigned to admin, not caretaker"
            };
            db.MaintenanceTasks.Add(task);
            await db.SaveChangesAsync();
            unassignedTaskId = task.Id;
        }

        var client = factory.CreateClient();
        var token = await LoginAsync(client, "caretaker@peakforge.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/peak-forge/maintenance-tasks/{unassignedTaskId}/status",
            new MaintenanceStatusUpdateRequest { Status = MaintenanceTaskStatus.InProgress });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── Scenario 5: Tenant user cannot access /api/v1/system endpoints ─────────

    // ── Scenario 6: System role cannot bypass tenant context ──────────────────

    // ── Baseline: unauthenticated returns 401 ─────────────────────────────────

    [Fact]
    public async Task Unauthenticated_Returns401_OnTenantEndpoint()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/v1/peak-forge/members");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Cross-tenant ID manipulation (IDOR via resource ID) ──────────────────
    //
    // A GymAdmin at north-star must not be able to mutate resources that belong
    // to peak-forge by supplying a peak-forge resource ID through the north-star
    // endpoint.  The fix scopes entity lookups by gymId so the resource is not
    // found (404) rather than silently modified.

    [Fact]
    public async Task GymAdmin_AtNorthStar_CannotCancel_PeakForgeBooking_ViaIdManipulation()
    {
        Guid peakForgeBookingId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var peakForge = await db.Gyms.FirstAsync(g => g.Code == "peak-forge");
            peakForgeBookingId = await db.Bookings
                .Where(b => b.GymId == peakForge.Id && b.Status == BookingStatus.Booked)
                .Select(b => b.Id)
                .FirstAsync();
        }

        var client = factory.CreateClient();
        var token = await LoginAsync(client, "multigym.admin@gym.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        // multigym.admin logs in with north-star as active gym (alphabetically first)
        Assert.Equal("north-star", token.ActiveGymCode);

        var response = await client.DeleteAsync($"/api/v1/north-star/bookings/{peakForgeBookingId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GymAdmin_AtNorthStar_CannotUpdateStatus_PeakForgeMaintenanceTask_ViaIdManipulation()
    {
        Guid peakForgeTaskId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var peakForge = await db.Gyms.FirstAsync(g => g.Code == "peak-forge");
            peakForgeTaskId = await db.MaintenanceTasks
                .Where(t => t.GymId == peakForge.Id)
                .Select(t => t.Id)
                .FirstAsync();
        }

        var client = factory.CreateClient();
        var token = await LoginAsync(client, "multigym.admin@gym.local", "GymStrong123!");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        Assert.Equal("north-star", token.ActiveGymCode);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/north-star/maintenance-tasks/{peakForgeTaskId}/status",
            new MaintenanceStatusUpdateRequest { Status = MaintenanceTaskStatus.Done });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JwtResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/v1/account/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();
        return (await loginResponse.Content.ReadFromJsonAsync<JwtResponse>())!;
    }
}
