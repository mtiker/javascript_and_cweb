using App.Domain.Common;

namespace App.Domain.Entities;

public class Patient : TenantBaseEntity
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; }
    public string? PersonalCode { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public ICollection<ToothRecord> ToothRecords { get; set; } = new List<ToothRecord>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<TreatmentPlan> TreatmentPlans { get; set; } = new List<TreatmentPlan>();
    public ICollection<Treatment> Treatments { get; set; } = new List<Treatment>();
    public ICollection<CostEstimate> CostEstimates { get; set; } = new List<CostEstimate>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PatientInsurancePolicy> InsurancePolicies { get; set; } = new List<PatientInsurancePolicy>();
}
