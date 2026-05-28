namespace App.BLL.Contracts.Services;

/// <summary>
/// Provisions and maintains the login account (<c>AppUser</c>) backing a gym
/// member. The login is keyed by email and shared across every gym the person
/// belongs to (one <c>AppUser</c> ↔ one <c>Person</c>); per-gym access is an
/// <c>AppUserGymRole</c>. Owned by the Users module.
/// </summary>
public interface IMemberAccountService
{
    /// <summary>
    /// Ensures a login exists for <paramref name="email"/> and that it can act
    /// as a Member in the given gym. If the email is new, creates the
    /// <c>Person</c> + <c>AppUser</c> from the supplied demographics. If it
    /// already exists, reuses that account/person and only adds the gym role
    /// (the supplied password is ignored). Returns the <c>PersonId</c> the
    /// caller must attach the new <c>Member</c> row to.
    /// </summary>
    Task<MemberLoginProvisionResult> ProvisionMemberLoginAsync(
        Guid gymId,
        string email,
        string password,
        MemberPersonDraft demographics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin-side password reset for the login backing the given member. No
    /// current password is required. Throws if the member has no login.
    /// </summary>
    Task SetPasswordByMemberAsync(string gymCode, Guid memberId, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the email of the login backing the given member, or
    /// <c>null</c> when the member has no login account.
    /// </summary>
    Task<string?> GetLoginEmailByMemberAsync(string gymCode, Guid memberId, CancellationToken cancellationToken = default);
}

public sealed record MemberPersonDraft(
    string FirstName,
    string LastName,
    string? PersonalCode,
    DateOnly? DateOfBirth);

public sealed record MemberLoginProvisionResult(Guid PersonId, bool ReusedExistingAccount);
