using System.Collections.Concurrent;

namespace WebApp.Helpers;

public class FeatureFlagStore : IFeatureFlagStore
{
    private readonly ConcurrentDictionary<string, bool> _flags = new(StringComparer.OrdinalIgnoreCase);

    public FeatureFlagStore()
    {
        _flags["advanced-reports"] = false;
        _flags["insurance-automation"] = false;
        _flags["ai-treatment-suggestions"] = false;
    }

    public IReadOnlyCollection<KeyValuePair<string, bool>> GetAll()
    {
        return _flags
            .OrderBy(entity => entity.Key)
            .ToList();
    }

    public void Set(string key, bool enabled)
    {
        var normalized = key.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return;
        }

        _flags[normalized] = enabled;
    }
}
