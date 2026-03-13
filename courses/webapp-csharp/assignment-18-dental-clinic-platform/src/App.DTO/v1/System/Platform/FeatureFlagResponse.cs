namespace App.DTO.v1.System.Platform;

public class FeatureFlagResponse
{
    public string Key { get; set; } = default!;
    public bool Enabled { get; set; }
}
