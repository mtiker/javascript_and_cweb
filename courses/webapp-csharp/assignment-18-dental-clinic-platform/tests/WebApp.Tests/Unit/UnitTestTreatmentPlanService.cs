using App.BLL.Contracts;
using App.BLL.Contracts.TreatmentPlans;
using App.BLL.Exceptions;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestTreatmentPlanService
{
    [Fact]
    public async Task CreateAsync_CreatesDraftPlanWithSortedItems()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"plan-create-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var dentist = new Dentist { CompanyId = companyId, DisplayName = "Dr One", LicenseNumber = "LIC-001" };
        var treatmentTypeA = new TreatmentType { CompanyId = companyId, Name = "Exam", BasePrice = 50m, DefaultDurationMinutes = 15 };
        var treatmentTypeB = new TreatmentType { CompanyId = companyId, Name = "Filling", BasePrice = 120m, DefaultDurationMinutes = 30 };

        db.Patients.Add(patient);
        db.Dentists.Add(dentist);
        db.TreatmentTypes.AddRange(treatmentTypeA, treatmentTypeB);
        await db.SaveChangesAsync();

        var service = new TreatmentPlanService(db, new TenantAccessService(db));

        var result = await service.CreateAsync(
            userId,
            new CreateTreatmentPlanCommand(
                patient.Id,
                dentist.Id,
                new[]
                {
                    new TreatmentPlanItemCommand(treatmentTypeB.Id, 2, "High", 120m),
                    new TreatmentPlanItemCommand(treatmentTypeA.Id, 1, "Low", 50m)
                }),
            CancellationToken.None);

        Assert.Equal(TreatmentPlanStatus.Draft.ToString(), result.Status);
        Assert.False(result.IsLocked);
        Assert.Equal([1, 2], result.Items.Select(entity => entity.Sequence).ToArray());

        var savedPlan = await db.TreatmentPlans
            .Include(entity => entity.Items)
            .SingleAsync(entity => entity.Id == result.Id);

        Assert.Equal(patient.Id, savedPlan.PatientId);
        Assert.Equal(dentist.Id, savedPlan.DentistId);
        Assert.Equal(2, savedPlan.Items.Count);
    }

    [Fact]
    public async Task RecordPlanItemDecision_SetsPartiallyAccepted_WhenAcceptedAndPendingItemsExist()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"plan-status-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });

        var plan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = Guid.NewGuid(),
            Status = TreatmentPlanStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        var firstItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlanId = plan.Id,
            TreatmentTypeId = Guid.NewGuid(),
            Sequence = 1,
            Urgency = UrgencyLevel.High,
            EstimatedPrice = 100
        };

        var secondItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlanId = plan.Id,
            TreatmentTypeId = Guid.NewGuid(),
            Sequence = 2,
            Urgency = UrgencyLevel.Low,
            EstimatedPrice = 50
        };

        db.TreatmentPlans.Add(plan);
        db.PlanItems.AddRange(firstItem, secondItem);
        await db.SaveChangesAsync();

        var accessService = new TenantAccessService(db);
        var service = new TreatmentPlanService(db, accessService);

        var result = await service.RecordPlanItemDecisionAsync(
            userId,
            new RecordPlanItemDecisionCommand(plan.Id, firstItem.Id, PlanItemDecision.Accepted, "urgent accepted"),
            CancellationToken.None);

        Assert.Equal("PartiallyAccepted", result.PlanStatus);
        Assert.Equal(PlanItemDecision.Accepted.ToString(), result.ItemDecision);
    }

    [Fact]
    public async Task RecordPlanItemDecision_ThrowsNotFound_WhenItemMissing()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"plan-notfound-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });

        var plan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = Guid.NewGuid(),
            Status = TreatmentPlanStatus.Pending,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        db.TreatmentPlans.Add(plan);
        await db.SaveChangesAsync();

        var accessService = new TenantAccessService(db);
        var service = new TreatmentPlanService(db, accessService);

        await Assert.ThrowsAsync<NotFoundException>(async () =>
            await service.RecordPlanItemDecisionAsync(
                userId,
                new RecordPlanItemDecisionCommand(plan.Id, Guid.NewGuid(), PlanItemDecision.Accepted, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task RecordPlanItemDecision_ThrowsValidation_WhenPlanIsStillDraft()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"plan-draft-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });

        var plan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = Guid.NewGuid(),
            Status = TreatmentPlanStatus.Draft
        };

        var item = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = plan,
            TreatmentTypeId = Guid.NewGuid(),
            Sequence = 1,
            Urgency = UrgencyLevel.High,
            EstimatedPrice = 100m
        };

        db.TreatmentPlans.Add(plan);
        db.PlanItems.Add(item);
        await db.SaveChangesAsync();

        var service = new TreatmentPlanService(db, new TenantAccessService(db));

        await Assert.ThrowsAsync<ValidationAppException>(async () =>
            await service.RecordPlanItemDecisionAsync(
                userId,
                new RecordPlanItemDecisionCommand(plan.Id, item.Id, PlanItemDecision.Accepted, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsValidation_WhenReplacingItemsOnSubmittedPlan()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"plan-locked-update-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var existingType = new TreatmentType { CompanyId = companyId, Name = "Exam", BasePrice = 50m, DefaultDurationMinutes = 15 };
        var replacementType = new TreatmentType { CompanyId = companyId, Name = "Filling", BasePrice = 100m, DefaultDurationMinutes = 30 };
        var plan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1),
            Status = TreatmentPlanStatus.Pending
        };
        var existingItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = plan,
            TreatmentTypeId = existingType.Id,
            Sequence = 1,
            Urgency = UrgencyLevel.Low,
            EstimatedPrice = 50m
        };

        db.Patients.Add(patient);
        db.TreatmentTypes.AddRange(existingType, replacementType);
        db.TreatmentPlans.Add(plan);
        db.PlanItems.Add(existingItem);
        await db.SaveChangesAsync();

        var service = new TreatmentPlanService(db, new TenantAccessService(db));

        await Assert.ThrowsAsync<ValidationAppException>(async () =>
            await service.UpdateAsync(
                userId,
                new UpdateTreatmentPlanCommand(
                    plan.Id,
                    null,
                    null,
                    new[]
                    {
                        new TreatmentPlanItemCommand(replacementType.Id, 1, "High", 100m)
                    }),
                CancellationToken.None));
    }

    [Fact]
    public async Task ListOpenItemsAsync_ReturnsOnlyPendingItemsFromSubmittedPlans()
    {
        var tenantProvider = new TestTenantProvider();
        var companyId = Guid.NewGuid();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);

        await using var db = TestDbContextFactory.Create($"plan-open-items-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var treatmentType = new TreatmentType { CompanyId = companyId, Name = "Filling", BasePrice = 100m, DefaultDurationMinutes = 30 };

        var submittedPlan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
            Status = TreatmentPlanStatus.Pending
        };
        var draftPlan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            Status = TreatmentPlanStatus.Draft
        };

        var pendingItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = submittedPlan,
            TreatmentTypeId = treatmentType.Id,
            Sequence = 1,
            Urgency = UrgencyLevel.High,
            EstimatedPrice = 100m,
            Decision = PlanItemDecision.Pending
        };
        var decidedItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = submittedPlan,
            TreatmentTypeId = treatmentType.Id,
            Sequence = 2,
            Urgency = UrgencyLevel.Low,
            EstimatedPrice = 50m,
            Decision = PlanItemDecision.Accepted
        };
        var draftItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = draftPlan,
            TreatmentTypeId = treatmentType.Id,
            Sequence = 1,
            Urgency = UrgencyLevel.Low,
            EstimatedPrice = 25m,
            Decision = PlanItemDecision.Pending
        };

        db.Patients.Add(patient);
        db.TreatmentTypes.Add(treatmentType);
        db.TreatmentPlans.AddRange(submittedPlan, draftPlan);
        db.PlanItems.AddRange(pendingItem, decidedItem, draftItem);
        await db.SaveChangesAsync();

        var service = new TreatmentPlanService(db, new TenantAccessService(db));

        var results = await service.ListOpenItemsAsync(userId, CancellationToken.None);

        var item = Assert.Single(results);
        Assert.Equal(submittedPlan.Id, item.PlanId);
        Assert.Equal(pendingItem.Id, item.PlanItemId);
        Assert.Equal("Filling", item.TreatmentTypeName);
    }
}
