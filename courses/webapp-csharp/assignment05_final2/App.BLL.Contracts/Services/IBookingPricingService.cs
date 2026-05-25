using App.Domain.Entities;

namespace App.BLL.Contracts.Services;

public interface IBookingPricingService
{
    Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default);
}
