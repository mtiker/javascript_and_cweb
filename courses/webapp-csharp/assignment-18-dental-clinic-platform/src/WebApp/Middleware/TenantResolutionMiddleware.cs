using App.DAL.EF;
using App.DAL.EF.Tenant;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ReservedTopSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "api",
        "app",
        "swagger",
        "identity",
        "health"
    };

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, AppDbContext dbContext)
    {
        tenantProvider.ClearTenant();

        // Static assets (css/js/images/fonts/etc.) must bypass tenant resolution.
        if (Path.HasExtension(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        var pathSegments = context.Request.Path.Value?
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];

        if (pathSegments.Length >= 3 && pathSegments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            var scopeSegment = pathSegments[2];

            if (scopeSegment.Equals("account", StringComparison.OrdinalIgnoreCase) ||
                scopeSegment.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                tenantProvider.SetIgnoreTenantFilter(true);
                await next(context);
                return;
            }

            await ResolveAndAssignTenantAsync(scopeSegment, context, tenantProvider, dbContext);
            if (context.Response.HasStarted)
            {
                return;
            }

            await next(context);
            return;
        }

        if (pathSegments.Length >= 1 && !ReservedTopSegments.Contains(pathSegments[0]))
        {
            await ResolveAndAssignTenantAsync(pathSegments[0], context, tenantProvider, dbContext);
            if (context.Response.HasStarted)
            {
                return;
            }
        }

        await next(context);
    }

    private static async Task ResolveAndAssignTenantAsync(string companySlug, HttpContext context, ITenantProvider tenantProvider, AppDbContext dbContext)
    {
        var normalizedSlug = companySlug.ToLowerInvariant();

        var company = await dbContext.Companies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Slug == normalizedSlug);

        if (company == null)
        {
            await Results.NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Company not found",
                Detail = $"Company slug '{normalizedSlug}' does not exist."
            }).ExecuteAsync(context);
            return;
        }

        if (!company.IsActive)
        {
            await Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Company is deactivated",
                detail: "The company is deactivated and currently has no access.").ExecuteAsync(context);
            return;
        }

        tenantProvider.SetTenant(company.Id, company.Slug);
        tenantProvider.SetIgnoreTenantFilter(false);
    }
}
