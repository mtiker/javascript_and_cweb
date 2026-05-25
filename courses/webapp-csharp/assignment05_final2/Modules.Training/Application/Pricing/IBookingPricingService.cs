using Shared.Contracts.ModuleApis;

namespace Modules.Training.Application.Pricing;

/// <summary>
/// Calculates the price a member would pay for a single booking against a
/// specific training session, taking active memberships and per-package
/// discounts into account. Lives in the Training module because the booking
/// price ultimately ships out on the booking row Training owns; the function
/// reaches into Memberships data through the shared persistence layer for
/// now and will move behind an explicit module API call in a later phase.
/// </summary>
public interface IBookingPricingService
{
    Task<decimal> CalculateBookingPriceAsync(
        Guid gymId,
        Guid memberId,
        TrainingSessionSummary trainingSession,
        CancellationToken cancellationToken = default);
}
