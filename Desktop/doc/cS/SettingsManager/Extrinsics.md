# Extrinsics

**Namespace:** `Dplus_Desktop.Config`

## Purpose

Represents extrinsic calibration data describing the spatial transformation (rotation and translation) from one node's camera coordinate system to another node's camera coordinate system. Used for multi-camera 3D reconstruction.

## Constructors

### `Extrinsics()`

Default constructor initializes with empty values.

```csharp
public Extrinsics()
{
    baseNodeName = string.Empty;
    targetNodeName = string.Empty;
    R = new double[][] { new double[3], new double[3], new double[3] };
    t = new double[] { 0, 0, 0 };
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `baseNodeName` | `string` | Source node name (coordinate system origin) |
| `targetNodeName` | `string` | Target node name (coordinates are expressed in this frame) |
| `R` | `double[][]` | 3×3 rotation matrix (row-major: R[i][j] = row i, column j) |
| `t` | `double[]` | Translation vector [tx, ty, tz] meters |

## Static Methods

### `Identity(string nodeName)`

Creates identity extrinsics for a node (same node as base and target).

```csharp
public static Extrinsics Identity(string nodeName)
```

**Parameters:**
- `nodeName`: The node name to use for both base and target

**Returns:** `Extrinsics` with R = I (identity matrix), t = [0, 0, 0]

**Example:**
```csharp
var identity = Extrinsics.Identity("Node1");
// Use when computing coordinates in the same node's frame
```

## Equality Operators

Value equality comparison based on node names and transformation matrices.

```csharp
public static bool operator ==(Extrinsics a, Extrinsics b)
public static bool operator !=(Extrinsics a, Extrinsics b)
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
// Get extrinsics from Node1 to Node2
Extrinsics node1ToNode2 = Settings.All.GetExtrinsicsForNode("Node1", "Node2");

// Apply transformation to a point (in Node1 frame)
Vector3d pointInNode1 = new Vector3d(0.5, 0.3, 1.2);
Vector3d pointInNode2 = TransformPoint(node1ToNode2, pointInNode1);

// Transform function:
// p_node2 = R * p_node1 + t
static Vector3d TransformPoint(Extrinsics extr, Vector3d p)
{
    return new Vector3d(
        extr.R[0][0]*p.X + extr.R[0][1]*p.Y + extr.R[0][2]*p.Z + extr.t[0],
        extr.R[1][0]*p.X + extr.R[1][1]*p.Y + extr.R[1][2]*p.Z + extr.t[1],
        extr.R[2][0]*p.X + extr.R[2][1]*p.Y + extr.R[2][2]*p.Z + extr.t[2]
    );
}

// Create identity for Node1
var identity = Extrinsics.Identity("Node1");

// Compare extrinsics to see if they're the same
if (node1ToNode2 == identity)
{
    Console.WriteLine("Node1 and target are co-located");
}
```

## Related Types

- `AppSettings` - Parent settings container with Extrinsics collection
- `Intrinsics` - Companion calibration data for cameras
- `Device` - Contains node names referenced in extrinsics
