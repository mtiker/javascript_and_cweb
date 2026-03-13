namespace WebApp.Helpers;

public interface IFeatureFlagStore
{
    IReadOnlyCollection<KeyValuePair<string, bool>> GetAll();
    void Set(string key, bool enabled);
}
