using Base.Domain;
using Shared.Contracts.Enums;

namespace App.BLL.Contracts.Services.Client;

public sealed record ClientSessionBookingState(
    Guid BookingId,
    BookingStatus Status);

public sealed record ClientSessionDetailSnapshot(
    LangStr? CategoryName,
    IReadOnlyList<string> TrainerNames,
    ClientSessionBookingState? CurrentBooking,
    bool CurrentStaffCanManageRoster);

public sealed record ClientSessionRosterRow(
    Guid BookingId,
    string MemberFirstName,
    string MemberLastName,
    BookingStatus Status,
    decimal ChargedPrice,
    bool PaymentRequired);
