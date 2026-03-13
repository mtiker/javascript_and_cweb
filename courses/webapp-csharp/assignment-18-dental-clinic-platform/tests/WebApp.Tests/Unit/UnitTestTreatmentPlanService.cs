using App.BLL.Contracts;
using App.BLL.Exceptions;
using App.BLL.Services;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestTreatmentPlanService
{
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
            Status = "Pending"
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
            Status = "Pending"
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
}
