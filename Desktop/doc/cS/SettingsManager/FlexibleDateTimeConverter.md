# FlexibleDateTimeConverter

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Custom JSON converter for DateTime? values that supports multiple input formats, enabling backward compatibility with settings files from different versions.

## Constructors

### `FlexibleDateTimeConverter()`

Default constructor initializes the supported format list.

```csharp
public FlexibleDateTimeConverter()
{
    formats = new[]
    {
        "yyyy-MM-dd HH:mm:ss",
        "MM-dd-yyyy hh:mm:sstt",
        "M-d-yyyy h:mm:sstt",
        "yyyy-MM-ddTHH:mm:ss",
    };
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `formats` | `string[]` | Array of supported datetime formats for parsing |

## Methods

### `Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)`

Parses a JSON string value to DateTime?. Tries multiple formats in order. Returns null if parsing fails or value is not a string.

```csharp
public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
```

**Parameters:**
- `reader`: JSON reader positioned at the value to convert
- `typeToConvert`: Expected type (should be DateTime?)
- `options`: Serialization options (not used by this converter)

**Returns:** `DateTime?` - Parsed datetime or null if invalid

**Supported Formats:**
- `yyyy-MM-dd HH:mm:ss` - ISO format with space separator
- `MM-dd-yyyy hh:mm:sstt` - US-style with AM/PM
- `M-d-yyyy h:mm:sstt` - Short month/day with AM/PM  
- `yyyy-MM-ddTHH:mm:ss` - ISO format with T separator

**Example:**
```csharp
var options = new JsonSerializerOptions { Converters = { new FlexibleDateTimeConverter() } };
string json = @"{ ""LastModifiedTime"": ""2024-07-15 14:30:00"" }";
var obj = JsonSerializer.Deserialize<object>(json, options);
// LastModifiedTime will be parsed as DateTime
```

### `Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)`

Writes a DateTime? to JSON. If value is null, writes null. Otherwise writes in ISO format with space separator.

```csharp
public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
```

**Parameters:**
- `writer`: JSON writer to write to
- `value`: DateTime? to convert
- `options`: Serialization options (not used by this converter)

**Example:**
```csharp
var writer = new Utf8JsonWriter(stream);
DateTime? dt = new DateTime(2024, 7, 15, 14, 30, 0);
writer.WriteNullValue(); // or write the value
```

## Usage Example

```csharp
// Use in JsonSerializerOptions
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new FlexibleDateTimeConverter() }
};

string json = @"{ ""LastModifiedTime"": ""2024-07-15 14:30:00"" }";
var settings = JsonSerializer.Deserialize<AppSettings>(json, options);

// The converter handles multiple input formats gracefully
```

## Related Types

- `AppSettings` - Uses FlexibleDateTimeConverter in LoadSettings()
- `FlexibleDateTimeConverter` is used internally by SettingsManager for all DateTime? properties
