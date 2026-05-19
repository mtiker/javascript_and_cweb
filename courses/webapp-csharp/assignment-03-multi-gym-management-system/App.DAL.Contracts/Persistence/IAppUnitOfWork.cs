namespace App.DAL.Contracts.Persistence;

public interface IAppUnitOfWork
{
    IRefreshTokenRepository RefreshTokens { get; }

    IMemberRepository Members { get; }

    ITrainingCategoryRepository TrainingCategories { get; }

    ITrainingSessionRepository TrainingSessions { get; }

    IBookingRepository Bookings { get; }

    IMembershipPackageRepository MembershipPackages { get; }

    IMembershipRepository Memberships { get; }

    IPaymentRepository Payments { get; }

    IMaintenanceRepository Maintenance { get; }

    IRepository<TEntity, Guid> Repository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
