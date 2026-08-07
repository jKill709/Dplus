# OrthographicViewerSettings

**Namespace:** `Dplus_Desktop.SettingsManager`

## Purpose

Defines configuration for an orthographic 3D viewer profile, including grid spacing parameters. Each profile can be selected via AppSettings.OrthographicViewerProfileToUse.

## Constructors

### `OrthographicViewerSettings()`

Default constructor initializes with empty name and default grid spacing.

```csharp
public OrthographicViewerSettings()
{
    profileName = string.Empty;
    MinGridSpacing = 1;
    MaxGridSpacing = 1;
    GridSpacing = 10;
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `profileName` | `string` | Unique profile name (used as identifier) |
| `MinGridSpacing` | `int` | Minimum grid spacing in pixels |
| `MaxGridSpacing` | `int` | Maximum grid spacing in pixels |
| `GridSpacing` | `float` | Base grid spacing in pixels (typically 10) |

## Usage Example

```csharp
// Create a viewer settings profile
OrthographicViewerSettings settings = new OrthographicViewerSettings
{
    profileName = "Standard",
    MinGridSpacing = 20,
    MaxGridSpacing = 100,
    GridSpacing = 10.0f
};

// Access from settings
OrthographicViewerSettings activeSettings = Settings.All.GetOrthographicViewerSettings();

Console.WriteLine($"Profile: {activeSettings.profileName}");
Console.WriteLine($"Grid spacing: {activeSettings.GridSpacing:F1}px " +
                  $"({activeSettings.MinGridSpacing}-{activeSettings.MaxGridSpacing}px range)");

// Modify settings
activeSettings.GridSpacing = 15.0f;
```

## Related Types

- `AppSettings` - Contains OrthoViewerSettings list and profile accessor
- `OrthographicViewerProfileToUse` - Property in ClusterProfile that references this profile
