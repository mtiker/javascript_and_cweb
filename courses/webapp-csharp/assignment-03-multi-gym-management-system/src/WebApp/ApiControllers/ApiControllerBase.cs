using System.Globalization;
using App.DAL.EF;
using App.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.ApiControllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase(AppDbContext dbContext) : ControllerBase
{
    private readonly AppDbContext? _dbContext = dbContext;

    protected ApiControllerBase() : this(null!)
    {
    }

    protected AppDbContext DbContext => _dbContext ?? throw new InvalidOperationException("This controller does not expose direct DbContext access.");

    protected static string? Translate(LangStr? value)
    {
        return value?.Translate(CultureInfo.CurrentUICulture.Name);
    }
}
