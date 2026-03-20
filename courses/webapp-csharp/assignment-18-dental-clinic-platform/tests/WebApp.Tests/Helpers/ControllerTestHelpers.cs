using System.Security.Claims;
using App.BLL.Contracts;
using App.BLL.Contracts.Appointments;
using App.BLL.Contracts.CompanySettings;
using App.BLL.Contracts.CompanyUsers;
using App.BLL.Contracts.Finance;
using App.BLL.Contracts.Patients;
using App.BLL.Contracts.TreatmentPlans;
using App.BLL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Tests.Helpers;

public static class ControllerTestContextFactory
{
    public static TestTenantProvider CreateTenantProvider(string companySlug = "acme")
    {
        var tenantProvider = new TestTenantProvider();
        tenantProvider.SetTenant(Guid.NewGuid(), companySlug);
        tenantProvider.SetIgnoreTenantFilter(false);
        return tenantProvider;
    }

    public static T WithUser<T>(T controller, Guid? userId = null)
        where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString())
            ],
                authenticationType: "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }
}

public static class ControllerAssert
{
    public static T AssertOk<T>(ActionResult<T> result)
    {
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        return Assert.IsAssignableFrom<T>(ok.Value);
    }

    public static T AssertCreated<T>(ActionResult<T> result)
    {
        return result.Result switch
        {
            CreatedResult created => Assert.IsAssignableFrom<T>(created.Value),
            CreatedAtActionResult createdAtAction => Assert.IsAssignableFrom<T>(createdAtAction.Value),
            _ => throw new InvalidOperationException($"Expected CreatedResult or CreatedAtActionResult but got {result.Result?.GetType().Name ?? "null"}.")
        };
    }

    public static void AssertForbid<T>(ActionResult<T> result)
    {
        Assert.IsType<ForbidResult>(result.Result);
    }

    public static void AssertForbid(IActionResult result)
    {
        Assert.IsType<ForbidResult>(result);
    }
}

public sealed class DelegatingPatientService : IPatientService
{
    public Func<Guid, CancellationToken, Task<IReadOnlyCollection<PatientResult>>> ListAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<PatientResult>>([]);

    public Func<Guid, Guid, CancellationToken, Task<PatientResult>> GetAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PatientResult>(new InvalidOperationException("GetAsyncHandler not configured."));

    public Func<Guid, Guid, CancellationToken, Task<PatientProfileResult>> GetProfileAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PatientProfileResult>(new InvalidOperationException("GetProfileAsyncHandler not configured."));

    public Func<Guid, CreatePatientCommand, CancellationToken, Task<PatientResult>> CreateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PatientResult>(new InvalidOperationException("CreateAsyncHandler not configured."));

    public Func<Guid, UpdatePatientCommand, CancellationToken, Task<PatientResult>> UpdateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PatientResult>(new InvalidOperationException("UpdateAsyncHandler not configured."));

    public Func<Guid, Guid, CancellationToken, Task> DeleteAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<PatientResult>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        ListAsyncHandler(userId, cancellationToken);

    public Task<PatientResult> GetAsync(Guid userId, Guid patientId, CancellationToken cancellationToken) =>
        GetAsyncHandler(userId, patientId, cancellationToken);

    public Task<PatientProfileResult> GetProfileAsync(Guid userId, Guid patientId, CancellationToken cancellationToken) =>
        GetProfileAsyncHandler(userId, patientId, cancellationToken);

    public Task<PatientResult> CreateAsync(Guid userId, CreatePatientCommand command, CancellationToken cancellationToken) =>
        CreateAsyncHandler(userId, command, cancellationToken);

    public Task<PatientResult> UpdateAsync(Guid userId, UpdatePatientCommand command, CancellationToken cancellationToken) =>
        UpdateAsyncHandler(userId, command, cancellationToken);

    public Task DeleteAsync(Guid userId, Guid patientId, CancellationToken cancellationToken) =>
        DeleteAsyncHandler(userId, patientId, cancellationToken);
}

public sealed class DelegatingAppointmentService : IAppointmentService
{
    public Func<Guid, CancellationToken, Task<IReadOnlyCollection<AppointmentResult>>> ListAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<AppointmentResult>>([]);

    public Func<Guid, CreateAppointmentCommand, CancellationToken, Task<AppointmentResult>> CreateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<AppointmentResult>(new InvalidOperationException("CreateAsyncHandler not configured."));

    public Func<Guid, RecordAppointmentClinicalCommand, CancellationToken, Task<AppointmentClinicalRecordResult>> RecordClinicalWorkAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<AppointmentClinicalRecordResult>(new InvalidOperationException("RecordClinicalWorkAsyncHandler not configured."));

    public Task<IReadOnlyCollection<AppointmentResult>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        ListAsyncHandler(userId, cancellationToken);

    public Task<AppointmentResult> CreateAsync(Guid userId, CreateAppointmentCommand command, CancellationToken cancellationToken) =>
        CreateAsyncHandler(userId, command, cancellationToken);

    public Task<AppointmentClinicalRecordResult> RecordClinicalWorkAsync(Guid userId, RecordAppointmentClinicalCommand command, CancellationToken cancellationToken) =>
        RecordClinicalWorkAsyncHandler(userId, command, cancellationToken);
}

public sealed class DelegatingTreatmentPlanService : ITreatmentPlanService
{
    public Func<Guid, Guid?, CancellationToken, Task<IReadOnlyCollection<TreatmentPlanResult>>> ListAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<TreatmentPlanResult>>([]);

    public Func<Guid, Guid, CancellationToken, Task<TreatmentPlanResult>> GetAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<TreatmentPlanResult>(new InvalidOperationException("GetAsyncHandler not configured."));

    public Func<Guid, CreateTreatmentPlanCommand, CancellationToken, Task<TreatmentPlanResult>> CreateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<TreatmentPlanResult>(new InvalidOperationException("CreateAsyncHandler not configured."));

    public Func<Guid, UpdateTreatmentPlanCommand, CancellationToken, Task<TreatmentPlanResult>> UpdateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<TreatmentPlanResult>(new InvalidOperationException("UpdateAsyncHandler not configured."));

    public Func<Guid, Guid, CancellationToken, Task<SubmitTreatmentPlanResult>> SubmitAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<SubmitTreatmentPlanResult>(new InvalidOperationException("SubmitAsyncHandler not configured."));

    public Func<Guid, Guid, CancellationToken, Task> DeleteAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Func<Guid, CancellationToken, Task<IReadOnlyCollection<OpenPlanItemResult>>> ListOpenItemsAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<OpenPlanItemResult>>([]);

    public Func<Guid, RecordPlanItemDecisionCommand, CancellationToken, Task<PlanDecisionResult>> RecordPlanItemDecisionAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PlanDecisionResult>(new InvalidOperationException("RecordPlanItemDecisionAsyncHandler not configured."));

    public Task<IReadOnlyCollection<TreatmentPlanResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken) =>
        ListAsyncHandler(userId, patientId, cancellationToken);

    public Task<TreatmentPlanResult> GetAsync(Guid userId, Guid planId, CancellationToken cancellationToken) =>
        GetAsyncHandler(userId, planId, cancellationToken);

    public Task<TreatmentPlanResult> CreateAsync(Guid userId, CreateTreatmentPlanCommand command, CancellationToken cancellationToken) =>
        CreateAsyncHandler(userId, command, cancellationToken);

    public Task<TreatmentPlanResult> UpdateAsync(Guid userId, UpdateTreatmentPlanCommand command, CancellationToken cancellationToken) =>
        UpdateAsyncHandler(userId, command, cancellationToken);

    public Task<SubmitTreatmentPlanResult> SubmitAsync(Guid userId, Guid planId, CancellationToken cancellationToken) =>
        SubmitAsyncHandler(userId, planId, cancellationToken);

    public Task DeleteAsync(Guid userId, Guid planId, CancellationToken cancellationToken) =>
        DeleteAsyncHandler(userId, planId, cancellationToken);

    public Task<IReadOnlyCollection<OpenPlanItemResult>> ListOpenItemsAsync(Guid userId, CancellationToken cancellationToken) =>
        ListOpenItemsAsyncHandler(userId, cancellationToken);

    public Task<PlanDecisionResult> RecordPlanItemDecisionAsync(Guid userId, RecordPlanItemDecisionCommand command, CancellationToken cancellationToken) =>
        RecordPlanItemDecisionAsyncHandler(userId, command, cancellationToken);
}

public sealed class DelegatingCostEstimateService : ICostEstimateService
{
    public Func<Guid, Guid?, CancellationToken, Task<IReadOnlyCollection<CostEstimateResult>>> ListAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<CostEstimateResult>>([]);

    public Func<Guid, CreateCostEstimateCommand, CancellationToken, Task<CostEstimateResult>> CreateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<CostEstimateResult>(new InvalidOperationException("CreateAsyncHandler not configured."));

    public Func<Guid, Guid, string?, CancellationToken, Task<LegalEstimateResult>> GetLegalAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<LegalEstimateResult>(new InvalidOperationException("GetLegalAsyncHandler not configured."));

    public Task<IReadOnlyCollection<CostEstimateResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken) =>
        ListAsyncHandler(userId, patientId, cancellationToken);

    public Task<CostEstimateResult> CreateAsync(Guid userId, CreateCostEstimateCommand command, CancellationToken cancellationToken) =>
        CreateAsyncHandler(userId, command, cancellationToken);

    public Task<LegalEstimateResult> GetLegalAsync(Guid userId, Guid costEstimateId, string? countryCode, CancellationToken cancellationToken) =>
        GetLegalAsyncHandler(userId, costEstimateId, countryCode, cancellationToken);
}

public sealed class DelegatingInvoiceService : IInvoiceService
{
    public Func<Guid, Guid?, CancellationToken, Task<IReadOnlyCollection<InvoiceSummaryResult>>> ListAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<InvoiceSummaryResult>>([]);

    public Func<Guid, Guid, CancellationToken, Task<InvoiceDetailResult>> GetAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<InvoiceDetailResult>(new InvalidOperationException("GetAsyncHandler not configured."));

    public Func<Guid, CreateInvoiceCommand, CancellationToken, Task<InvoiceDetailResult>> CreateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<InvoiceDetailResult>(new InvalidOperationException("CreateAsyncHandler not configured."));

    public Func<Guid, GenerateInvoiceFromProceduresCommand, CancellationToken, Task<InvoiceDetailResult>> GenerateFromProceduresAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<InvoiceDetailResult>(new InvalidOperationException("GenerateFromProceduresAsyncHandler not configured."));

    public Func<Guid, Guid, CreatePaymentCommand, CancellationToken, Task<PaymentResult>> AddPaymentAsyncHandler { get; set; } =
        static (_, _, _, _) => Task.FromException<PaymentResult>(new InvalidOperationException("AddPaymentAsyncHandler not configured."));

    public Func<Guid, UpdateInvoiceCommand, CancellationToken, Task<InvoiceDetailResult>> UpdateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<InvoiceDetailResult>(new InvalidOperationException("UpdateAsyncHandler not configured."));

    public Func<Guid, Guid, CancellationToken, Task> DeleteAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<InvoiceSummaryResult>> ListAsync(Guid userId, Guid? patientId, CancellationToken cancellationToken) =>
        ListAsyncHandler(userId, patientId, cancellationToken);

    public Task<InvoiceDetailResult> GetAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) =>
        GetAsyncHandler(userId, invoiceId, cancellationToken);

    public Task<InvoiceDetailResult> CreateAsync(Guid userId, CreateInvoiceCommand command, CancellationToken cancellationToken) =>
        CreateAsyncHandler(userId, command, cancellationToken);

    public Task<InvoiceDetailResult> GenerateFromProceduresAsync(Guid userId, GenerateInvoiceFromProceduresCommand command, CancellationToken cancellationToken) =>
        GenerateFromProceduresAsyncHandler(userId, command, cancellationToken);

    public Task<PaymentResult> AddPaymentAsync(Guid userId, Guid invoiceId, CreatePaymentCommand command, CancellationToken cancellationToken) =>
        AddPaymentAsyncHandler(userId, invoiceId, command, cancellationToken);

    public Task<InvoiceDetailResult> UpdateAsync(Guid userId, UpdateInvoiceCommand command, CancellationToken cancellationToken) =>
        UpdateAsyncHandler(userId, command, cancellationToken);

    public Task DeleteAsync(Guid userId, Guid invoiceId, CancellationToken cancellationToken) =>
        DeleteAsyncHandler(userId, invoiceId, cancellationToken);
}

public sealed class DelegatingPaymentPlanService : IPaymentPlanService
{
    public Func<Guid, Guid?, CancellationToken, Task<IReadOnlyCollection<PaymentPlanResult>>> ListAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyCollection<PaymentPlanResult>>([]);

    public Func<Guid, Guid, CancellationToken, Task<PaymentPlanResult>> GetAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PaymentPlanResult>(new InvalidOperationException("GetAsyncHandler not configured."));

    public Func<Guid, CreatePaymentPlanCommand, CancellationToken, Task<PaymentPlanResult>> CreateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PaymentPlanResult>(new InvalidOperationException("CreateAsyncHandler not configured."));

    public Func<Guid, UpdatePaymentPlanCommand, CancellationToken, Task<PaymentPlanResult>> UpdateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<PaymentPlanResult>(new InvalidOperationException("UpdateAsyncHandler not configured."));

    public Func<Guid, Guid, CancellationToken, Task> DeleteAsyncHandler { get; set; } =
        static (_, _, _) => Task.CompletedTask;

    public Task<IReadOnlyCollection<PaymentPlanResult>> ListAsync(Guid userId, Guid? invoiceId, CancellationToken cancellationToken) =>
        ListAsyncHandler(userId, invoiceId, cancellationToken);

    public Task<PaymentPlanResult> GetAsync(Guid userId, Guid paymentPlanId, CancellationToken cancellationToken) =>
        GetAsyncHandler(userId, paymentPlanId, cancellationToken);

    public Task<PaymentPlanResult> CreateAsync(Guid userId, CreatePaymentPlanCommand command, CancellationToken cancellationToken) =>
        CreateAsyncHandler(userId, command, cancellationToken);

    public Task<PaymentPlanResult> UpdateAsync(Guid userId, UpdatePaymentPlanCommand command, CancellationToken cancellationToken) =>
        UpdateAsyncHandler(userId, command, cancellationToken);

    public Task DeleteAsync(Guid userId, Guid paymentPlanId, CancellationToken cancellationToken) =>
        DeleteAsyncHandler(userId, paymentPlanId, cancellationToken);
}

public sealed class DelegatingFinanceWorkspaceService : IFinanceWorkspaceService
{
    public Func<Guid, Guid, CancellationToken, Task<FinanceWorkspaceResult>> GetWorkspaceAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<FinanceWorkspaceResult>(new InvalidOperationException("GetWorkspaceAsyncHandler not configured."));

    public Task<FinanceWorkspaceResult> GetWorkspaceAsync(Guid userId, Guid patientId, CancellationToken cancellationToken) =>
        GetWorkspaceAsyncHandler(userId, patientId, cancellationToken);
}

public sealed class DelegatingCompanySettingsService : ICompanySettingsService
{
    public Func<Guid, CancellationToken, Task<CompanySettingsResult>> GetAsyncHandler { get; set; } =
        static (_, _) => Task.FromException<CompanySettingsResult>(new InvalidOperationException("GetAsyncHandler not configured."));

    public Func<Guid, UpdateCompanySettingsCommand, CancellationToken, Task<CompanySettingsResult>> UpdateAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<CompanySettingsResult>(new InvalidOperationException("UpdateAsyncHandler not configured."));

    public Task<CompanySettingsResult> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        GetAsyncHandler(userId, cancellationToken);

    public Task<CompanySettingsResult> UpdateAsync(Guid userId, UpdateCompanySettingsCommand command, CancellationToken cancellationToken) =>
        UpdateAsyncHandler(userId, command, cancellationToken);
}

public sealed class DelegatingCompanyUserService : ICompanyUserService
{
    public Func<Guid, CancellationToken, Task<IReadOnlyCollection<CompanyUserResult>>> ListAsyncHandler { get; set; } =
        static (_, _) => Task.FromResult<IReadOnlyCollection<CompanyUserResult>>([]);

    public Func<Guid, UpsertCompanyUserCommand, CancellationToken, Task<CompanyUserResult>> UpsertAsyncHandler { get; set; } =
        static (_, _, _) => Task.FromException<CompanyUserResult>(new InvalidOperationException("UpsertAsyncHandler not configured."));

    public Task<IReadOnlyCollection<CompanyUserResult>> ListAsync(Guid actorUserId, CancellationToken cancellationToken) =>
        ListAsyncHandler(actorUserId, cancellationToken);

    public Task<CompanyUserResult> UpsertAsync(Guid actorUserId, UpsertCompanyUserCommand command, CancellationToken cancellationToken) =>
        UpsertAsyncHandler(actorUserId, command, cancellationToken);
}
