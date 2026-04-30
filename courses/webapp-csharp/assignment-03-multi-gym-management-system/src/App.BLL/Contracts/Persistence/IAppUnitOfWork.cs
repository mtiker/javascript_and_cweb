namespace App.BLL.Contracts.Persistence;

public interface IAppUnitOfWork
{
    IRefreshTokenRepository RefreshTokens { get; }

    IMemberRepository Members { get; }

    ITrainingCategoryRepository TrainingCategories { get; }

    ITrainingSessionRepository TrainingSessions { get; }

    IBookingRepository Bookings { get; }

    IWorkShiftRepository WorkShifts { get; }

    IMembershipPackageRepository MembershipPackages { get; }

    IMembershipRepository Memberships { get; }

    IPaymentRepository Payments { get; }

    IFinanceRepository Finance { get; }

    IMaintenanceRepository Maintenance { get; }

    IRepository<TEntity, Guid> Repository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
