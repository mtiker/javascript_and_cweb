using System.Net.Http.Headers;
using System.Net.Http.Json;
using App.DAL.EF;
using App.Domain.Entities;
using App.DTO.v1.Appointments;
using App.DTO.v1.Identity;
using App.DTO.v1.Patients;
using App.DTO.v1.System;
using Microsoft.AspNetCore.Mvc.Testing;
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
}
