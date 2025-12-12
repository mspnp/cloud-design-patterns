namespace Microsoft.Extensions.Configuration;

public static class SampleSettingsValidator
{
    /// <summary>
    /// Ensure the required app settings are present
    /// </summary>
    /// <param name="settings"></param>
    /// <exception cref="ApplicationException"></exception>
    public static void ThrowIfMissingSettings(this IConfiguration configuration, List<string> requiredSettings)
    {
        var missingSettings = requiredSettings.Where(option => string.IsNullOrWhiteSpace(configuration.GetSection(option).Value)).ToList();
        if (missingSettings.Any())
        {
            throw new ApplicationException($"Configure required options {string.Join(", ", missingSettings)} in appsettings.json and run again.");
        }
    }

}