using App.DTO.v1.CostEstimates;
using App.DTO.v1.InsurancePlans;
using App.DTO.v1.Invoices;
using App.DTO.v1.PatientInsurancePolicies;
using App.DTO.v1.Patients;
using App.DTO.v1.TreatmentPlans;

namespace App.DTO.v1.Finance;

public class FinanceWorkspaceResponse
{
    public PatientResponse Patient { get; set; } = default!;
    public IReadOnlyCollection<InsurancePlanResponse> InsurancePlans { get; set; } = Array.Empty<InsurancePlanResponse>();
    public IReadOnlyCollection<TreatmentPlanResponse> Plans { get; set; } = Array.Empty<TreatmentPlanResponse>();
    public IReadOnlyCollection<PatientInsurancePolicyResponse> Policies { get; set; } = Array.Empty<PatientInsurancePolicyResponse>();
    public IReadOnlyCollection<CostEstimateResponse> Estimates { get; set; } = Array.Empty<CostEstimateResponse>();
    public IReadOnlyCollection<PerformedProcedureResponse> Procedures { get; set; } = Array.Empty<PerformedProcedureResponse>();
    public IReadOnlyCollection<InvoiceResponse> Invoices { get; set; } = Array.Empty<InvoiceResponse>();
}
