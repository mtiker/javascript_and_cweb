using App.Domain.Identity;

namespace Modules.Users.Application.Persistence;

public interface IRefreshTokenRepository
{
    Task<AppRefreshToken?> GetByUserAndTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppRefreshToken>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(AppRefreshToken refreshToken, CancellationToken cancellationToken = default);

    void Remove(AppRefreshToken refreshToken);

    void RemoveRange(IEnumerable<AppRefreshToken> refreshTokens);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
