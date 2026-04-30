using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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
}
