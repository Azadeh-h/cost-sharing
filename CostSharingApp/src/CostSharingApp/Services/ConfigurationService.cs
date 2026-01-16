// <copyright file="ConfigurationService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Reflection;
using System.Text.Json;

namespace CostSharingApp.Services;

/// <summary>
/// Service for reading application configuration from appsettings.json.
/// </summary>
public class ConfigurationService
{
    private readonly Dictionary<string, JsonElement> config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationService"/> class.
    /// </summary>
    public ConfigurationService()
    {
        this.config = new Dictionary<string, JsonElement>();
        try
        {
            this.LoadConfiguration();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ConfigurationService initialization failed: {ex}");
            // Continue with empty config - app can still work
        }
    }

    /// <summary>
    /// Gets a configuration value by key path (e.g., "GoogleDrive:ClientId").
    /// </summary>
    /// <param name="keyPath">Colon-separated key path.</param>
    /// <returns>Configuration value or null if not found.</returns>
    public string? GetValue(string keyPath)
    {
        try
        {
            var keys = keyPath.Split(':');
            JsonElement current = default;

            foreach (var key in keys)
            {
                if (current.ValueKind == JsonValueKind.Undefined)
                {
                    if (!this.config.TryGetValue(key, out current))
                    {
                        return null;
                    }
                }
                else
                {
                    if (!current.TryGetProperty(key, out current))
                    {
                        return null;
                    }
                }
            }

            return current.GetString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get config value for {keyPath}: {ex.Message}");
            return null;
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CostSharingApp.appsettings.json";

            // Load base settings
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                using var doc = JsonDocument.Parse(json);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    // Parse and store as new JsonDocument to avoid disposal issues
                    var propJson = prop.Value.GetRawText();
                    using var propDoc = JsonDocument.Parse(propJson);
                    this.config[prop.Name] = propDoc.RootElement.Clone();
                }
            }

#if DEBUG
            // Try to load Development settings and override
            var devResourceName = "CostSharingApp.appsettings.Development.json";
            using var devStream = assembly.GetManifestResourceStream(devResourceName);
            if (devStream != null)
            {
                using var devReader = new StreamReader(devStream);
                var devJson = devReader.ReadToEnd();
                using var devDoc = JsonDocument.Parse(devJson);
                foreach (var prop in devDoc.RootElement.EnumerateObject())
                {
                    var propJson = prop.Value.GetRawText();
                    using var propDoc = JsonDocument.Parse(propJson);
                    this.config[prop.Name] = propDoc.RootElement.Clone();
                }
            }
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load configuration: {ex.Message}");
        }
    }
}
