using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using App.DTO.v1;
using App.DTO.v1.Identity;
using WebApp.ApiControllers.Identity;

namespace WebApp.Tests.Unit;

public class ApiContractMetadataTests
{
    [Fact]
    public void PublicApiControllers_DeclareStandardProblemDetailsErrorMetadata()
    {
        var controllerTypes = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type.Namespace?.StartsWith("WebApp.ApiControllers", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(controllerTypes);

        foreach (var controllerType in controllerTypes)
        {
            Assert.True(HasProducesErrorProblemDetails(controllerType), $"{controllerType.Name} is missing ProducesErrorResponseType(typeof(ProblemDetails)).");

            Assert.True(HasProblemDetailsStatusCode(controllerType, StatusCodes.Status400BadRequest), $"{controllerType.Name} is missing 400 ProblemDetails metadata.");
            Assert.True(HasProblemDetailsStatusCode(controllerType, StatusCodes.Status401Unauthorized), $"{controllerType.Name} is missing 401 ProblemDetails metadata.");
            Assert.True(HasProblemDetailsStatusCode(controllerType, StatusCodes.Status403Forbidden), $"{controllerType.Name} is missing 403 ProblemDetails metadata.");
            Assert.True(HasProblemDetailsStatusCode(controllerType, StatusCodes.Status404NotFound), $"{controllerType.Name} is missing 404 ProblemDetails metadata.");
            Assert.True(HasProblemDetailsStatusCode(controllerType, StatusCodes.Status409Conflict), $"{controllerType.Name} is missing 409 ProblemDetails metadata.");
        }
    }

    [Fact]
    public void AccountAuthPublicRoutesAndDtos_RemainStable()
    {
        var route = Assert.Single(typeof(AccountController)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>());

        Assert.Equal("api/v{version:apiVersion}/account", route.Template);

        AssertActionContract(
            nameof(AccountController.Login),
            "login",
            typeof(LoginRequest),
            typeof(JwtResponse));
        AssertActionContract(
            nameof(AccountController.Logout),
            "logout",
            null,
            typeof(Message));
        AssertActionContract(
            nameof(AccountController.RenewRefreshToken),
            "renew-refresh-token",
            typeof(RefreshTokenRequest),
            typeof(JwtResponse));
    }

    [Fact]
    public void PublicApiRoutes_RemainStableForFinal2Submission()
    {
        var actual = typeof(Program).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                typeof(ControllerBase).IsAssignableFrom(type) &&
                type.Namespace?.StartsWith("WebApp.ApiControllers", StringComparison.Ordinal) == true)
            .SelectMany(GetRouteSignatures)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "DELETE api/v{version:apiVersion}/{gymCode}/bookings/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/equipment-models/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/equipment/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/gym-users/{appUserId:guid}/{roleName}",
            "DELETE api/v{version:apiVersion}/{gymCode}/maintenance-tasks/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/members/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/membership-packages/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/memberships/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/staff/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/training-categories/{id:guid}",
            "DELETE api/v{version:apiVersion}/{gymCode}/training-sessions/{id:guid}",
            "GET api/v{version:apiVersion}/{gymCode}/bookings",
            "GET api/v{version:apiVersion}/{gymCode}/equipment",
            "GET api/v{version:apiVersion}/{gymCode}/equipment-models",
            "GET api/v{version:apiVersion}/{gymCode}/gym-settings",
            "GET api/v{version:apiVersion}/{gymCode}/gym-users",
            "GET api/v{version:apiVersion}/{gymCode}/maintenance-tasks",
            "GET api/v{version:apiVersion}/{gymCode}/member-workspace/me",
            "GET api/v{version:apiVersion}/{gymCode}/member-workspace/members/{memberId:guid}",
            "GET api/v{version:apiVersion}/{gymCode}/members",
            "GET api/v{version:apiVersion}/{gymCode}/members/me",
            "GET api/v{version:apiVersion}/{gymCode}/members/{id:guid}",
            "GET api/v{version:apiVersion}/{gymCode}/membership-packages",
            "GET api/v{version:apiVersion}/{gymCode}/memberships",
            "GET api/v{version:apiVersion}/{gymCode}/payments",
            "GET api/v{version:apiVersion}/{gymCode}/staff",
            "GET api/v{version:apiVersion}/{gymCode}/training-categories",
            "GET api/v{version:apiVersion}/{gymCode}/training-sessions",
            "GET api/v{version:apiVersion}/{gymCode}/training-sessions/{id:guid}",
            "GET api/v{version:apiVersion}/system/gyms",
            "GET api/v{version:apiVersion}/system/gyms/{gymId:guid}/snapshot",
            "GET api/v{version:apiVersion}/system/platform/analytics",
            "POST api/v{version:apiVersion}/{gymCode}/bookings",
            "POST api/v{version:apiVersion}/{gymCode}/equipment",
            "POST api/v{version:apiVersion}/{gymCode}/equipment-models",
            "POST api/v{version:apiVersion}/{gymCode}/gym-users",
            "POST api/v{version:apiVersion}/{gymCode}/maintenance-tasks",
            "POST api/v{version:apiVersion}/{gymCode}/maintenance-tasks/generate-due",
            "POST api/v{version:apiVersion}/{gymCode}/members",
            "POST api/v{version:apiVersion}/{gymCode}/membership-packages",
            "POST api/v{version:apiVersion}/{gymCode}/memberships",
            "POST api/v{version:apiVersion}/{gymCode}/payments",
            "POST api/v{version:apiVersion}/{gymCode}/staff",
            "POST api/v{version:apiVersion}/{gymCode}/training-categories",
            "POST api/v{version:apiVersion}/{gymCode}/training-sessions",
            "POST api/v{version:apiVersion}/account/forgot-password",
            "POST api/v{version:apiVersion}/account/login",
            "POST api/v{version:apiVersion}/account/logout",
            "POST api/v{version:apiVersion}/account/register",
            "POST api/v{version:apiVersion}/account/renew-refresh-token",
            "POST api/v{version:apiVersion}/account/reset-password",
            "POST api/v{version:apiVersion}/account/switch-gym",
            "POST api/v{version:apiVersion}/account/switch-role",
            "POST api/v{version:apiVersion}/system/gyms",
            "PUT api/v{version:apiVersion}/{gymCode}/bookings/{id:guid}/attendance",
            "PUT api/v{version:apiVersion}/{gymCode}/equipment-models/{id:guid}",
            "PUT api/v{version:apiVersion}/{gymCode}/equipment/{id:guid}",
            "PUT api/v{version:apiVersion}/{gymCode}/gym-settings",
            "PUT api/v{version:apiVersion}/{gymCode}/maintenance-tasks/{id:guid}/assignment",
            "PUT api/v{version:apiVersion}/{gymCode}/maintenance-tasks/{id:guid}/status",
            "PUT api/v{version:apiVersion}/{gymCode}/members/{id:guid}",
            "PUT api/v{version:apiVersion}/{gymCode}/membership-packages/{id:guid}",
            "PUT api/v{version:apiVersion}/{gymCode}/memberships/{id:guid}/status",
            "PUT api/v{version:apiVersion}/{gymCode}/staff/{id:guid}",
            "PUT api/v{version:apiVersion}/{gymCode}/training-categories/{id:guid}",
            "PUT api/v{version:apiVersion}/{gymCode}/training-sessions/{id:guid}",
            "PUT api/v{version:apiVersion}/system/gyms/{gymId:guid}/activation",
        }
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private static bool HasProducesErrorProblemDetails(Type controllerType)
    {
        return EnumerateControllerHierarchy(controllerType)
            .SelectMany(type => type.GetCustomAttributes(typeof(ProducesErrorResponseTypeAttribute), inherit: false)
                .Cast<ProducesErrorResponseTypeAttribute>())
            .Any(attribute => attribute.Type == typeof(ProblemDetails));
    }

    private static bool HasProblemDetailsStatusCode(Type controllerType, int statusCode)
    {
        return EnumerateControllerHierarchy(controllerType)
            .SelectMany(type => type.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: false)
                .Cast<ProducesResponseTypeAttribute>())
            .Any(attribute => attribute.StatusCode == statusCode && attribute.Type == typeof(ProblemDetails));
    }

    private static IEnumerable<Type> EnumerateControllerHierarchy(Type controllerType)
    {
        for (var current = controllerType; current != null && typeof(ControllerBase).IsAssignableFrom(current); current = current.BaseType)
        {
            yield return current;
        }
    }

    private static void AssertActionContract(string actionName, string expectedTemplate, Type? requestType, Type responseType)
    {
        var method = typeof(AccountController).GetMethod(actionName)
                     ?? throw new InvalidOperationException($"{actionName} was not found.");

        var post = Assert.Single(method
            .GetCustomAttributes(typeof(HttpPostAttribute), inherit: false)
            .Cast<HttpPostAttribute>());

        Assert.Equal(expectedTemplate, post.Template);
        Assert.Equal(typeof(Task<>).MakeGenericType(typeof(ActionResult<>).MakeGenericType(responseType)), method.ReturnType);

        var bodyParameter = method.GetParameters()
            .FirstOrDefault(parameter => parameter.GetCustomAttributes(typeof(FromBodyAttribute), inherit: false).Any());

        if (requestType is null)
        {
            Assert.Null(bodyParameter);
        }
        else
        {
            Assert.NotNull(bodyParameter);
            Assert.Equal(requestType, bodyParameter!.ParameterType);
        }
    }

    private static IEnumerable<string> GetRouteSignatures(Type controllerType)
    {
        var controllerRoute = Assert.Single(controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>());

        var baseTemplate = controllerRoute.Template?.Trim('/') ?? string.Empty;
        var methods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            foreach (var httpMethodAttribute in method.GetCustomAttributes<HttpMethodAttribute>(inherit: false))
            {
                var actionTemplate = httpMethodAttribute.Template?.Trim('/');
                var route = string.IsNullOrWhiteSpace(actionTemplate)
                    ? baseTemplate
                    : $"{baseTemplate}/{actionTemplate}";

                foreach (var httpMethod in httpMethodAttribute.HttpMethods)
                {
                    yield return $"{httpMethod} {route}";
                }
            }
        }
    }
}
