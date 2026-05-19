using System.Collections.Concurrent;
using App.DAL.Contracts.Persistence;

namespace App.DAL.EF.Repositories;

public class AppUOW(AppDbContext dbContext) : IAppUnitOfWork
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IRefreshTokenRepository? _refreshTokens;
    private IMemberRepository? _members;
    private ITrainingCategoryRepository? _trainingCategories;
    private ITrainingSessionRepository? _trainingSessions;
    private IBookingRepository? _bookings;
    private IMembershipPackageRepository? _membershipPackages;
    private IMembershipRepository? _memberships;
    private IPaymentRepository? _payments;
    private IMaintenanceRepository? _maintenance;

    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new EfRefreshTokenRepository(_dbContext);

    public IMemberRepository Members => _members ??= new EfMemberRepository(_dbContext);

    public ITrainingCategoryRepository TrainingCategories => _trainingCategories ??= new EfTrainingCategoryRepository(_dbContext);

    public ITrainingSessionRepository TrainingSessions => _trainingSessions ??= new EfTrainingSessionRepository(_dbContext);

    public IBookingRepository Bookings => _bookings ??= new EfBookingRepository(_dbContext);

    public IMembershipPackageRepository MembershipPackages => _membershipPackages ??= new EfMembershipPackageRepository(_dbContext);

    public IMembershipRepository Memberships => _memberships ??= new EfMembershipRepository(_dbContext);

    public IPaymentRepository Payments => _payments ??= new EfPaymentRepository(_dbContext);

    public IMaintenanceRepository Maintenance => _maintenance ??= new EfMaintenanceRepository(_dbContext);

    public IRepository<TEntity, Guid> Repository<TEntity>() where TEntity : class
    {
        return (IRepository<TEntity, Guid>)_repositories.GetOrAdd(
            typeof(TEntity),
            _ => new EfRepository<TEntity, Guid>(_dbContext));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
