using App.BLL.Contracts.Patients;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestPatientService
{
    [Fact]
    public async Task CreateAsync_CreatesFullToothChartForNewPatient()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"patient-create-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, tenantProvider);

        var createdPatient = await service.CreateAsync(
            userId,
            new CreatePatientCommand("Jane", "Doe", null, null, "jane@example.test", null),
            CancellationToken.None);

        var profile = await service.GetProfileAsync(userId, createdPatient.Id, CancellationToken.None);

        Assert.Equal(32, profile.Teeth.Count);
        Assert.All(profile.Teeth, tooth => Assert.Equal("Healthy", tooth.Condition));
        Assert.Contains(profile.Teeth, tooth => tooth.ToothNumber == 11);
        Assert.Contains(profile.Teeth, tooth => tooth.ToothNumber == 48);
    }

    [Fact]
    public async Task GetProfileAsync_BackfillsMissingTeethAndIncludesToothHistory()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"patient-profile-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });

        var patient = new Patient
        {
            CompanyId = companyId,
            FirstName = "Liis",
            LastName = "Kask",
            Email = "liis@example.test"
        };

        var treatmentType = new TreatmentType
        {
            CompanyId = companyId,
            Name = "Composite Filling",
            BasePrice = 140m,
            DefaultDurationMinutes = 30
        };

        db.Patients.Add(patient);
        db.TreatmentTypes.Add(treatmentType);
        db.ToothRecords.Add(new ToothRecord
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            ToothNumber = 11,
            Condition = ToothConditionStatus.Filled,
            Notes = "Recent composite."
        });
        db.Treatments.Add(new Treatment
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            TreatmentTypeId = treatmentType.Id,
            ToothNumber = 11,
            PerformedAtUtc = new DateTime(2026, 03, 10, 9, 30, 0, DateTimeKind.Utc),
            Price = 140m,
            Notes = "Palatal surface restored."
        });

        await db.SaveChangesAsync();

        var service = CreateService(db, tenantProvider);

        var profile = await service.GetProfileAsync(userId, patient.Id, CancellationToken.None);

        Assert.Equal(32, profile.Teeth.Count);

        var tooth11 = Assert.Single(profile.Teeth, tooth => tooth.ToothNumber == 11);
        Assert.Equal("Filled", tooth11.Condition);
        Assert.Equal("Composite Filling", tooth11.LastTreatmentTypeName);
        Assert.Equal("Palatal surface restored.", tooth11.LastTreatmentNotes);
        Assert.Single(tooth11.History);
    }

    private static PatientService CreateService(App.DAL.EF.AppDbContext db, TestTenantProvider tenantProvider)
    {
        return new PatientService(
            db,
            new TenantAccessService(db),
            new SubscriptionPolicyService(db, tenantProvider));
    }
}
