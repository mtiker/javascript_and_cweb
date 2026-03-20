using App.BLL.Contracts.Appointments;
using App.BLL.Exceptions;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestAppointmentService
{
    [Fact]
    public async Task CreateAsync_ThrowsValidation_WhenDentistHasOverlap()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"appointment-overlap-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var dentist = new Dentist { CompanyId = companyId, DisplayName = "Dr One", LicenseNumber = "LIC-001" };
        var room = new TreatmentRoom { CompanyId = companyId, Name = "Room 1", Code = "R1" };

        db.Patients.Add(patient);
        db.Dentists.Add(dentist);
        db.TreatmentRooms.Add(room);

        var existing = new Appointment
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            DentistId = dentist.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            EndAtUtc = DateTime.UtcNow.AddHours(2),
            Status = AppointmentStatus.Scheduled
        };
        db.Appointments.Add(existing);

        await db.SaveChangesAsync();

        var accessService = new TenantAccessService(db);
        var service = new AppointmentService(db, accessService);

        await Assert.ThrowsAsync<ValidationAppException>(async () =>
            await service.CreateAsync(
                userId,
                new CreateAppointmentCommand(
                    patient.Id,
                    dentist.Id,
                    room.Id,
                    existing.StartAtUtc.AddMinutes(15),
                    existing.EndAtUtc.AddMinutes(15),
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task RecordClinicalWorkAsync_CreatesTreatmentUpdatesToothRecordAndCompletesAppointment()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"appointment-clinical-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var dentist = new Dentist { CompanyId = companyId, DisplayName = "Dr One", LicenseNumber = "LIC-001" };
        var room = new TreatmentRoom { CompanyId = companyId, Name = "Room 1", Code = "R1" };
        var treatmentType = new TreatmentType
        {
            CompanyId = companyId,
            Name = "Composite Filling",
            BasePrice = 150m,
            DefaultDurationMinutes = 30
        };
        var appointment = new Appointment
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            DentistId = dentist.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(-1),
            EndAtUtc = DateTime.UtcNow,
            Status = AppointmentStatus.Confirmed
        };

        db.Patients.Add(patient);
        db.Dentists.Add(dentist);
        db.TreatmentRooms.Add(room);
        db.TreatmentTypes.Add(treatmentType);
        db.Appointments.Add(appointment);

        await db.SaveChangesAsync();

        var service = new AppointmentService(db, new TenantAccessService(db));

        var result = await service.RecordClinicalWorkAsync(
            userId,
            new RecordAppointmentClinicalCommand(
                appointment.Id,
                DateTime.UtcNow,
                true,
                new[]
                {
                    new RecordAppointmentClinicalItemCommand(
                        treatmentType.Id,
                        null,
                        11,
                        ToothConditionStatus.Filled,
                        null,
                        "Occlusal composite restoration")
                }),
            CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, result.RecordedItemCount);

        var treatment = Assert.Single(db.Treatments);
        Assert.Equal(appointment.Id, treatment.AppointmentId);
        Assert.Equal(11, treatment.ToothNumber);
        Assert.Equal(150m, treatment.Price);

        var toothRecord = Assert.Single(db.ToothRecords.Where(entity => entity.PatientId == patient.Id && entity.ToothNumber == 11));
        Assert.Equal(ToothConditionStatus.Filled, toothRecord.Condition);
        Assert.Equal("Occlusal composite restoration", toothRecord.Notes);
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public async Task RecordClinicalWorkAsync_AssignsPlanItem_WhenLinkedPlanItemIsValid()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"appointment-planitem-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var dentist = new Dentist { CompanyId = companyId, DisplayName = "Dr One", LicenseNumber = "LIC-001" };
        var room = new TreatmentRoom { CompanyId = companyId, Name = "Room 1", Code = "R1" };
        var treatmentType = new TreatmentType
        {
            CompanyId = companyId,
            Name = "Composite Filling",
            BasePrice = 150m,
            DefaultDurationMinutes = 30
        };
        var appointment = new Appointment
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            DentistId = dentist.Id,
            TreatmentRoomId = room.Id,
            StartAtUtc = DateTime.UtcNow.AddHours(-2),
            EndAtUtc = DateTime.UtcNow.AddHours(-1),
            Status = AppointmentStatus.Confirmed
        };
        var plan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            DentistId = dentist.Id,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
            Status = TreatmentPlanStatus.Pending
        };
        var planItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = plan,
            TreatmentTypeId = treatmentType.Id,
            Sequence = 1,
            Urgency = UrgencyLevel.High,
            EstimatedPrice = 150m
        };

        db.Patients.Add(patient);
        db.Dentists.Add(dentist);
        db.TreatmentRooms.Add(room);
        db.TreatmentTypes.Add(treatmentType);
        db.Appointments.Add(appointment);
        db.TreatmentPlans.Add(plan);
        db.PlanItems.Add(planItem);
        await db.SaveChangesAsync();

        var service = new AppointmentService(db, new TenantAccessService(db));

        await service.RecordClinicalWorkAsync(
            userId,
            new RecordAppointmentClinicalCommand(
                appointment.Id,
                DateTime.UtcNow,
                false,
                new[]
                {
                    new RecordAppointmentClinicalItemCommand(
                        treatmentType.Id,
                        planItem.Id,
                        11,
                        ToothConditionStatus.Filled,
                        null,
                        "Linked to plan item")
                }),
            CancellationToken.None);

        var treatment = Assert.Single(db.Treatments);
        Assert.Equal(planItem.Id, treatment.PlanItemId);
    }
}
