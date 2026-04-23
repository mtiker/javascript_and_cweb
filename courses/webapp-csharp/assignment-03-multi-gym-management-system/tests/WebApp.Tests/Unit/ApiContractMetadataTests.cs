using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

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
}
