# Intrinsics

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents camera intrinsic calibration parameters for a single camera. Contains focal length (K matrix), distortion coefficients, and RMS error from calibration. Supports comparison operators based on RMS quality.

## Constructors

### `Intrinsics()`

Default constructor initializes with placeholder values indicating no calibration data.

```csharp
public Intrinsics()
{
    CameraIDnumber = 0;
    Rms = double.MaxValue;
    ImageWidth = 640;
    ImageHeight = 480;
    K = new double[3][] { new double[3], new double[3], new double[3] };
    Dist = Array.Empty<double>();
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `CameraIDnumber` | `int` | Unique camera identifier |
| `Rms` | `double` | Root mean square error from calibration (lower is better) |
| `ImageWidth` | `int` | Image width in pixels |
| `ImageHeight` | `int` | Image height in pixels |
| `K` | `double[][]` | 3×3 intrinsic matrix [fx, 0, cx; 0, fy, cy; 0, 0, 1] |
| `Dist` | `double[]` | Distortion coefficients (length varies by model) |

## Methods

### `Project(Point2f normalized)`

Projects normalized coordinates (-1 to +1) to pixel coordinates using intrinsic matrix.

```csharp
public PointF Project(Point2f normalized)
```

**Parameters:**
- `normalized`: Point in normalized image space (x, y ∈ [-1, 1])

**Returns:** `PointF` - Pixel coordinates (x, y)

**Example:**
```csharp
var intrinsics = Settings.All.GetIntrinsicsForCameraID(42);
var pixelCoords = intrinsics.Project(new Point2f(0.5f, 0.3f));
// pixelCoords.X ≈ fx * 0.5 + cx
```

### `Project(Rect2f normalizedBox)`

Projects a normalized rectangle to pixel coordinates.

```csharp
public Rect2f Project(Rect2f normalizedBox)
```

**Parameters:**
- `normalizedBox`: Rectangle in normalized space with width/height ≤ 2

**Returns:** `Rect2f` - Pixel rectangle

## Comparison Operators

All operators compare based on RMS error (lower is better). Throws `ArgumentNullException` if either operand is null.

```csharp
public static bool operator <(Intrinsics a, Intrinsics b)
public static bool operator >(Intrinsics a, Intrinsics b)
public static bool operator <=(Intrinsics a, Intrinsics b)
public static bool operator >=(Intrinsics a, Intrinsics b)
```

**Example:**
```csharp
var intr1 = Settings.All.GetIntrinsicsForCameraID(42);
var intr2 = Settings.All.GetIntrinsicsForCameraID(43);

if (intr1 < intr2)
{
    Console.WriteLine("Camera 42 has better calibration");
}
```

## Equality Operators

Value equality comparison based on all properties.

```csharp
public static bool operator ==(Intrinsics a, Intrinsics b)
public static bool operator !=(Intrinsics a, Intrinsics b)
```

### `Equals(object obj)`

Standard object equality override.

```csharp
public override bool Equals(object obj)
```

### `GetHashCode()`

Hash code generation for use in collections.

```csharp
public override int GetHashCode()
```

## Usage Example

```csharp
// Get intrinsics for a node
Intrinsics intrinsics = Settings.All.GetIntrinsicsForNode("Node1");

// Project a normalized box (e.g., from YOLO output)
var normBox = new Rect2f(0.25f, 0.3f, 0.15f, 0.2f);
var pixelRect = intrinsics.Project(normBox);

// Find best intrinsics for a camera ID
int cameraID = 42;
Intrinsics best = Settings.All.GetIntrinsicsForCameraID(cameraID);

// Sort all intrinsics by quality (best first)
var sorted = Settings.All.Intrinsics.OrderBy(i => i.Rms).ToList();

// Access K matrix elements
double fx = intrinsics.K[0][0];  // Focal length X
double cx = intrinsics.K[0][2];  // Principal point X
```

## Related Types

- `AppSettings` - Parent settings container with Intrinsics collection
- `Extrinsics` - Companion calibration data for node transformations
- `Device` - Contains CameraIDnumber for intrinsics lookup
