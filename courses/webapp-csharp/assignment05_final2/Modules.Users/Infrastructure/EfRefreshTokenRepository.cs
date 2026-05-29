using App.DAL.EF;
using App.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Modules.Users.Application.Persistence;

namespace Modules.Users.Infrastructure;

/// <summary>
/// Refresh-token EF repository owned by the Users module. Runtime storage still
/// uses the active <see cref="AppDbContext"/> migration path until module
/// migrations replace the compatibility bridge.
/// </summary>
internal sealed class EfRefreshTokenRepository(AppDbContext dbContext) : IRefreshTokenRepository
{
    public async Task<AppRefreshToken?> GetByUserAndTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(
                token => token.UserId == userId && token.RefreshToken == refreshToken,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AppRefreshToken>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.RefreshTokens
            .Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AppRefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public void Remove(AppRefreshToken refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        dbContext.RefreshTokens.Remove(refreshToken);
    }

    public void RemoveRange(IEnumerable<AppRefreshToken> refreshTokens)
    {
        ArgumentNullException.ThrowIfNull(refreshTokens);
        dbContext.RefreshTokens.RemoveRange(refreshTokens);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
