using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Identity;
using App.DTO.v1.Appointments;
using App.DTO.v1.Dentists;
using App.DTO.v1.Identity;
using App.DTO.v1.Patients;
using App.DTO.v1.System;
using App.DTO.v1.TreatmentRooms;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Tests.Integration;

public class IntegrationTestTenantOperations : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public IntegrationTestTenantOperations(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task PatientCrud_And_AppointmentCreate_Flow_Works()
    {
        await AuthenticateAsSystemOperatorAsync();

        var slug = $"acme-{Guid.NewGuid():N}"[..12];
        var ownerEmail = $"owner-{Guid.NewGuid():N}@tenant.test";
        const string ownerPassword = "Strong.Pass.123!";

        var onboarding = await _client.PostAsJsonAsync("/api/v1/system/onboarding/registercompany", new RegisterCompanyRequest
        {
            CompanyName = "Acme Tenant",
            CompanySlug = slug,
            OwnerEmail = ownerEmail,
            OwnerPassword = ownerPassword,
            CountryCode = "DE"
        });
        onboarding.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = ownerEmail,
            Password = ownerPassword
        });
        login.EnsureSuccessStatusCode();

        var token = await login.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var createPatientResponse = await _client.PostAsJsonAsync($"/api/v1/{slug}/patients", new CreatePatientRequest
        {
            FirstName = "Marta",
            LastName = "Kask",
            Email = "marta@example.com"
        });
        createPatientResponse.EnsureSuccessStatusCode();

        var createdPatient = await createPatientResponse.Content.ReadFromJsonAsync<PatientResponse>();
        Assert.NotNull(createdPatient);

        var listPatients = await _client.GetAsync($"/api/v1/{slug}/patients");
        listPatients.EnsureSuccessStatusCode();

        var patientList = await listPatients.Content.ReadFromJsonAsync<List<PatientResponse>>();
        Assert.NotNull(patientList);
        Assert.Contains(patientList, item => item.Id == createdPatient.Id);

        Guid dentistId;
        Guid roomId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var company = await db.Companies.IgnoreQueryFilters().AsNoTracking().SingleAsync(entity => entity.Slug == slug);

            var dentist = new Dentist
            {
                CompanyId = company.Id,
                DisplayName = "Dr House",
                LicenseNumber = "LIC-TEST-1"
            };

            var room = new TreatmentRoom
            {
                CompanyId = company.Id,
                Name = "Room A",
                Code = "A1"
            };

            db.Dentists.Add(dentist);
            db.TreatmentRooms.Add(room);
            await db.SaveChangesAsync();

            dentistId = dentist.Id;
            roomId = room.Id;
        }

        var start = DateTime.UtcNow.AddHours(2);
        var createAppointmentResponse = await _client.PostAsJsonAsync($"/api/v1/{slug}/appointments", new CreateAppointmentRequest
        {
            PatientId = createdPatient.Id,
            DentistId = dentistId,
            TreatmentRoomId = roomId,
            StartAtUtc = start,
            EndAtUtc = start.AddMinutes(30),
            Notes = "Initial check"
        });
        createAppointmentResponse.EnsureSuccessStatusCode();

        var createdAppointment = await createAppointmentResponse.Content.ReadFromJsonAsync<AppointmentResponse>();
        Assert.NotNull(createdAppointment);
        Assert.Equal(createdPatient.Id, createdAppointment.PatientId);

        var listAppointments = await _client.GetAsync($"/api/v1/{slug}/appointments");
        listAppointments.EnsureSuccessStatusCode();

        var appointmentList = await listAppointments.Content.ReadFromJsonAsync<List<AppointmentResponse>>();
        Assert.NotNull(appointmentList);
        Assert.Contains(appointmentList, item => item.Id == createdAppointment.Id);

        var deletePatient = await _client.DeleteAsync($"/api/v1/{slug}/patients/{createdPatient.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deletePatient.StatusCode);
    }

    [Fact]
    public async Task Dentist_Create_Update_Delete_Flow_Works()
    {
        await AuthenticateAsSystemOperatorAsync();

        var slug = $"clinic-{Guid.NewGuid():N}"[..12];
        var ownerEmail = $"owner-{Guid.NewGuid():N}@dentist.test";
        const string ownerPassword = "Strong.Pass.123!";

        var onboarding = await _client.PostAsJsonAsync("/api/v1/system/onboarding/registercompany", new RegisterCompanyRequest
        {
            CompanyName = "Dentist Clinic",
            CompanySlug = slug,
            OwnerEmail = ownerEmail,
            OwnerPassword = ownerPassword,
            CountryCode = "EE"
        });
        onboarding.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = ownerEmail,
            Password = ownerPassword
        });
        login.EnsureSuccessStatusCode();

        var token = await login.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var createResponse = await _client.PostAsJsonAsync($"/api/v1/{slug}/dentists", new CreateDentistRequest
        {
            DisplayName = "Dr. Helena Saar",
            LicenseNumber = "EE-DENT-555",
            Specialty = "Restorative Dentistry"
        });
        createResponse.EnsureSuccessStatusCode();

        var createdDentist = await createResponse.Content.ReadFromJsonAsync<DentistResponse>();
        Assert.NotNull(createdDentist);
        Assert.Equal("Dr. Helena Saar", createdDentist.DisplayName);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/{slug}/dentists/{createdDentist.Id}", new CreateDentistRequest
        {
            DisplayName = "Dr. Helena Saar, DDS",
            LicenseNumber = "EE-DENT-556",
            Specialty = "Prosthodontics"
        });
        updateResponse.EnsureSuccessStatusCode();

        var updatedDentist = await updateResponse.Content.ReadFromJsonAsync<DentistResponse>();
        Assert.NotNull(updatedDentist);
        Assert.Equal("Dr. Helena Saar, DDS", updatedDentist.DisplayName);
        Assert.Equal("EE-DENT-556", updatedDentist.LicenseNumber);
        Assert.Equal("Prosthodontics", updatedDentist.Specialty);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/{slug}/dentists/{createdDentist.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/api/v1/{slug}/dentists");
        listResponse.EnsureSuccessStatusCode();

        var dentists = await listResponse.Content.ReadFromJsonAsync<List<DentistResponse>>();
        Assert.NotNull(dentists);
        Assert.DoesNotContain(dentists, item => item.Id == createdDentist.Id);
    }

    [Fact]
    public async Task TreatmentRoom_Create_Update_Delete_Flow_Works()
    {
        await AuthenticateAsSystemOperatorAsync();

        var slug = $"room-{Guid.NewGuid():N}"[..12];
        var ownerEmail = $"owner-{Guid.NewGuid():N}@room.test";
        const string ownerPassword = "Strong.Pass.123!";

        var onboarding = await _client.PostAsJsonAsync("/api/v1/system/onboarding/registercompany", new RegisterCompanyRequest
        {
            CompanyName = "Room Clinic",
            CompanySlug = slug,
            OwnerEmail = ownerEmail,
            OwnerPassword = ownerPassword,
            CountryCode = "EE"
        });
        onboarding.EnsureSuccessStatusCode();

        var login = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = ownerEmail,
            Password = ownerPassword
        });
        login.EnsureSuccessStatusCode();

        var token = await login.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);

        var createResponse = await _client.PostAsJsonAsync($"/api/v1/{slug}/treatmentrooms", new CreateTreatmentRoomRequest
        {
            Name = "North Room",
            Code = "N1",
            IsActiveRoom = true
        });
        createResponse.EnsureSuccessStatusCode();

        var createdRoom = await createResponse.Content.ReadFromJsonAsync<TreatmentRoomResponse>();
        Assert.NotNull(createdRoom);
        Assert.Equal("North Room", createdRoom.Name);

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/{slug}/treatmentrooms/{createdRoom.Id}", new CreateTreatmentRoomRequest
        {
            Name = "North Surgery",
            Code = "N2",
            IsActiveRoom = false
        });
        updateResponse.EnsureSuccessStatusCode();

        var updatedRoom = await updateResponse.Content.ReadFromJsonAsync<TreatmentRoomResponse>();
        Assert.NotNull(updatedRoom);
        Assert.Equal("North Surgery", updatedRoom.Name);
        Assert.Equal("N2", updatedRoom.Code);
        Assert.False(updatedRoom.IsActiveRoom);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/{slug}/treatmentrooms/{createdRoom.Id}");
        Assert.Equal(System.Net.HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/api/v1/{slug}/treatmentrooms");
        listResponse.EnsureSuccessStatusCode();

        var rooms = await listResponse.Content.ReadFromJsonAsync<List<TreatmentRoomResponse>>();
        Assert.NotNull(rooms);
        Assert.DoesNotContain(rooms, item => item.Id == createdRoom.Id);
    }

    private async Task AuthenticateAsSystemOperatorAsync()
    {
        const string email = "sysadmin-tenant-ops@test.local";
        const string password = "Strong.Pass.123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            if (!await roleManager.RoleExistsAsync(RoleNames.SystemAdmin))
            {
                var createRole = await roleManager.CreateAsync(new AppRole { Name = RoleNames.SystemAdmin });
                Assert.True(createRole.Succeeded);
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true
                };

                var createUser = await userManager.CreateAsync(user, password);
                Assert.True(createUser.Succeeded);
            }

            if (!await userManager.IsInRoleAsync(user, RoleNames.SystemAdmin))
            {
                var addRole = await userManager.AddToRoleAsync(user, RoleNames.SystemAdmin);
                Assert.True(addRole.Succeeded);
            }
        }

        var login = await _client.PostAsJsonAsync("/api/v1/account/login", new Login
        {
            Email = email,
            Password = password
        });
        login.EnsureSuccessStatusCode();

        var token = await login.Content.ReadFromJsonAsync<JWTResponse>();
        Assert.NotNull(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Jwt);
    }
}
