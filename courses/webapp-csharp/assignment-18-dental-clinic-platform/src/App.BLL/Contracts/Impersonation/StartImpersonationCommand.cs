namespace App.BLL.Contracts.Impersonation;

public sealed record StartImpersonationCommand(
    string TargetUserEmail,
    string CompanySlug,
    string Reason);
