# Main

**Namespace:** `Dplus_Desktop`

## Purpose

Main entry point form for the Dplus Desktop application. Provides buttons to launch Uploader and Viewer forms, manages singleton instances of these forms, and configures logging sinks.

## Constructors

### `Main()`

Constructor that initializes the form and sets up logging.

```csharp
public Main()
{
    InitializeComponent();

    _tbSink = new RichTextBoxSink(LiveLoggingBox);
    _tbSink.AddSource("CamManager", Color.Red, true);
    logger.AddSink(_tbSink);
    logger.Log(LogLevel.INFO, "CamManager", "_tbSink Added");

    _tfSink = new TextFileSink(Path.Combine(Settings.All.LocalLogPath, "CamManager"), "CamManager", ".log");
    logger.AddSink(_tfSink);
    logger.Log(LogLevel.INFO, "CamManager", "_tfSink Added: " + Settings.All.LocalLogPath);

    logger.LogHeading(LogLevel.INFO, "CamManager", "Main Initialized");
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `_uploader` | `Uploader?` | Singleton instance of Uploader form |
| `_viewer` | `Viewer?` | Singleton instance of Viewer form |

## Methods

### `AddLogSource(string source, Color color = default, bool andModules = true)`

Adds a new log source to the rich text box sink.

```csharp
private void AddLogSource(string source, Color color = default, bool andModules = true)
{
    _tbSink.AddSource(source, color, andModules);
    logger.Log(LogLevel.INFO, "CamManager", $"Added source '{source}' to _tbSink");
}
```

**Parameters:**
- `source`: Name of the source to add
- `color`: Color for the source in the log (default: system default)
- `andModules`: Whether to include related modules (default: true)

### `Uploader_Button_Click(object sender, EventArgs e)`

Button click handler that launches or brings to front the Uploader form.

```csharp
private void Uploader_Button_Click(object sender, EventArgs e)
{
    if (_uploader == null || _uploader.IsDisposed)
    {
        AddLogSource("Uploader", Color.Green, true);
        _uploader = new Uploader();
        _uploader.FormClosed += (s, args) => _uploader = null; // cleanup
        _uploader.Show();
    }
    else
    {
        _uploader.BringToFront();
        _uploader.Focus();
    }
}
```

### `Viewer_Button_Click(object sender, EventArgs e)`

Button click handler that launches or brings to front the Viewer form.

```csharp
private void Viewer_Button_Click(object sender, EventArgs e)
{
    if (_viewer == null || _viewer.IsDisposed)
    {
        _viewer = new Viewer();
        _viewer.FormClosed += (s, args) => _viewer = null; // cleanup
        _viewer.Show(this);
    }
    else
    {
        _viewer.BringToFront();
        _viewer.Focus();
    }
}
```

## Usage Example

```csharp
// Create and show main form
using (Main main = new Main())
{
    main.ShowDialog();
}

// Access Uploader and Viewer buttons
var uploaderBtn = main.Uploader_Button;
var viewerBtn = main.Viewer_Button;
```

## Related Types

- `Uploader` - Form launched by Uploader_Button_Click
- `Viewer` - Form launched by Viewer_Button_Click
- `RichTextBoxSink`, `TextFileSink` - Logging sinks configured in constructor
