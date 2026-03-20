using App.BLL.Contracts.TreatmentPlans;

namespace App.BLL.Contracts.Finance;

public sealed record CreateCostEstimateCommand(
    Guid PatientId,
    Guid TreatmentPlanId,
    Guid? PatientInsurancePolicyId,
    string EstimateNumber,
    string FormatCode);

public sealed record CostEstimateResult(
    Guid Id,
    Guid PatientId,
    Guid TreatmentPlanId,
    Guid? PatientInsurancePolicyId,
    Guid? InsurancePlanId,
    string EstimateNumber,
    string FormatCode,
    decimal TotalEstimatedAmount,
    decimal CoverageAmount,
    decimal PatientEstimatedAmount,
    DateTime GeneratedAtUtc,
    string Status);

public sealed record LegalEstimateResult(
    Guid CostEstimateId,
    string CountryCode,
    string DocumentType,
    string GeneratedText,
    DateTime GeneratedAtUtc);

public sealed record InvoiceLineCommand(
    Guid? TreatmentId,
    Guid? PlanItemId,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal CoverageAmount);

public sealed record CreateInvoiceCommand(
    Guid PatientId,
    Guid? CostEstimateId,
    string InvoiceNumber,
    DateTime DueDateUtc,
    IReadOnlyCollection<InvoiceLineCommand> Lines);

public sealed record UpdateInvoiceCommand(
    Guid InvoiceId,
    Guid PatientId,
    Guid? CostEstimateId,
    string InvoiceNumber,
    DateTime DueDateUtc,
    IReadOnlyCollection<InvoiceLineCommand> Lines);

public sealed record GenerateInvoiceFromProceduresCommand(
    Guid PatientId,
    Guid? CostEstimateId,
    string InvoiceNumber,
    DateTime DueDateUtc,
    IReadOnlyCollection<Guid> TreatmentIds);

public sealed record CreatePaymentCommand(
    decimal Amount,
    DateTime PaidAtUtc,
    string Method,
    string? Reference,
    string? Notes);

public sealed record InvoiceSummaryResult(
    Guid Id,
    Guid PatientId,
    Guid? CostEstimateId,
    string InvoiceNumber,
    decimal TotalAmount,
    decimal CoverageAmount,
    decimal PatientResponsibilityAmount,
    decimal PaidAmount,
    decimal BalanceAmount,
    DateTime DueDateUtc,
    string Status);

public sealed record InvoiceLineResult(
    Guid Id,
    Guid? TreatmentId,
    Guid? PlanItemId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    decimal CoverageAmount,
    decimal PatientAmount);

public sealed record PaymentResult(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    DateTime PaidAtUtc,
    string Method,
    string? Reference,
    string? Notes);

public sealed record PaymentPlanInstallmentCommand(
    DateTime DueDateUtc,
    decimal Amount);

public sealed record CreatePaymentPlanCommand(
    Guid InvoiceId,
    DateTime StartsAtUtc,
    string Terms,
    IReadOnlyCollection<PaymentPlanInstallmentCommand> Installments);

public sealed record UpdatePaymentPlanCommand(
    Guid PaymentPlanId,
    DateTime StartsAtUtc,
    string Terms,
    IReadOnlyCollection<PaymentPlanInstallmentCommand> Installments);

public sealed record PaymentPlanInstallmentResult(
    Guid Id,
    DateTime DueDateUtc,
    decimal Amount,
    string Status,
    DateTime? PaidAtUtc);

public sealed record PaymentPlanResult(
    Guid Id,
    Guid InvoiceId,
    DateTime StartsAtUtc,
    string Status,
    string Terms,
    decimal ScheduledAmount,
    decimal RemainingAmount,
    IReadOnlyCollection<PaymentPlanInstallmentResult> Installments);

public sealed record InvoiceDetailResult(
    Guid Id,
    Guid PatientId,
    Guid? CostEstimateId,
    string InvoiceNumber,
    decimal TotalAmount,
    decimal CoverageAmount,
    decimal PatientResponsibilityAmount,
    decimal PaidAmount,
    decimal BalanceAmount,
    DateTime DueDateUtc,
    string Status,
    IReadOnlyCollection<InvoiceLineResult> Lines,
    IReadOnlyCollection<PaymentResult> Payments,
    PaymentPlanResult? PaymentPlan);

public sealed record FinancePatientResult(
    Guid Id,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string? PersonalCode,
    string? Email,
    string? Phone);

public sealed record InsurancePlanResult(
    Guid Id,
    string Name,
    string CountryCode,
    string CoverageType,
    bool IsActivePlan,
    string? ClaimSubmissionEndpoint);

public sealed record PatientInsurancePolicyResult(
    Guid Id,
    Guid PatientId,
    Guid InsurancePlanId,
    string InsurancePlanName,
    string PolicyNumber,
    string? MemberNumber,
    string? GroupNumber,
    DateOnly CoverageStart,
    DateOnly? CoverageEnd,
    decimal AnnualMaximum,
    decimal Deductible,
    decimal CoveragePercent,
    string Status);

public sealed record PerformedProcedureResult(
    Guid Id,
    Guid PatientId,
    Guid TreatmentTypeId,
    Guid? PlanItemId,
    Guid? AppointmentId,
    int? ToothNumber,
    DateTime PerformedAtUtc,
    decimal Price,
    string TreatmentTypeName,
    string? Notes);

public sealed record FinanceWorkspaceResult(
    FinancePatientResult Patient,
    IReadOnlyCollection<InsurancePlanResult> InsurancePlans,
    IReadOnlyCollection<TreatmentPlanResult> Plans,
    IReadOnlyCollection<PatientInsurancePolicyResult> Policies,
    IReadOnlyCollection<CostEstimateResult> Estimates,
    IReadOnlyCollection<PerformedProcedureResult> Procedures,
    IReadOnlyCollection<InvoiceSummaryResult> Invoices);
