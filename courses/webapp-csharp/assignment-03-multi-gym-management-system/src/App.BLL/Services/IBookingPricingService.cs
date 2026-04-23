using App.Domain.Entities;

namespace App.BLL.Services;

public interface IBookingPricingService
{
    Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default);
}
