using App.DAL.EF;
using App.DAL.EF.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace WebApp.Tests.Integration;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class PostgreSqlIntegrationCollection : ICollectionFixture<PostgreSqlTestDatabaseFixture>
{
    public const string CollectionName = "postgresql-integration";
}

public sealed class PostgreSqlTestDatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _isStarted;

    public string ConnectionString => _container?.GetConnectionString()
                                      ?? throw new InvalidOperationException("PostgreSQL test container has not been initialized.");

    public async Task InitializeAsync()
    {
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await EnsureContainerStartedAsync();

        await using var context = CreateDbContext(ignoreGymFilter: true);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public AppDbContext CreateDbContext(Guid? gymId = null, bool ignoreGymFilter = false, string? gymCode = null)
    {
        if (!_isStarted)
        {
            throw new InvalidOperationException("PostgreSQL test container has not been started. Call ResetDatabaseAsync first.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new AppDbContext(
            options,
            new FixtureGymContext
            {
                GymId = gymId,
                GymCode = gymCode,
                IgnoreGymFilter = ignoreGymFilter
            },
            new HttpContextAccessor());
    }

    private async Task EnsureContainerStartedAsync()
    {
        if (_isStarted)
        {
            return;
        }

        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("assignment03_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _container.StartAsync();
        _isStarted = true;
    }

    private sealed class FixtureGymContext : IGymContext
    {
        public Guid? GymId { get; init; }
        public string? GymCode { get; init; }
        public string? ActiveRole => null;
        public bool IgnoreGymFilter { get; init; }
    }
}
