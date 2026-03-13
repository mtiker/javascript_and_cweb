namespace App.BLL.Contracts.Impersonation;

public sealed record StartImpersonationResult(
    Guid TargetUserId,
    string TargetUserEmail,
    Guid ActiveCompanyId,
    string ActiveCompanySlug,
    string ActiveCompanyRole,
    Guid ImpersonatedByUserId,
    string Reason);
