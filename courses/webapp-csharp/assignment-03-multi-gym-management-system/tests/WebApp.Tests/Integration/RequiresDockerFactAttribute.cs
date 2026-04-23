namespace WebApp.Tests.Integration;

public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_POSTGRES_TESTS"), "1", StringComparison.Ordinal))
        {
            Skip = "Set RUN_POSTGRES_TESTS=1 to run PostgreSQL Testcontainers integration tests.";
        }
    }
}
