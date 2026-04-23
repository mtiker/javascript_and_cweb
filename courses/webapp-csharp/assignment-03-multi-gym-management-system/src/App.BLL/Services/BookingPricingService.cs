using App.BLL.Contracts.Infrastructure;
using App.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Services;

public class BookingPricingService(IAppDbContext dbContext) : IBookingPricingService
{
    public async Task<decimal> CalculateBookingPriceAsync(Guid gymId, Guid memberId, TrainingSession trainingSession, CancellationToken cancellationToken = default)
    {
        var bookingDate = DateOnly.FromDateTime(trainingSession.StartAtUtc.Date);

        var membership = await dbContext.Memberships
            .Include(entity => entity.MembershipPackage)
            .Where(entity => entity.GymId == gymId && entity.MemberId == memberId)
            .Where(entity => entity.StartDate <= bookingDate && entity.EndDate >= bookingDate)
            .OrderByDescending(entity => entity.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership?.MembershipPackage == null)
        {
            return trainingSession.BasePrice;
        }

        if (membership.MembershipPackage.IsTrainingFree)
        {
            return 0m;
        }

        if (membership.MembershipPackage.TrainingDiscountPercent is > 0)
        {
            var percent = membership.MembershipPackage.TrainingDiscountPercent.Value;
            return Math.Round(trainingSession.BasePrice * (100 - percent) / 100m, 2, MidpointRounding.AwayFromZero);
        }

        return trainingSession.BasePrice;
    }
}
