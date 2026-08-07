# Viewer

**Namespace:** `Dplus_Desktop`

## Purpose

Live streaming viewer form that receives frame data via MQTT from the embedded cluster, displays camera views with detections/reconstructions, and allows replay of saved frames. Supports multiple image types (captures, YOLO pose/object, face recognition, ChArUco, chessboard).

## Constructors

### `Viewer()`

Constructor that initializes the form, sets up logging, loads saved frames, and connects to MQTT.

```csharp
public Viewer()
{
    InitializeComponent();

    logger.LogHeading(mLogger.LogLevel.INFO, "Viewer", "Viewer Initialing");
    AddLogSource("Viewer", Color.Blue, true);

    // Add Overlays to ImageViewers
    _image1Overlay = new PrimitiveOverlayLayer();
    _image2Overlay = new PrimitiveOverlayLayer();
    Image1_View.AddLayer(_image1Overlay);
    Image2_View.AddLayer(_image2Overlay);

    // Set initial confidence thresholds from settings
    ClusterProfile cProfile = Settings.All.GetClusterProfile();
    Objectness_Bar.Value = (int)(Settings.All.getYoloPoseDetectorProfileByClusterID(cProfile.profileName).detectConfThreshold * 100);
    KP_Bar.Value = (int)(Settings.All.getYoloPoseDetectorProfileByClusterID(cProfile.profileName).kpDetectThreshold * 100);

    // Load saved frames from disk
    logger.Log(mLogger.LogLevel.INFO, "Viewer", "Loading Saved RigFrames...");
    LoadSavedRigFrames();
    logger.Log(mLogger.LogLevel.INFO, "Viewer", $"Loaded {_savedFrames.Count} RigFrames");
    if (_savedFrames.Count > 0)
    {
        SavedFrames_rButton.Checked = true;
        SavedFrames_Box.SelectedIndex = 0;
    }
    else
    {
        LiveView_rButton.Checked = true;
    }

    AutofitViewers();
    UpdateViewers();
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `_isSyncing` | `bool` | Flag to prevent recursive updates when changing selections programmatically |
| `_worker` | `MqttWorker` | MQTT worker for receiving live frames |
| `_savedFrames` | `List<RigFrame>` | List of saved RigFrame objects loaded from disk |
| `_liveFrames` | `List<RigFrame>` | List of live frames currently buffered |
| `_livePlayerState` | `LivePlayerState` | Current live player state (Disconnected, Play) |
| `_currentFrame` | `RigFrame?` | Currently selected frame for display |

## Methods

### `HandleIncomingFrame(string jsonString)`

Processes an incoming JSON frame from MQTT. Uses InvokeRequired pattern to marshal back to UI thread.

```csharp
private void HandleIncomingFrame(string jsonString)
{
    if (InvokeRequired)
    {
        BeginInvoke(new Action(() => HandleIncomingFrame(jsonString)));
        return;
    }

    logger.Log(mLogger.LogLevel.DEBUG, "Viewer", "Deserializing live frame...");
    var frame = JsonSerializer.Deserialize<RigFrame>(jsonString, Options);

    if (frame != null)
    {
        _liveFrames.Add(frame);

        LiveFrames_Bar.Minimum = 1;
        LiveFrames_Bar.Maximum = _liveFrames.Count;
        if (LiveFrames_Bar.Value > LiveFrames_Bar.Maximum)
        {
            LiveFrames_Bar.Value = LiveFrames_Bar.Maximum;
        }
        else if (LiveFrames_Bar.Value < LiveFrames_Bar.Minimum)
        {
            LiveFrames_Bar.Value = LiveFrames_Bar.Minimum;
        }

        logger.Log(mLogger.LogLevel.INFO, "Viewer", $"Received frame: CmdID={frame.commandID}, Time={frame.Timestamp:HH:mm:ss.fff}, Cams={frame.camFrames.Count}, PoseRecs={frame.poseRecs?.Count ?? 0}, ObjectRecs={frame.objectRecs?.Count ?? 0}, FaceRecs={frame.faceRecs?.Count ?? 0}, ChArucoRec={(frame.charucoRec != null ? "Yes" : "No")}");

        if (_livePlayerState == LivePlayerState.Disconected)
        {
            _livePlayerState = LivePlayerState.Play;
            LiveViewStatus_Label.Text = "Play";
        }
        if ((LiveView_rButton.Checked) && (_livePlayerState == LivePlayerState.Play))
        {
            _isSyncing = true;
            LiveFrames_Bar.Value = _liveFrames.Count;
            _isSyncing = false;
            UpdateViewers();
        }

        LiveViewInfo_Label.Text = $"{LiveFrames_Bar.Value}/{LiveFrames_Bar.Maximum}";
        LiveViewTimestamp_Label.Text = $"{_currentFrame?.Timestamp:HH:mm:ss.fff}";
        if (_liveFrames.Count >= 2)
        {
            var delta = _liveFrames[LiveFrames_Bar.Maximum - 1].Timestamp - _liveFrames[LiveFrames_Bar.Maximum - 2].Timestamp;
            LiveViewFPS_Label.Text = $"{1.0 / delta.TotalSeconds:F2} FPS";
        }
    }
}
```

**Parameters:**
- `jsonString`: JSON string containing frame data

### `LoadSavedRigFrames()`

Loads saved RigFrame files from disk into the viewer.

```csharp
private void LoadSavedRigFrames()
{
    try
    {
        string dirPath = Path.Combine(Settings.All.LocalLogPath, Settings.All.Hubs[0].Name, "Reconstructions");

        if (!Directory.Exists(dirPath))
        {
            logger.Log(mLogger.LogLevel.ERROR, "Viewer", $"Reconstructions directory not found:  {dirPath}");
            MessageBox.Show($"Reconstructions directory not found:\n{dirPath}");
            return;
        }

        var files = Directory.GetFiles(dirPath, "*.json");

        var framePairs = new List<(RigFrame Frame, string Name)>();

        foreach (var file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                string fileName = Path.GetFileNameWithoutExtension(file);
                try
                {
                    var rigFrame = JsonSerializer.Deserialize<RigFrame>(json, Options);

                    if (rigFrame != null)
                    {
                        framePairs.Add((rigFrame, fileName));
                    }
                    else
                    {
                        logger.Log(mLogger.LogLevel.ERROR, "Viewer", $"Failed to deserialize {fileName}: Result was null");
                        MessageBox.Show($"Failed to deserialize {fileName}: Result was null");
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(mLogger.LogLevel.DEBUG, "Viewer", $"Deserialize FAILED\n\n{ex.GetType()}\n{ex.Message}\n\n{ex.StackTrace}");
                    MessageBox.Show($"Deserialize FAILED\n\n{ex.GetType()}\n{ex.Message}\n\n{ex.StackTrace}");
                }
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.DEBUG, "Viewer", $"Failed to load {file}: {ex.Message}");
                MessageBox.Show($"Failed to load {file}: {ex.Message}");
            }
        }

        framePairs = framePairs.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var pair in framePairs)
        {
            _savedFrames.Add(pair.Frame);
            SavedFrames_Box.Items.Add(pair.Name);
        }
        SavedFramesBoxIndex_UpDown.Maximum = Math.Max(0, SavedFrames_Box.Items.Count - 1);
        SavedFrames_Box.SelectedIndex = 0;
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Error loading RigFrames:\n{ex.Message}");
    }
}
```

### `DisplayRigFrameData(System.Windows.Forms.TreeView treeView, RigFrame frame)`

Displays all reconstruction data from a RigFrame in a TreeView control.

```csharp
public void DisplayRigFrameData(System.Windows.Forms.TreeView treeView, RigFrame frame)
{
    treeView.Nodes.Clear();

    var root = new TreeNode($"RigFrame | Cmd: {frame.commandID} | Time: {frame.Timestamp:HH:mm:ss.fff}");

    // Camera Frames
    var camsNode = new TreeNode($"CameraFrames ({frame.camFrames.Count})");

    foreach (var cam in frame.camFrames)
    {
        var camNode = new TreeNode($"{cam.sourceName}");

        // Pose Detections
        var poseNode = new TreeNode($"PoseDets ({cam.poseDets?.Count ?? 0})");
        if (cam.poseDets != null)
        {
            for (int i = 0; i < cam.poseDets.Count; i++)
            {
                var det = cam.poseDets[i];
                var color = _colorProvider.GetColor(i);

                var detNode = new TreeNode(
                    $"[{i}] Conf={det.Confidence:F2}, Class={det.ClassId}, Box=({det.Box.X:F1},{det.Box.Y:F1},{det.Box.Width:F1},{det.Box.Height:F1})"
                )
                {
                    ForeColor = color
                };

                if (det.Keypoints != null && det.Keypoints.Count > 0)
                {
                    for (int k = 0; k < det.Keypoints.Count; k++)
                    {
                        var kp = det.Keypoints[k];
                        detNode.Nodes.Add($"[{k}] ({kp.X:F3}, {kp.Y:F3})  conf={det.Kp_Confidences[k]:F2}");
                    }
                }

                poseNode.Nodes.Add(detNode);
            }
        }
        camNode.Nodes.Add(poseNode);
        poseNode.Expand();

        // Object Detections
        var objNode = new TreeNode($"ObjectDets ({cam.objectDets?.Count ?? 0})");
        if (cam.objectDets != null)
        {
            for (int i = 0; i < cam.objectDets.Count; i++)
            {
                var det = cam.objectDets[i];
                var color = _colorProvider.GetColor(i);

                var detNode = new TreeNode(
                    $"[{i}] Conf={det.Confidence:F2}, Class={det.ClassId}, Box=({det.Box.X:F1},{det.Box.Y:F1},{det.Box.Width:F1},{det.Box.Height:F1})"
                )
                {
                    ForeColor = color
                };

                objNode.Nodes.Add(detNode);
            }
        }
        camNode.Nodes.Add(objNode);
        objNode.Expand();

        // Face Detections
        var faceNode = new TreeNode($"FaceDets ({cam.faceDets?.Count ?? 0})");
        if (cam.faceDets != null)
        {
            for (int i = 0; i < cam.faceDets.Count; i++)
            {
                var det = cam.faceDets[i];
                var color = _colorProvider.GetColor(i);

                var detNode = new TreeNode(
                    $"[{i}] Conf={det.Confidence:F2}, Class={det.ClassId}, Box=({det.Box.X:F1},{det.Box.Y:F1},{det.Box.Width:F1},{det.Box.Height:F1})"
                )
                {
                    ForeColor = color
                };

                faceNode.Nodes.Add(detNode);
            }
        }
        camNode.Nodes.Add(faceNode);
        faceNode.Expand();

        // ChArUco Detection
        if (cam.charucoDet != null)
        {
            var chNode = new TreeNode(
                $"ChArUco: {(cam.charucoDet.Valid ? "Valid" : "Invalid")} | IDs: {cam.charucoDet.CharucoIds.Count}"
            );

            for (int i = 0; i < cam.charucoDet.CharucoIds.Count; i++)
            {
                var id = cam.charucoDet.CharucoIds[i];
                var pt = cam.charucoDet.CharucoCorners[i];

                chNode.Nodes.Add($"ID {id}: ({pt.X:F1}, {pt.Y:F1})");
            }

            camNode.Nodes.Add(chNode);
            camNode.Expand();
        }

        camsNode.Nodes.Add(camNode);
        camsNode.Expand();
    }

    root.Nodes.Add(camsNode);
    camsNode.Expand();

    // Reconstructions
    var recNode = new TreeNode("Reconstructions");

    // Pose Reconstructions
    var poseRecNode = new TreeNode($"PoseRecs ({frame.poseRecs?.Count ?? 0})");
    if (frame.poseRecs != null)
    {
        for (int i = 0; i < frame.poseRecs.Count; i++)
        {
            var rec = frame.poseRecs[i];
            var color = _colorProvider.GetColor(i);

            var recNodeItem = new TreeNode(
                $"[{i}] Conf={rec.Confidence:F2}, Class={rec.ClassId}, Center=({rec.BoxCenter.X:F2},{rec.BoxCenter.Y:F2},{rec.BoxCenter.Z:F2})"
            )
            {
                ForeColor = color
            };

            if (rec.Keypoints != null && rec.Keypoints.Count > 0)
            {
                for (int k = 0; k < rec.Keypoints.Count; k++)
                {
                    var kp = rec.Keypoints[k];
                    recNodeItem.Nodes.Add($"[{k}] ({kp.X:F3}, {kp.Y:F3}, {kp.Z:F3})  conf={rec.Kp_Confidences[k]:F2}");
                }
            }

            poseRecNode.Nodes.Add(recNodeItem);
        }
    }
    recNode.Nodes.Add(poseRecNode);

    // Object Reconstructions
    var objRecNode = new TreeNode($"ObjectRecs ({frame.objectRecs?.Count ?? 0})");
    if (frame.objectRecs != null)
    {
        for (int i = 0; i < frame.objectRecs.Count; i++)
        {
            var rec = frame.objectRecs[i];
            var color = _colorProvider.GetColor(i);

            var recNodeItem = new TreeNode(
                $"[{i}] Conf={rec.Confidence:F2}, Class={rec.ClassId}, Center=({rec.BoxCenter.X:F2},{rec.BoxCenter.Y:F2},{rec.BoxCenter.Z:F2})"
            )
            {
                ForeColor = color
            };

            objRecNode.Nodes.Add(recNodeItem);
        }
    }
    recNode.Nodes.Add(objRecNode);

    // Face Reconstructions
    var faceRecNode = new TreeNode($"FaceRecs ({frame.faceRecs?.Count ?? 0})");
    if (frame.faceRecs != null)
    {
        for (int i = 0; i < frame.faceRecs.Count; i++)
        {
            var rec = frame.faceRecs[i];
            var color = _colorProvider.GetColor(i);

            var recNodeItem = new TreeNode(
                $"[{i}] Conf={rec.Confidence:F2}, Class={rec.ClassId}, Center=({rec.BoxCenter.X:F2},{rec.BoxCenter.Y:F2},{rec.BoxCenter.Z:F2})"
            )
            {
                ForeColor = color
            };

            faceRecNode.Nodes.Add(recNodeItem);
        }
    }
    recNode.Nodes.Add(faceRecNode);

    // ChArUco Reconstruction
    if (frame.charucoRec != null)
    {
        var chRecNode = new TreeNode($"ChArUcoRec IDs={frame.charucoRec.charucoIds.Count}");

        for (int i = 0; i < frame.charucoRec.charucoIds.Count; i++)
        {
            var id = frame.charucoRec.charucoIds[i];
            var pt = frame.charucoRec.charucoCorners[i];
            var er = frame.charucoRec.cornerReproductionError[i];

            chRecNode.Nodes.Add($"ID {id}: ({pt.X:F2}, {pt.Y:F2}, {pt.Z:F2}, err:{er:F2})");
        }

        recNode.Nodes.Add(chRecNode);
    }

    root.Nodes.Add(recNode);
    recNode.Expand();

    treeView.Nodes.Add(root);
    root.Expand();
}
```

**Parameters:**
- `treeView`: TreeView control to populate
- `frame`: RigFrame containing reconstruction data

### `AutofitViewers()`

Fits all viewers to their content.

```csharp
private void AutofitViewers()
{
    Image1_View.ZoomToFit();
    Image2_View.ZoomToFit();
    XYView.AutoFit();
    YZView.AutoFit();
    XZView.AutoFit();
    PerspectiveView.AutoCenter();
}
```

### `UpdateViewers()`

Updates all viewers with the current frame.

```csharp
private void UpdateViewers()
{
    UpdateViewers(_currentFrame);
}
```

**Parameters:**
- `frame`: Frame to update viewers with (null to clear)

### `ShowPoseReconstruction(List<YoloPoseReconstruction> reconstructions)`

Displays pose reconstruction data in the 3D viewers.

```csharp
private void ShowPoseReconstruction(List<YoloPoseReconstruction> reconstructions)
{
    for (int r = 0; r < reconstructions.Count; r++)
    {
        var rec = reconstructions[r];
        var color = _colorProvider.GetColor(r);

        rec.Keypoints.ForEach(kp =>
        {
            XYView.AddPoint(-kp.X, -kp.Y, color);
            YZView.AddPoint(kp.Z, -kp.Y, color);
            XZView.AddPoint(-kp.X, kp.Z, color);
            PerspectiveView.AddPoint(-kp.X, -kp.Y, kp.Z, color);
        });

        Skeleton.ForEach(bone =>
        {
            int a = bone.Item1;
            int b = bone.Item2;

            if (a < rec.Keypoints.Count && b < rec.Keypoints.Count)
            {
                var kp1 = rec.Keypoints[a];
                var kp2 = rec.Keypoints[b];

                XYView.AddLine(new PointF(-kp1.X, -kp1.Y), new PointF(-kp2.X, -kp2.Y), color);
                YZView.AddLine(new PointF(kp1.Z, -kp1.Y), new PointF(kp2.Z, -kp2.Y), color);
                XZView.AddLine(new PointF(-kp1.X, kp1.Z), new PointF(-kp2.X, kp2.Z), color);
                PerspectiveView.AddLine(new Vector3(-kp1.X, -kp1.Y, kp1.Z), new Vector3(-kp2.X, -kp2.Y, kp2.Z), color);
            }
        });
    }
}
```

**Parameters:**
- `reconstructions`: List of pose reconstructions to display

### `ShowCharucoReconstruction(ChArUcoReconstruction? reconstruction)`

Displays ChArUco reconstruction data in the 3D viewers.

```csharp
private void ShowCharucoReconstruction(ChArUcoReconstruction? reconstruction)
{
    if (reconstruction == null)
        return;

    ChArUcoBoardParameters parameters = Settings.All.GetChArUcoBoardParametersForClusterProfile(Settings.All.GetClusterProfile());

    int cornerColumns = parameters.squaresX - 1;

    for (int i = 0; i < reconstruction.charucoCorners.Count; i++)
    {
        Point3f corner = reconstruction.charucoCorners[i];

        int columnIndex = reconstruction.charucoIds[i] % cornerColumns;

        Color color = _colorProvider.GetColor(columnIndex);

        XYView.AddPoint(-corner.X, -corner.Y, color);
        YZView.AddPoint(corner.Z, -corner.Y, color);
        XZView.AddPoint(-corner.X, corner.Z, color);
        PerspectiveView.AddPoint(-corner.X, -corner.Y, corner.Z, color);
    }
}
```

**Parameters:**
- `reconstruction`: ChArUco reconstruction data to display

## Related Types

- `RigFrame` - Frame container with camera frames and reconstructions
- `MqttWorker` - MQTT communication used internally
- `LivePlayerState` - Live player state enum
- `YoloPoseReconstruction`, `ChArUcoReconstruction` - Reconstruction data types
- `CameraModel`, `PrimitiveOverlayLayer` - 3D viewer components
