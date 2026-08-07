# Dplus_Desktop.ImageControls

## Dplus_Desktop.ILayer

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Visible | bool | true | Gets or sets whether the layer is visible. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Render | Graphics, Matrix | void | Renders the layer to the specified graphics context with the given image transform. |

## Dplus_Desktop.OverlayLayer

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Visible | bool | true | Gets or sets whether the layer is visible. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Render | Graphics, Matrix | void | Renders the overlay to the specified graphics context with the given image transform. |

## Dplus_Desktop.PointOverlay

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Location | PointF | - | The point location. |
| Radius | float | 4 | The radius of the point. |
| Pen | Pen? | null | Optional pen for drawing the outline. |
| Brush | Brush | Brushes.Lime | Brush for filling the point. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Draw | Graphics | void | Draws the point overlay. |

## Dplus_Desktop.LineOverlay

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| P1 | PointF | - | First endpoint of the line. |
| P2 | PointF | - | Second endpoint of the line. |
| Pen | Pen | Pens.Red | Pen for drawing the line. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Draw | Graphics | void | Draws the line overlay. |

## Dplus_Desktop.PolygonOverlay

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Points | List<PointF> | new() | The polygon points. |
| Pen | Pen? | Pens.Yellow | Optional pen for drawing the outline. |
| Brush | Brush? | null | Optional brush for filling. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Draw | Graphics | void | Draws the polygon overlay. |

## Dplus_Desktop.TextOverlay

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Text | string | empty | The text to display. |
| Location | PointF | - | The text location. |
| Font | Font | SystemFonts.DefaultFont | The font. |
| Brush | Brush | Brushes.White | Brush for the text. |
| DrawBackground | bool | false | Whether to draw a background rectangle. |
| BackgroundBrush | Brush | Brushes.Black | Brush for the background. |
| Padding | float | 2 | Padding around the background. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Draw | Graphics | void | Draws the text overlay. |

## Dplus_Desktop.PrimitiveOverlayLayer

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Points | List<PointOverlay> | new() | The point overlays. |
| Lines | List<LineOverlay> | new() | The line overlays. |
| Polygons | List<PolygonOverlay> | new() | The polygon overlays. |
| Texts | List<TextOverlay> | new() | The text overlays. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Render | Graphics, Matrix | void | Renders all overlay elements to the specified graphics context with the given image transform. |
| ClearLayers | - | void | Clears all overlay elements. |

## Dplus_Desktop.ImageControls

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| DisplayedImage | Image? | - | Gets or sets the displayed image. |
| Layers | IReadOnlyList<ILayer> | new() | Gets the list of layers. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| AddLayer | ILayer | void | Adds a layer to the controls. |
| RemoveLayer | ILayer | void | Removes a layer from the controls. |
| ClearLayers | - | void | Clears all layers. |
| ResetView | - | void | Resets the view to default zoom and pan. |
| ZoomToFit | - | void | Zooms to fit the displayed image. |
| ScreenToImage | PointF | PointF | Converts screen coordinates to image coordinates. |
| ImageToScreen | PointF | PointF | Converts image coordinates to screen coordinates. |

## Dplus_Desktop.OrthographicViewer

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| ShowGrid | bool | true | Whether to show the grid. |
| ShowAxes | bool | true | Whether to show the axes. |
| ShowDebug | bool | false | Whether to show debug information. |
| GridSpacing | float | 10 | The grid spacing. |
| settings | OrthographicViewerSettings | - | The viewer settings. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Clear | - | void | Clears all data. |
| AddPoint | float, float, Color | void | Adds a point to the viewer. |
| AddLine | PointF, PointF, Color | void | Adds a line to the viewer. |
| AddLabel | string, PointF | void | Adds a label to the viewer. |
| AutoFit | - | void | Auto-fits the view to all data. |
| WorldToScreen | PointF | PointF | Converts world coordinates to screen coordinates. |
| ScreenToWorld | PointF | PointF | Converts screen coordinates to world coordinates. |

## Dplus_Desktop.BoundingBox

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Min | Vector3 | - | The minimum corner of the bounding box. |
| Max | Vector3 | - | The maximum corner of the bounding box. |
| IsValid | bool | - | Gets whether the bounding box is valid. |

## Dplus_Desktop.CameraModel

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| Intrinsics | Intrinsics | - | The camera intrinsics. |
| Extrinsics | Extrinsics | - | The camera extrinsics. |
| WorldTransform | Matrix4x4 | - | The world transform. |
| ViewTransform | Matrix4x4 | computed | Gets the view transform. |
| Position | Vector3 | computed | Gets the camera position. |
| Right | Vector3 | computed | Gets the right vector. |
| Up | Vector3 | computed | Gets the up vector. |
| Forward | Vector3 | computed | Gets the forward vector. |
| NearClip | float | - | The near clip plane distance. |
| FarClip | float | - | The far clip plane distance. |
| AspectRatio | float | - | The aspect ratio. |
| HorizontalFov | float | - | The horizontal field of view in degrees. |
| VerticalFov | float | - | The vertical field of view in degrees. |
| Color | Color | - | The display color. |
| ShowOrigin | bool | true | Whether to show the origin marker. |
| ShowAxes | bool | true | Whether to show the axes. |
| ShowFrustum | bool | true | Whether to show the frustum. |
| ShowImagePlane | bool | true | Whether to show the image plane. |
| ShowCenterRay | bool | false | Whether to show the center ray. |
| ShowLabel | bool | true | Whether to show the label. |
| Name | string | computed | Gets the camera name. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| TransformPoint | Vector3 | Vector3 | Transforms a point from local to world coordinates. |
| TransformDirection | Vector3 | Vector3 | Transforms a direction from local to world coordinates. |

## Dplus_Desktop.PerspectiveViewer

| Type | Value | Description |
| ---- | ----- | ----------- |

### Properties

| Name | Type | Value | Description |
| ---- | ---- | ----- | ----------- |
| ShowAxes | bool | true | Whether to show the axes. |
| ShowDebug | bool | false | Whether to show debug information. |

### Methods

| Name | Parameters | Returns | Description |
| ---- | ---------- | ------- | ----------- |
| Clear | - | void | Clears all data. |
| AddPoint | float, float, float, Color | void | Adds a point to the viewer. |
| AddLine | Vector3, Vector3, Color | void | Adds a line to the viewer. |
| AddCamera | CameraModel | void | Adds a camera to the viewer. |
| AutoCenter | - | void | Auto-centers the view on all points. |
| ComputeBoundingBox | - | BoundingBox | Computes the bounding box of all points. |
