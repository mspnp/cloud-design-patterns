namespace Microsoft.Extensions.Configuration;

public static class SampleSettingsValidator
{
    /// <summary>
    /// Ensure the required app settings are present
    /// </summary>
    /// <param name="settings"></param>
    /// <exception cref="ApplicationException"></exception>
    public static void ThrowIfMissingSettings(this IConfiguration configuration, List<string> settings)
    {
        if (settings.Any(option => string.IsNullOrWhiteSpace(configuration.GetSection(option).Value)))
        {
            throw new ApplicationException($"Configure options {string.Join(", ", settings)} in appsettings.json and run again.");
        }
    }

}