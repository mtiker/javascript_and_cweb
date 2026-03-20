using App.BLL.Contracts.Finance;
using App.BLL.Exceptions;
using App.BLL.Services;
using App.DAL.EF;
using App.Domain;
using App.Domain.Entities;
using App.Domain.Enums;
using WebApp.Tests.Helpers;

namespace WebApp.Tests.Unit;

public class UnitTestFinanceServices
{
    [Fact]
    public async Task CostEstimateService_CreateAsync_CalculatesCoverageFromActivePolicy()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"estimate-create-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedManagerAndStandardSubscriptionAsync(db, companyId, userId);

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var plan = new TreatmentPlan { CompanyId = companyId, PatientId = patient.Id, Status = TreatmentPlanStatus.Draft };
        var insurancePlan = new InsurancePlan
        {
            CompanyId = companyId,
            Name = "Dental Plus",
            CountryCode = "EE",
            CoverageType = CoverageType.Private,
            IsActivePlan = true
        };
        var policy = new PatientInsurancePolicy
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            InsurancePlanId = insurancePlan.Id,
            PolicyNumber = "POL-001",
            CoverageStart = new DateOnly(2026, 1, 1),
            AnnualMaximum = 1000m,
            Deductible = 20m,
            CoveragePercent = 50m,
            Status = PatientInsurancePolicyStatus.Active
        };

        db.Patients.Add(patient);
        db.TreatmentPlans.Add(plan);
        db.PlanItems.AddRange(
            new PlanItem
            {
                CompanyId = companyId,
                TreatmentPlan = plan,
                TreatmentTypeId = Guid.NewGuid(),
                Sequence = 1,
                Urgency = UrgencyLevel.High,
                EstimatedPrice = 100m
            },
            new PlanItem
            {
                CompanyId = companyId,
                TreatmentPlan = plan,
                TreatmentTypeId = Guid.NewGuid(),
                Sequence = 2,
                Urgency = UrgencyLevel.Medium,
                EstimatedPrice = 200m
            });
        db.InsurancePlans.Add(insurancePlan);
        db.PatientInsurancePolicies.Add(policy);
        await db.SaveChangesAsync();

        var service = new CostEstimateService(db, new TenantAccessService(db), new SubscriptionPolicyService(db, tenantProvider));

        var result = await service.CreateAsync(
            userId,
            new CreateCostEstimateCommand(
                patient.Id,
                plan.Id,
                policy.Id,
                "EST-001",
                "de"),
            CancellationToken.None);

        Assert.Equal(300m, result.TotalEstimatedAmount);
        Assert.Equal(140m, result.CoverageAmount);
        Assert.Equal(160m, result.PatientEstimatedAmount);
        Assert.Equal("DE", result.FormatCode);
        Assert.Equal(CostEstimateStatus.Prepared.ToString(), result.Status);
    }

    [Fact]
    public async Task CostEstimateService_GetLegalAsync_UsesCompanySettingsCountryCodeWhenQueryMissing()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"estimate-legal-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedManagerAndStandardSubscriptionAsync(db, companyId, userId);

        db.CompanySettings.Add(new CompanySettings
        {
            CompanyId = companyId,
            CountryCode = "DE",
            CurrencyCode = "EUR",
            Timezone = "Europe/Berlin"
        });

        var estimate = new CostEstimate
        {
            CompanyId = companyId,
            PatientId = Guid.NewGuid(),
            TreatmentPlanId = Guid.NewGuid(),
            EstimateNumber = "EST-DE-1",
            FormatCode = "DE",
            TotalEstimatedAmount = 250m,
            CoverageAmount = 100m,
            PatientEstimatedAmount = 150m,
            GeneratedAtUtc = DateTime.UtcNow,
            Status = CostEstimateStatus.Prepared
        };
        db.CostEstimates.Add(estimate);
        await db.SaveChangesAsync();

        var service = new CostEstimateService(db, new TenantAccessService(db), new SubscriptionPolicyService(db, tenantProvider));

        var result = await service.GetLegalAsync(userId, estimate.Id, null, CancellationToken.None);

        Assert.Equal("DE", result.CountryCode);
        Assert.Equal("Kostenvoranschlag", result.DocumentType);
        Assert.Contains("EST-DE-1", result.GeneratedText);
    }

    [Fact]
    public async Task InvoiceService_CreateAsync_BuildsTotalsFromPlanItemLine()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"invoice-create-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedManagerRoleAsync(db, companyId, userId);

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var treatmentType = new TreatmentType
        {
            CompanyId = companyId,
            Name = "Filling",
            BasePrice = 120m,
            DefaultDurationMinutes = 30
        };
        var plan = new TreatmentPlan { CompanyId = companyId, PatientId = patient.Id, Status = TreatmentPlanStatus.Draft };
        var planItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = plan,
            TreatmentTypeId = treatmentType.Id,
            Sequence = 1,
            Urgency = UrgencyLevel.High,
            EstimatedPrice = 120m
        };

        db.Patients.Add(patient);
        db.TreatmentTypes.Add(treatmentType);
        db.TreatmentPlans.Add(plan);
        db.PlanItems.Add(planItem);
        await db.SaveChangesAsync();

        var service = new InvoiceService(db, new TenantAccessService(db));

        var result = await service.CreateAsync(
            userId,
            new CreateInvoiceCommand(
                patient.Id,
                null,
                "INV-001",
                DateTime.UtcNow.AddDays(10),
                new[]
                {
                    new InvoiceLineCommand(null, planItem.Id, null, 2m, 0m, 30m)
                }),
            CancellationToken.None);

        Assert.Equal(240m, result.TotalAmount);
        Assert.Equal(30m, result.CoverageAmount);
        Assert.Equal(210m, result.PatientResponsibilityAmount);
        Assert.Equal(210m, result.BalanceAmount);
        Assert.Equal(InvoiceStatus.Issued.ToString(), result.Status);

        var line = Assert.Single(result.Lines);
        Assert.Equal("Planned Filling", line.Description);
        Assert.Equal(planItem.Id, line.PlanItemId);
    }

    [Fact]
    public async Task InvoiceService_AddPaymentAsync_ThrowsValidation_WhenPaymentExceedsBalance()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"invoice-payment-limit-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedManagerRoleAsync(db, companyId, userId);

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var invoice = new Invoice
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            InvoiceNumber = "INV-OVERPAY",
            DueDateUtc = DateTime.UtcNow.AddDays(5)
        };
        var line = new InvoiceLine
        {
            CompanyId = companyId,
            Invoice = invoice,
            Description = "Procedure",
            Quantity = 1m,
            UnitPrice = 100m,
            CoverageAmount = 0m
        };
        FinanceMath.NormalizeInvoiceLine(line);
        invoice.Lines.Add(line);
        FinanceMath.ApplyInvoiceState(invoice);

        db.Patients.Add(patient);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new InvoiceService(db, new TenantAccessService(db));

        await Assert.ThrowsAsync<ValidationAppException>(async () =>
            await service.AddPaymentAsync(
                userId,
                invoice.Id,
                new CreatePaymentCommand(120m, DateTime.UtcNow, "Card", null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task PaymentPlanService_CreateAsync_CreatesInstallmentsMatchingInvoiceBalance()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"payment-plan-create-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedManagerAndStandardSubscriptionAsync(db, companyId, userId);

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var invoice = BuildInvoice(companyId, patient.Id, "INV-PP-1", 150m);

        db.Patients.Add(patient);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        var service = new PaymentPlanService(db, new TenantAccessService(db), new SubscriptionPolicyService(db, tenantProvider));

        var result = await service.CreateAsync(
            userId,
            new CreatePaymentPlanCommand(
                invoice.Id,
                DateTime.UtcNow.AddDays(1),
                "Two monthly installments",
                new[]
                {
                    new PaymentPlanInstallmentCommand(DateTime.UtcNow.AddDays(30), 50m),
                    new PaymentPlanInstallmentCommand(DateTime.UtcNow.AddDays(60), 100m)
                }),
            CancellationToken.None);

        Assert.Equal(PaymentPlanStatus.Active.ToString(), result.Status);
        Assert.Equal(150m, result.ScheduledAmount);
        Assert.Equal(150m, result.RemainingAmount);
        Assert.Equal(2, result.Installments.Count);
        Assert.All(result.Installments, installment => Assert.Equal(PaymentPlanInstallmentStatus.Scheduled.ToString(), installment.Status));
    }

    [Fact]
    public async Task PaymentPlanService_UpdateAsync_ThrowsValidation_WhenPaidInstallmentAlreadyExists()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"payment-plan-paid-update-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedManagerAndStandardSubscriptionAsync(db, companyId, userId);

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var invoice = BuildInvoice(companyId, patient.Id, "INV-PP-2", 150m);
        var plan = new PaymentPlan
        {
            CompanyId = companyId,
            Invoice = invoice,
            StartsAtUtc = DateTime.UtcNow.AddDays(-10),
            Terms = "Existing terms",
            Status = PaymentPlanStatus.Active
        };
        var installment = new PaymentPlanInstallment
        {
            CompanyId = companyId,
            PaymentPlan = plan,
            DueDateUtc = DateTime.UtcNow.AddDays(-5),
            Amount = 150m,
            Status = PaymentPlanInstallmentStatus.Paid,
            PaidAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        plan.Installments.Add(installment);
        invoice.PaymentPlan = plan;

        db.Patients.Add(patient);
        db.Invoices.Add(invoice);
        db.PaymentPlans.Add(plan);
        db.PaymentPlanInstallments.Add(installment);
        await db.SaveChangesAsync();

        var service = new PaymentPlanService(db, new TenantAccessService(db), new SubscriptionPolicyService(db, tenantProvider));

        await Assert.ThrowsAsync<ValidationAppException>(async () =>
            await service.UpdateAsync(
                userId,
                new UpdatePaymentPlanCommand(
                    plan.Id,
                    DateTime.UtcNow.AddDays(1),
                    "Updated terms",
                    new[]
                    {
                        new PaymentPlanInstallmentCommand(DateTime.UtcNow.AddDays(30), 150m)
                    }),
                CancellationToken.None));
    }

    [Fact]
    public async Task FinanceWorkspaceService_GetWorkspaceAsync_ReturnsAggregatedPatientFinanceData()
    {
        var tenantProvider = CreateTenantProvider(out var companyId);
        await using var db = TestDbContextFactory.Create($"finance-workspace-{Guid.NewGuid():N}", tenantProvider);

        var userId = Guid.NewGuid();
        await SeedEmployeeRoleAsync(db, companyId, userId);

        var patient = new Patient { CompanyId = companyId, FirstName = "Jane", LastName = "Doe" };
        var insurancePlan = new InsurancePlan
        {
            CompanyId = companyId,
            Name = "Dental Plus",
            CountryCode = "EE",
            CoverageType = CoverageType.Private,
            IsActivePlan = true
        };
        var treatmentType = new TreatmentType
        {
            CompanyId = companyId,
            Name = "Filling",
            BasePrice = 100m,
            DefaultDurationMinutes = 30
        };
        var plan = new TreatmentPlan
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-3),
            Status = TreatmentPlanStatus.Pending
        };
        var planItem = new PlanItem
        {
            CompanyId = companyId,
            TreatmentPlan = plan,
            TreatmentTypeId = treatmentType.Id,
            Sequence = 1,
            Urgency = UrgencyLevel.High,
            EstimatedPrice = 100m
        };
        var policy = new PatientInsurancePolicy
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            InsurancePlanId = insurancePlan.Id,
            PolicyNumber = "POL-WS",
            CoverageStart = new DateOnly(2026, 1, 1),
            AnnualMaximum = 500m,
            Deductible = 0m,
            CoveragePercent = 50m,
            Status = PatientInsurancePolicyStatus.Active
        };
        var estimate = new CostEstimate
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            TreatmentPlanId = plan.Id,
            PatientInsurancePolicyId = policy.Id,
            InsurancePlanId = insurancePlan.Id,
            EstimateNumber = "EST-WS",
            FormatCode = "EE",
            TotalEstimatedAmount = 100m,
            CoverageAmount = 50m,
            PatientEstimatedAmount = 50m,
            GeneratedAtUtc = DateTime.UtcNow.AddDays(-2),
            Status = CostEstimateStatus.Prepared
        };
        var treatment = new Treatment
        {
            CompanyId = companyId,
            PatientId = patient.Id,
            TreatmentTypeId = treatmentType.Id,
            PlanItemId = planItem.Id,
            ToothNumber = 11,
            PerformedAtUtc = DateTime.UtcNow.AddDays(-1),
            Price = 100m,
            Notes = "Completed"
        };
        var invoice = BuildInvoice(companyId, patient.Id, "INV-WS", 100m);
        var payment = new Payment
        {
            CompanyId = companyId,
            Invoice = invoice,
            Amount = 40m,
            PaidAtUtc = DateTime.UtcNow,
            Method = "Card"
        };
        invoice.Payments.Add(payment);
        FinanceMath.ApplyInvoiceState(invoice);

        db.Patients.Add(patient);
        db.InsurancePlans.Add(insurancePlan);
        db.TreatmentTypes.Add(treatmentType);
        db.TreatmentPlans.Add(plan);
        db.PlanItems.Add(planItem);
        db.PatientInsurancePolicies.Add(policy);
        db.CostEstimates.Add(estimate);
        db.Treatments.Add(treatment);
        db.Invoices.Add(invoice);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var service = new FinanceWorkspaceService(db, new TenantAccessService(db));

        var result = await service.GetWorkspaceAsync(userId, patient.Id, CancellationToken.None);

        Assert.Equal(patient.Id, result.Patient.Id);
        Assert.Single(result.Plans);
        Assert.Single(result.Policies);
        Assert.Single(result.Estimates);
        Assert.Single(result.Procedures);
        Assert.Single(result.Invoices);
        Assert.Equal(40m, result.Invoices.Single().PaidAmount);
    }

    private static TestTenantProvider CreateTenantProvider(out Guid companyId)
    {
        companyId = Guid.NewGuid();
        var tenantProvider = new TestTenantProvider();
        tenantProvider.SetTenant(companyId, "acme");
        tenantProvider.SetIgnoreTenantFilter(false);
        return tenantProvider;
    }

    private static async Task SeedManagerAndStandardSubscriptionAsync(AppDbContext db, Guid companyId, Guid userId)
    {
        await SeedManagerRoleAsync(db, companyId, userId);
        db.Subscriptions.Add(new Subscription
        {
            CompanyId = companyId,
            Tier = SubscriptionTier.Standard,
            Status = SubscriptionStatus.Active,
            StartsAtUtc = DateTime.UtcNow.AddDays(-30)
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedManagerRoleAsync(AppDbContext db, Guid companyId, Guid userId)
    {
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyManager,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedEmployeeRoleAsync(AppDbContext db, Guid companyId, Guid userId)
    {
        db.AppUserRoles.Add(new AppUserRole
        {
            AppUserId = userId,
            CompanyId = companyId,
            RoleName = RoleNames.CompanyEmployee,
            IsActive = true
        });
        await db.SaveChangesAsync();
    }

    private static Invoice BuildInvoice(Guid companyId, Guid patientId, string invoiceNumber, decimal amount)
    {
        var invoice = new Invoice
        {
            CompanyId = companyId,
            PatientId = patientId,
            InvoiceNumber = invoiceNumber,
            DueDateUtc = DateTime.UtcNow.AddDays(10)
        };
        var line = new InvoiceLine
        {
            CompanyId = companyId,
            Invoice = invoice,
            Description = "Procedure",
            Quantity = 1m,
            UnitPrice = amount,
            CoverageAmount = 0m
        };

        FinanceMath.NormalizeInvoiceLine(line);
        invoice.Lines.Add(line);
        FinanceMath.ApplyInvoiceState(invoice);

        return invoice;
    }
}
