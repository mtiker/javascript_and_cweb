using System.Text.Json.Serialization;

namespace App.Domain.Common;

public sealed class LangStr
{
    public static string DefaultCulture { get; set; } = "en";

    [JsonInclude]
    public Dictionary<string, string> Translations { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public int Count => Translations.Count;

    public ICollection<string> Values => Translations.Values;

    public string this[string culture]
    {
        get => Translations[culture];
        set => Translations[culture] = value;
    }

    [JsonConstructor]
    public LangStr()
    {
    }

    public LangStr(string value, string? culture = null)
    {
        SetTranslation(value, culture);
    }

    public string? Translate(string? culture = null)
    {
        if (Count == 0)
        {
            return null;
        }

        culture = string.IsNullOrWhiteSpace(culture)
            ? DefaultCulture
            : culture.Trim();

        if (Translations.TryGetValue(culture, out var exact))
        {
            return exact;
        }

        var neutral = culture.Split('-')[0];
        if (Translations.TryGetValue(neutral, out var neutralValue))
        {
            return neutralValue;
        }

        if (Translations.TryGetValue(DefaultCulture, out var fallback))
        {
            return fallback;
        }

        return Values.FirstOrDefault();
    }

    public void SetTranslation(string value, string? culture = null)
    {
        culture = string.IsNullOrWhiteSpace(culture)
            ? DefaultCulture
            : culture.Trim();

        var neutral = culture.Split('-')[0];
        Translations[neutral] = value;

        if (!Translations.ContainsKey(DefaultCulture))
        {
            Translations[DefaultCulture] = value;
        }
    }

    public bool TryGetValue(string culture, out string value)
    {
        return Translations.TryGetValue(culture, out value!);
    }

    public override string ToString()
    {
        return Translate() ?? string.Empty;
    }

    public static implicit operator LangStr(string value)
    {
        return new LangStr(value);
    }

    public static implicit operator string?(LangStr? value)
    {
        return value?.ToString();
    }
}
