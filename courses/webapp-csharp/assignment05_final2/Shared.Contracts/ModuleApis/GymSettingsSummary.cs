namespace Shared.Contracts.ModuleApis;

public sealed record GymSettingsSummary(
    Guid GymId,
    string CurrencyCode,
    bool AllowNonMemberBookings);
