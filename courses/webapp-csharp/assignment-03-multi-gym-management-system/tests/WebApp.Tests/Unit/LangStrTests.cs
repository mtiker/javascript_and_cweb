using App.Domain.Common;

namespace WebApp.Tests.Unit;

public class LangStrTests
{
    [Fact]
    public void Translate_FallsBackToDefaultCulture_WhenExactCultureMissing()
    {
        var value = new LangStr
        {
            ["en"] = "Strength session",
            ["et"] = "Joutreening"
        };

        var result = value.Translate("fr-FR");

        Assert.Equal("Strength session", result);
    }
}
