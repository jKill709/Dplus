using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using OpenCvSharp;
using OpenCvSharp.Aruco;
using OpenCvSharp.Dnn;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.Design.AxImporter;

using mLogger;
using jColorProviders;

namespace Dplus_Desktop
{
    public partial class Viewer : Form
    {
        private static readonly JsonSerializerOptions Options = GetJsonSerializerOptions();
        public static readonly List<(int, int)> Skeleton = new()
        {
            (0, 1), (0, 2), (1, 3), (2, 4),       // head
            (5, 6), (5, 7), (7, 9), (6, 8), (8, 10), // arms
            (5, 11), (6, 12), (11, 12),           // torso
            (11, 13), (13, 15), (12, 14), (14, 16)   // legs
        };
        private static readonly Regex FilePattern = new Regex(@"^(?<ts>\d+)\((?<id>\d+)\)\.json$", RegexOptions.Compiled);

        private int colorCount = 6;
        private bool _isSyncing = false; // Flag to prevent recursive updates when changing selections programmatically

        private MqttWorker _worker;

        private List<RigFrame> _savedFrames = new();
        private List<RigFrame> _liveFrames = new();
        private LivePlayerState _livePlayerState = LivePlayerState.Disconected;
        private RigFrame? _currentFrame = null;
        private PrimitiveOverlayLayer _image1Overlay;
        private PrimitiveOverlayLayer _image2Overlay;
        private FixedIndexColorProvider _colorProvider = new FixedIndexColorProvider();

        private Logger logger = Logger.Instance;

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
        private async void Viewer_Load(object sender, EventArgs e)
        {
            try
            {
                logger.Log(mLogger.LogLevel.INFO, "Viewer", "Connecting to MQTT...");
                AddLogSource("MQTT Worker", Color.Green, true);

                Device currentHub = Settings.All.GetDeviceByName("Hub1");

                _worker = new MqttWorker(currentHub.IPAddress, 1883, currentHub.ClusterID + "/hubTelemetry");

                _worker.OnMessage += async e =>
                {
                    string json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                    HandleIncomingFrame(json);
                    await Task.CompletedTask;
                };

                await _worker.StartAsync();
            }
            catch (Exception ex)
            {
                LiveViewStatus_Label.Text = "Failed";
                logger.Log(mLogger.LogLevel.ERROR, "Viewer", $"MQTT connection failed: {ex.ToString()}");
                logger.Log(mLogger.LogLevel.ERROR, "Viewer", $"MQTT connection failed: {ex.Message}");
            }
        }
        private void AddLogSource(string source, Color color = default, bool andModules = true)
        {
            logger.AddSource(source, color, andModules);
            logger.Log(mLogger.LogLevel.INFO, "Viewer", $"Added source '{source}' to _tbSink");
        }
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

                logger.Log(mLogger.LogLevel.INFO, "Viewer", $"Received frame: CmdID={frame.commandID}, Time={frame.Timestamp:HH:mm:ss.fff}, Cams={frame.camFrames.Count}, PoseRecs={frame.poseRecs?.Count ?? 0}, ObjectRecs={frame.objectRecs?.Count ?? 0}, FaceRecs={frame.faceRecs?.Count ?? 0}, ChArUcoRec={(frame.charucoRec != null ? "Yes" : "No")}");

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
                    //OutputText($"Loading Saved RigFrame: {file}", mLogger.LogLevel.INFO);
                    try
                    {
                        string json = File.ReadAllText(file);
                        string fileName = Path.GetFileNameWithoutExtension(file);
                        //OutputText($"Raw File Contents: {json}", mLogger.LogLevel.INFO);
                        try
                        {
                            var rigFrame = JsonSerializer.Deserialize<RigFrame>(json, Options);
                            //OutputText($"Deserialization successful.", mLogger.LogLevel.INFO);

                            if (rigFrame != null)
                            {
                                //logger.Log(mLogger.LogLevel.INFO, "Viewer", $"Deserialized CommandID: {rigFrame?.commandID}: {rigFrame?.camFrames.Count} frames, {rigFrame?.charucoRec?.charucoIds.Count()} charuco IDs");
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

                // Sort by CommandID, then by file name (alphabetical)
                //framePairs = framePairs.OrderBy(p => p.Frame.commandID).ToList();
                framePairs = framePairs.OrderByDescending(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

                // Populate collections in same order
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
        public static RigFrame LoadFromFile(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RigFrame>(json, Options)
                   ?? new RigFrame();
        }
        public static RigFrame LoadFromJson(string json)
        {
            return JsonSerializer.Deserialize<RigFrame>(json, Options)
                   ?? new RigFrame();
        }

        public static void GroupRuns(List<string> fileNames, out List<List<string>> runs, out List<string> runSummaries)
        {
            // Parse
            var parsed = fileNames
                .Select(f =>
                {
                    var match = FilePattern.Match(f);
                    if (!match.Success)
                        throw new ArgumentException($"Invalid filename format: {f}");

                    long ts = long.Parse(match.Groups["ts"].Value);
                    int id = int.Parse(match.Groups["id"].Value);

                    return new ParsedFile
                    {
                        FileName = f,
                        Timestamp = ts,
                        CommandId = id
                    };
                })
                .OrderBy(p => p.Timestamp)
                .ToList();

            runs = new List<List<string>>();
            runSummaries = new List<string>();

            if (parsed.Count == 0)
                return;

            List<ParsedFile> currentRun = new List<ParsedFile>();
            currentRun.Add(parsed[0]);

            for (int i = 1; i < parsed.Count; i++)
            {
                var prev = parsed[i - 1];
                var curr = parsed[i];

                // Detect new run when commandId resets (drops)
                if (curr.CommandId <= prev.CommandId)
                {
                    FinalizeRun(currentRun, runs, runSummaries);
                    currentRun = new List<ParsedFile>();
                }

                currentRun.Add(curr);
            }

            // Final run
            FinalizeRun(currentRun, runs, runSummaries);
        }
        private static void FinalizeRun(List<ParsedFile> run, List<List<string>> runs, List<string> summaries)
        {
            runs.Add(run.Select(r => r.FileName).ToList());

            long startTs = run.First().Timestamp;
            long endTs = run.Last().Timestamp;

            DateTime startTime = DateTimeOffset
                .FromUnixTimeMilliseconds(startTs)
                .LocalDateTime;

            TimeSpan duration = TimeSpan.FromMilliseconds(endTs - startTs);

            summaries.Add($"{startTime:yyyy-MM-dd HH:mm:ss} | Duration: {duration}");
        }
        private class ParsedFile
        {
            public string FileName { get; set; }
            public long Timestamp { get; set; }
            public int CommandId { get; set; }
        }

        private static int GetPoseDetectionCount(RigFrame frame)
        {
            return frame.camFrames.Sum(cf => cf.poseDets?.Count ?? 0);
        }
        private static int GetObjectDetectionCount(RigFrame frame)
        {
            return frame.camFrames.Sum(cf => cf.objectDets?.Count ?? 0);
        }
        private static int GetFaceDetectionCount(RigFrame frame)
        {
            return frame.camFrames.Sum(cf => cf.faceDets?.Count ?? 0);
        }
        private static int GetCharucoDetectionCount(RigFrame frame)
        {
            return frame.camFrames.Count(cf => cf.charucoDet != null && cf.charucoDet.Valid);
        }
        private void PopulateImageType_Box()
        {
            string oldText = "";
            bool reuseText = false;
            if (_currentFrame == null)
                return;

            if (ImageType_Box.Text != null)
                oldText = ImageType_Box.Text;
            if (oldText == "(all)" || oldText == "None")
                reuseText = true;

            ImageType_Box.Items.Clear();
            ImageType_Box.Items.Add("(all)");
            if ((_currentFrame.poseRecs != null && _currentFrame.poseRecs.Count > 0) || (GetPoseDetectionCount(_currentFrame) > 0))
            {
                if (oldText == "YoloPose")
                    reuseText = true;

                ImageType_Box.Items.Add("YoloPose");
            }
            if ((_currentFrame.objectRecs != null && _currentFrame.objectRecs.Count > 0) || (GetObjectDetectionCount(_currentFrame) > 0))
            {
                if (oldText == "YoloObject")
                    reuseText = true;

                ImageType_Box.Items.Add("YoloObject");
            }
            if ((_currentFrame.faceRecs != null && _currentFrame.faceRecs.Count > 0) || (GetFaceDetectionCount(_currentFrame) > 0))
            {
                if (oldText == "FaceRec")
                    reuseText = true;

                ImageType_Box.Items.Add("FaceRec");
            }
            if ((_currentFrame.charucoRec != null && _currentFrame.charucoRec.charucoIds.Count > 0) || (GetCharucoDetectionCount(_currentFrame) > 0))
            {
                if (oldText == "ChArUco")
                    reuseText = true;

                ImageType_Box.Items.Add("ChArUco");
            }
            ImageType_Box.Items.Add("None");

            if (reuseText && ImageType_Box.Items.Contains(oldText))
            {
                ImageType_Box.SelectedItem = oldText;
            }
            else
            {
                ImageType_Box.SelectedIndex = 0; // Default to "(all)"
            }
        }
        private void SavedFrames_Box_SelectedIndexChanged(object sender, EventArgs e)
        {
            logger.Log(mLogger.LogLevel.INFO, "Viewer", $"Setting Current Frame to saved frame {SavedFrames_Box.SelectedItem}");
            _currentFrame = _savedFrames[SavedFrames_Box.SelectedIndex];

            PopulateImageType_Box();

            SavedFrameIndex_Label.Text = (SavedFrames_Box.SelectedIndex + 1) + " / " + SavedFrames_Box.Items.Count;

            if (_isSyncing) return;

            try
            {
                _isSyncing = true;

                SavedFramesBoxIndex_UpDown.Value = SavedFrames_Box.SelectedIndex;
            }
            finally
            {
                _isSyncing = false;
            }
        }
        private void SavedFramesBoxIndex_UpDown_ValueChanged(object sender, EventArgs e)
        {
            if (_isSyncing) return;

            try
            {
                _isSyncing = true;

                int index = (int)SavedFramesBoxIndex_UpDown.Value;

                if (index >= 0 && index < SavedFrames_Box.Items.Count)
                {
                    SavedFrames_Box.SelectedIndex = index;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }
        private string getImagePath(string NodeName, ImageType Type, DateTime Timestamp)
        {
            string basePath = Settings.All.LocalLogPath + NodeName + "\\";
            string subDir = "";
            string fileName = GetFilenameFromTimestamp(Timestamp);

            switch (Type)
            {
                case ImageType.Capture:
                    subDir = "Captures\\";
                    break;
                case ImageType.Yolo11Pose:
                    subDir = "YoloPose\\";
                    break;
                case ImageType.Yolo11Object:
                    subDir = "YoloObject\\";
                    break;
                case ImageType.FaceRec:
                    subDir = "Face\\";
                    break;
                case ImageType.ChArUco:
                    subDir = "Startup\\";
                    break;
            }

            string primaryPath = basePath + subDir + fileName;

            // Check original location
            if (useDetectorImages_Box.Checked && File.Exists(primaryPath))
                return primaryPath;

            // Check Captures directory
            string captureDir = basePath + "Captures\\";
            if (Directory.Exists(captureDir))
            {
                string capturePath = captureDir + fileName;
                if (File.Exists(capturePath))
                    return capturePath;
            }

            // Nothing found
            return "";
        }
        private string GetFilenameFromTimestamp(DateTime timestamp)
        {
            // Match C++ localtime behavior
            DateTime localTime = timestamp.ToLocalTime();

            string formatted = localTime.ToString("MMMddyy_HHmmss",
                System.Globalization.CultureInfo.InvariantCulture);

            return $"Frame_{formatted}.png";
        }
        public void DisplayRigFrameData(System.Windows.Forms.TreeView treeView, RigFrame frame)
        {
            treeView.Nodes.Clear();

            var root = new TreeNode($"RigFrame | Cmd: {frame.commandID} | Time: {frame.Timestamp:HH:mm:ss.fff}");

            // --- Camera Frames ---
            var camsNode = new TreeNode($"CameraFrames ({frame.camFrames.Count})");

            foreach (var cam in frame.camFrames)
            {
                var camNode = new TreeNode($"{cam.sourceName}");

                // --- Pose Detections ---
                var poseNode = new TreeNode($"PoseDets ({cam.poseDets?.Count ?? 0})");
                if (cam.poseDets != null)
                {
                    for (int i = 0; i < cam.poseDets.Count; i++)
                    {
                        var det = cam.poseDets[i];
                        var color = _colorProvider.GetColor(i);

                        // --- Detection Summary ---
                        var detNode = new TreeNode(
                            $"[{i}] Conf={det.Confidence:F2}, Class={det.ClassId}, Box=({det.Box.X:F1},{det.Box.Y:F1},{det.Box.Width:F1},{det.Box.Height:F1})"
                        )
                        {
                            ForeColor = color
                        };

                        // Keypoints summary
                        if (det.Keypoints != null && det.Keypoints.Count > 0)
                        {
                            //var kpNode = new TreeNode($"Keypoints ({det.Keypoints.Count})");
                            for (int k = 0; k < det.Keypoints.Count; k++)
                            {
                                var kp = det.Keypoints[k];
                                detNode.Nodes.Add($"[{k}] ({kp.X:F3}, {kp.Y:F3})  conf={det.Kp_Confidences[k]:F2}");
                            }
                            //detNode.Nodes.Add(kpNode);
                        }

                        poseNode.Nodes.Add(detNode);
                    }
                }
                camNode.Nodes.Add(poseNode);
                poseNode.Expand();

                // --- Object Detections ---
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

                // --- Face Detections ---
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

                // --- ChArUco Detection ---
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

            // --- Reconstructions ---
            var recNode = new TreeNode("Reconstructions");

            // Pose Reconstructions
            var poseRecNode = new TreeNode($"PoseRecs ({frame.poseRecs?.Count ?? 0})");
            if (frame.poseRecs != null)
            {
                for (int i = 0; i < frame.poseRecs.Count; i++)
                {
                    var rec = frame.poseRecs[i];
                    var color = _colorProvider.GetColor(i);

                    // --- Detection Summary ---
                    var recNodeItem = new TreeNode(
                        $"[{i}] Conf={rec.Confidence:F2}, Class={rec.ClassId}, Center=({rec.BoxCenter.X:F2},{rec.BoxCenter.Y:F2},{rec.BoxCenter.Z:F2})"
                    )
                    {
                        ForeColor = color
                    };

                    // Keypoints summary
                    if (rec.Keypoints != null && rec.Keypoints.Count > 0)
                    {
                        //var kpNode = new TreeNode($"Keypoints ({det.Keypoints.Count})");
                        for (int k = 0; k < rec.Keypoints.Count; k++)
                        {
                            var kp = rec.Keypoints[k];
                            recNodeItem.Nodes.Add($"[{k}] ({kp.X:F3}, {kp.Y:F3}, {kp.Z:F3})  conf={rec.Kp_Confidences[k]:F2}");     //$"{k}] ({kp.X:F3}, {kp.Y:F3})  conf={rec.Kp_Confidences[k]:F2}");
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
            //treeView.ExpandAll();
        }
        private void AutofitViewers()
        {
            Image1_View.ZoomToFit();
            Image2_View.ZoomToFit();
            XYView.AutoFit();
            YZView.AutoFit();
            XZView.AutoFit();
            PerspectiveView.AutoCenter();
        }
        private void UpdateViewers(object sender, EventArgs e)
        {
            UpdateViewers();
        }
        private void UpdateViewers()
        {
            UpdateViewers(_currentFrame);
        }
        private void UpdateViewers(RigFrame frame)
        {
            //Clear Previous overlays and points
            _image1Overlay.ClearLayers();
            _image2Overlay.ClearLayers();
            XYView.Clear();
            YZView.Clear();
            XZView.Clear();
            PerspectiveView.Clear();

            //OutputText($"Selected frame: {Frames_Box.SelectedItem}", LogLevel.INFO);

            if (frame == null || ImageType_Box == null)
                return;

            DisplayRigFrameData(CurrentFrame_View, frame);

            // Determine which image type to load based on user selection
            ImageType type = ImageType.Capture;
            DateTime timestamp = frame.Timestamp;
            if (ImageType_Box.SelectedItem != null)
            {
                switch (ImageType_Box.SelectedItem.ToString())
                {
                    case "(all)":
                        type = ImageType.Capture;
                        break;
                    case "None":
                        type = ImageType.Capture;
                        break;
                    case "YoloPose":
                        type = ImageType.Yolo11Pose;
                        break;
                    case "YoloObject":
                        type = ImageType.Yolo11Object;
                        break;
                    case "FaceRec":
                        type = ImageType.FaceRec;
                        break;
                    case "ChArUco":
                        type = ImageType.ChArUco;
                        break;
                    case "Chessboard":
                        type = ImageType.Chessboard;
                        break;
                    default:
                        return;
                }
            }

            frame.camFrames.ForEach(cframe => ShowCamerasView(cframe, type, timestamp));

            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "ChArUco") && (frame.Timestamp != DateTime.UnixEpoch))
            {
                ShowCharucoReconstruction(frame.charucoRec);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "Chessboard") && (frame.Timestamp != DateTime.UnixEpoch))
            {
                ShowChessboardReconstruction(frame.chessboardRec);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "YoloPose") && (frame.poseRecs != null))
            {
                ShowPoseReconstruction(frame.poseRecs);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "YoloObject") && (frame.poseRecs != null))
            {
                ShowObjectReconstruction(frame.objectRecs);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "FaceRec") && (frame.poseRecs != null))
            {
                ShowFaceReconstruction(frame.faceRecs);
            }
            Image1_View.Invalidate();
            Image2_View.Invalidate();
        }
        private void ShowCamerasView(CameraFrame frame, ImageType type, DateTime timestamp)
        {
            CameraModel cameraModel;
            ShowCameraFrame(frame, type, timestamp);

            Intrinsics intrinsics;
            Extrinsics extrinsics;
            intrinsics = Settings.All.GetIntrinsicsForNode(frame.sourceName);

            if (frame.sourceName == "Node1")
                extrinsics = Extrinsics.Identity(frame.sourceName);
            else
                extrinsics = Settings.All.GetExtrinsicsForNode("Node1", frame.sourceName);

            cameraModel = new CameraModel(intrinsics, extrinsics);
            PerspectiveView.AddCamera(cameraModel);
        }
        private void ShowCameraFrame(CameraFrame frame, ImageType type, DateTime timestamp)
        {
            ImageControls ImageControl;
            PrimitiveOverlayLayer CurrentOverlay;

            //float xTextOffset = (float)(5 / Settings.All.GetIntrinsicsForNode(frame.sourceName).K[0][0]); // 5 pixel offset converted to normalized coordinates
            //float yTextOffset = (float)(5 / Settings.All.GetIntrinsicsForNode(frame.sourceName).K[1][1]); // 5 pixel offset converted to normalized coordinates
            System.Drawing.Font font = new System.Drawing.Font("Arial", 12);

            Intrinsics intrinsics;
            Extrinsics extrinsics;
            intrinsics = Settings.All.GetIntrinsicsForNode(frame.sourceName);

            if (frame.sourceName == "Node1")
            {
                CurrentOverlay = _image1Overlay;
                ImageControl = Image1_View;
                extrinsics = Extrinsics.Identity(frame.sourceName);
            }
            else if (frame.sourceName == "Node2")
            {
                CurrentOverlay = _image2Overlay;
                ImageControl = Image2_View;
                extrinsics = Settings.All.GetExtrinsicsForNode("Node1", frame.sourceName);
            }
            else if (frame.sourceName == "Node3")
            {
                CurrentOverlay = _image2Overlay;
                ImageControl = Image2_View;
                extrinsics = Settings.All.GetExtrinsicsForNode("Node1", frame.sourceName);
            }
            else if (frame.sourceName == "Node4")
            {
                CurrentOverlay = _image2Overlay;
                ImageControl = Image2_View;
                extrinsics = Settings.All.GetExtrinsicsForNode("Node1", frame.sourceName);
            }
            else
            {
                MessageBox.Show($"Unknown camera source: {frame.sourceName}");
                return;
            }

            try
            {
                string path = getImagePath(frame.sourceName, type, timestamp);

                if (string.IsNullOrWhiteSpace(path))
                    path = Settings.All.NoImagePath;

                if (!File.Exists(path))
                    throw new FileNotFoundException("Image file not found.", path);

                // Dispose previous image to avoid memory leaks / file locks
                var oldImage = ImageControl.DisplayedImage;
                ImageControl.DisplayedImage = System.Drawing.Image.FromFile(path);
                oldImage?.Dispose();
            }
            catch (FileNotFoundException ex)
            { MessageBox.Show($"Image file not found:\n{ex.FileName}", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (ArgumentException ex)
            { MessageBox.Show($"Invalid image path:\n{ex.Message}", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            catch (OutOfMemoryException ex)
            { MessageBox.Show($"The file is not a valid image or is corrupted.\n\n{ex.Message}", "Image Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            catch (Exception ex)
            { MessageBox.Show($"Unexpected error loading image:\n\n{ex.Message}\n\n{ex.StackTrace}", "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }

            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "ChArUco") && (frame.charucoDet.Timestamp != DateTime.UnixEpoch))// && frame.charucoDet.Valid)
            {
                ShowCharucoDetection(frame.charucoDet, CurrentOverlay, intrinsics, font);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "Chessboard") && (frame.chessboardDet != null) && (frame.chessboardDet.Timestamp != DateTime.UnixEpoch))
            {
                ShowChesboardDetection(frame.chessboardDet, CurrentOverlay, intrinsics, font);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "YoloPose") && (frame.poseDets != null))
            {
                ShowPoseDetections(frame.poseDets, CurrentOverlay, intrinsics, font);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "YoloObject") && (frame.objectDets != null))
            {
                ShowObjectDetections(frame.objectDets, CurrentOverlay, intrinsics, font);
            }
            if ((ImageType_Box.SelectedItem.ToString() == "(all)" || ImageType_Box.SelectedItem.ToString() == "FaceRec") && (frame.faceDets != null))
            {
                ShowFaceDetections(frame.faceDets, CurrentOverlay, intrinsics, font);
            }
        }
        private void ShowCharucoDetection(ChArUcoDetection detection, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font)
        {
            float xTextOffset = (float)(5 / intrinsics.K[0][0]); // 5 pixel offset converted to normalized coordinates
            float yTextOffset = (float)(5 / intrinsics.K[1][1]); // 5 pixel offset converted to normalized coordinates
            for (int i = 0; i < detection.CharucoCorners.Count; i++)
            {
                var color = _colorProvider.GetColor(i);
                Brush brush = new SolidBrush(color);

                var kp = detection.CharucoCorners[i];
                var id = detection.CharucoIds[i];

                CurrentOverlay.Points.Add(new PointOverlay
                {
                    Location = intrinsics.Project(new Point2f(kp.X, kp.Y)),
                    Radius = 5,
                    Brush = brush
                });

                if (ShowIDs_Box.Checked)
                {
                    CurrentOverlay.Texts.Add(new TextOverlay
                    {
                        Location = intrinsics.Project(new Point2f(kp.X + xTextOffset, kp.Y - yTextOffset)),
                        Text = id.ToString(),
                        Brush = brush,
                        Font = font
                    });
                }
            }
        }
        private void ShowChesboardDetection(ChessboardDetection detection, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font)
        {
            float xTextOffset = (float)(5 / intrinsics.K[0][0]); // 5 pixel offset converted to normalized coordinates
            float yTextOffset = (float)(5 / intrinsics.K[1][1]); // 5 pixel offset converted to normalized coordinates

            for (int i = 0; i < detection.ChessboardCorners.Count; i++)
            {
                var color = _colorProvider.GetColor(i);
                Brush brush = new SolidBrush(color);

                var kp = detection.ChessboardCorners[i];
                var id = detection.ChessboardIds[i];

                CurrentOverlay.Points.Add(new PointOverlay
                {
                    Location = intrinsics.Project(new Point2f(kp.X * 10000, kp.Y * 10000)),
                    Radius = 5,
                    Brush = brush
                });

                if (ShowIDs_Box.Checked)
                {
                    CurrentOverlay.Texts.Add(new TextOverlay
                    {
                        Location = intrinsics.Project(new Point2f(kp.X + xTextOffset, kp.Y - yTextOffset)),
                        Text = id.ToString(),
                        Brush = brush,
                        Font = font
                    });
                }
            }
        }
        private void ShowPoseDetection(YoloPoseDetection detection, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font, Color color)
        {
            float xTextOffset = (float)(5 / intrinsics.K[0][0]); // 5 pixel offset converted to normalized coordinates
            float yTextOffset = (float)(5 / intrinsics.K[1][1]); // 5 pixel offset converted to normalized coordinates

            if (detection == null || detection.Confidence < Objectness_Bar.Value / 100.0)
                return; // Skip detections below confidence threshold


            Brush brush = new SolidBrush(color);
            Pen pen = new Pen(color);

            // --- Keypoints ---

            for (int i = 0; i < detection.Keypoints.Count; i++)
            {
                if (detection.Kp_Confidences[i] < KP_Bar.Value / 100.0)
                    continue;

                var kp = detection.Keypoints[i];
                var kpConf = detection.Kp_Confidences[i];
                var kpId = i.ToString();
                CurrentOverlay.Points.Add(new PointOverlay
                {
                    Location = intrinsics.Project(new Point2f(kp.X, kp.Y)), //new PointF(kp.X, kp.Y),
                    Radius = 5,
                    Brush = brush
                });

                if (ShowIDs_Box.Checked)
                {
                    CurrentOverlay.Texts.Add(new TextOverlay
                    {
                        Location = intrinsics.Project(new Point2f(kp.X + xTextOffset, kp.Y - yTextOffset)),
                        Text = kpId,
                        Brush = brush,
                        Font = font
                    });
                }
            }

            // Draw skeleton connections
            Skeleton.ForEach(bone =>
            {
                int a = bone.Item1;
                int b = bone.Item2;

                if (a < detection.Keypoints.Count && b < detection.Keypoints.Count)
                {
                    var kp1 = detection.Keypoints[a];
                    var kp2 = detection.Keypoints[b];

                    CurrentOverlay.Lines.Add(new LineOverlay
                    {
                        P1 = intrinsics.Project(new Point2f(kp1.X, kp1.Y)),
                        P2 = intrinsics.Project(new Point2f(kp2.X, kp2.Y)),
                        Pen = pen
                    });
                }
            });
        }
        private void ShowPoseDetections(List<YoloPoseDetection> detections, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font)
        {
            int i = 0;
            Color currentColor;
            foreach (var detection in detections)
            {
                currentColor = _colorProvider.GetColor(i);
                i++;
                ShowPoseDetection(detection, CurrentOverlay, intrinsics, font, currentColor);
            }
        }
        private void ShowObjectDetection(YoloObjectDetection detection, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font, Color color)
        {
            float xTextOffset = (float)(5 / intrinsics.K[0][0]); // 5 pixel offset converted to normalized coordinates
            float yTextOffset = (float)(5 / intrinsics.K[1][1]); // 5 pixel offset converted to normalized coordinates

            //OutputText($"Adding box for YoloObject det at {det.Timestamp}: {det.Box}", LogLevel.DEBUG);
            CurrentOverlay.Polygons.Add(new PolygonOverlay
            {
                Points = new List<PointF>
                    {
                            intrinsics.Project(new Point2f(detection.Box.X, detection.Box.Y)),
                            intrinsics.Project(new Point2f(detection.Box.X + detection.Box.Width, detection.Box.Y)),
                            intrinsics.Project(new Point2f(detection.Box.X + detection.Box.Width, detection.Box.Y + detection.Box.Height)),
                            intrinsics.Project(new Point2f(detection.Box.X, detection.Box.Y + detection.Box.Height))
                    },
                //Rect = new RectangleF(det.Box.X, det.Box.Y, det.Box.Width, det.Box.Height),
                Pen = Pens.Red
            });
            if (ShowIDs_Box.Checked)
            {
                CurrentOverlay.Texts.Add(new TextOverlay
                {
                    Location = intrinsics.Project(new Point2f(detection.Box.X + xTextOffset, detection.Box.Y - yTextOffset)),
                    Text = detection.ClassId.ToString(),
                    Brush = new SolidBrush(color),
                    Font = font
                });
            }
        }
        private void ShowObjectDetections(List<YoloObjectDetection> detections, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font)
        {
            int i = 0;
            Color currentColor;
            foreach (var detection in detections)
            {
                currentColor = _colorProvider.GetColor(i);
                i++;
                ShowObjectDetection(detection, CurrentOverlay, intrinsics, font, currentColor);
            }
        }
        private void ShowFaceDetection(FaceDetection detection, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font, Color color)
        {
            float xTextOffset = (float)(5 / intrinsics.K[0][0]); // 5 pixel offset converted to normalized coordinates
            float yTextOffset = (float)(5 / intrinsics.K[1][1]); // 5 pixel offset converted to normalized coordinates

            //OutputText($"Adding box for YoloObject det at {det.Timestamp}: {det.Box}", LogLevel.DEBUG);
            CurrentOverlay.Polygons.Add(new PolygonOverlay
            {
                Points = new List<PointF>
                            {
                                    intrinsics.Project(new Point2f(detection.Box.X, detection.Box.Y)),
                                    intrinsics.Project(new Point2f(detection.Box.X + detection.Box.Width, detection.Box.Y)),
                                    intrinsics.Project(new Point2f(detection.Box.X + detection.Box.Width, detection.Box.Y + detection.Box.Height)),
                                    intrinsics.Project(new Point2f(detection.Box.X, detection.Box.Y + detection.Box.Height))
                            },
                //Rect = new RectangleF(det.Box.X, det.Box.Y, det.Box.Width, det.Box.Height),
                Pen = new Pen(color)
            });
            if (ShowIDs_Box.Checked)
            {
                CurrentOverlay.Texts.Add(new TextOverlay
                {
                    Location = intrinsics.Project(new Point2f(detection.Box.X + xTextOffset, detection.Box.Y - yTextOffset)),
                    Text = detection.ClassId.ToString(),
                    Brush = new SolidBrush(color),
                    Font = font
                });
            }
        }
        private void ShowFaceDetections(List<FaceDetection> detections, PrimitiveOverlayLayer CurrentOverlay, Intrinsics intrinsics, System.Drawing.Font font)
        {
            int i = 0;
            Color currentColor;
            foreach (var detection in detections)
            {
                currentColor = _colorProvider.GetColor(i);
                i++;
                ShowFaceDetection(detection, CurrentOverlay, intrinsics, font, currentColor);
            }
        }
        private void ShowCharucoReconstruction(ChArUcoReconstruction? reconstruction)
        {
            if (reconstruction == null)
                return;

            ChArUcoBoardParameters parameters = Settings.All.GetChArUcoBoardParametersForClusterProfile(Settings.All.GetClusterProfile());

            // ChArUco corner grid dimensions are one less than square dimensions
            int cornerColumns = parameters.squaresX - 1;

            for (int i = 0; i < reconstruction.charucoCorners.Count; i++)
            {
                Point3f corner = reconstruction.charucoCorners[i];

                // Determine column based on ChArUco ID layout
                int columnIndex = reconstruction.charucoIds[i] % cornerColumns;

                Color color = _colorProvider.GetColor(columnIndex);

                XYView.AddPoint(-corner.X, -corner.Y, color);
                YZView.AddPoint(corner.Z, -corner.Y, color);
                XZView.AddPoint(-corner.X, corner.Z, color);

                PerspectiveView.AddPoint(-corner.X, -corner.Y, corner.Z, color);
            }
        }
        private void ShowChessboardReconstruction(ChessboardReconstruction? reconstruction)
        {

        }
        private void ShowPoseReconstruction(List<YoloPoseReconstruction> reconstructions)
        {
            for (int r = 0; r < reconstructions.Count; r++)
            {
                var rec = reconstructions[r];
                var color = _colorProvider.GetColor(r);

                rec.Keypoints.ForEach(kp =>
                {
                    //MessageBox.Show($"Adding pose keypoint to viewers: {kp}");
                    XYView.AddPoint(-kp.X, -kp.Y, color);
                    YZView.AddPoint(kp.Z, -kp.Y, color);
                    XZView.AddPoint(-kp.X, kp.Z, color);

                    PerspectiveView.AddPoint(-kp.X, -kp.Y, kp.Z, color);
                });

                // Draw skeleton connections
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
        private void ShowObjectReconstruction(List<YoloObjectReconstruction> reconstruction)
        {

        }
        private void ShowFaceReconstruction(List<FaceReconstruction> reconstruction)
        {

        }
        public static Point2f NormalizedToPixel(Point2f norm, float fx, float fy, float cx, float cy)
        {
            float px = fx * norm.X + cx;
            float py = fy * norm.Y + cy;

            return new Point2f(px, py);
        }
        private void XYViewer_Click(object sender, EventArgs e)
        {

        }
        private void Objectness_Bar_ValueChanged(object sender, EventArgs e)
        {
            ObjectnessValue_Label.Text = (Objectness_Bar.Value / 100.0).ToString();
            UpdateViewers();
        }
        private void KP_Bar_ValueChanged(object sender, EventArgs e)
        {
            KpValue_Label.Text = (KP_Bar.Value / 100.0).ToString();
            UpdateViewers();
        }
        private void ShowDebug_Box_CheckedChanged(object sender, EventArgs e)
        {
            XYView.ShowDebug = ShowDebug_Box.Checked;
            YZView.ShowDebug = ShowDebug_Box.Checked;
            XZView.ShowDebug = ShowDebug_Box.Checked;

            PerspectiveView.ShowDebug = ShowDebug_Box.Checked;

            XYView.Invalidate();
            YZView.Invalidate();
            XZView.Invalidate();

            PerspectiveView.Invalidate();
        }
        private void EnableLiveFeed(bool enable)
        {
            // If Enabling Live Feed, Then
            //  Enable Live Controls
            LiveFrames_Bar.Enabled = enable;
            //LiveFramesIndex_Label.andModules = enable;
            LiveRewind_Button.Enabled = enable;
            LivePrevious_Button.Enabled = enable;
            LivePause_Button.Enabled = enable;
            LiveNext_Button.Enabled = enable;
            LiveCatchup_Button.Enabled = enable;
            if (enable)
            {
                LiveRewind_Button.BackgroundImage = Properties.Resources.RewindButtonGreen;
                LivePrevious_Button.BackgroundImage = Properties.Resources.PreviousButtonGreen;
                LivePause_Button.BackgroundImage = Properties.Resources.PauseButtonRed;
                LiveNext_Button.BackgroundImage = Properties.Resources.NextButtonGreen;
                LiveCatchup_Button.BackgroundImage = Properties.Resources.PlayButtonGreen;
            }
            else
            {
                LiveRewind_Button.BackgroundImage = Properties.Resources.RewindButtonGray;
                LivePrevious_Button.BackgroundImage = Properties.Resources.PreviousButtonGray;
                LivePause_Button.BackgroundImage = Properties.Resources.PauseButtonGray;
                LiveNext_Button.BackgroundImage = Properties.Resources.NextButtonGray;
                LiveCatchup_Button.BackgroundImage = Properties.Resources.PlayButtonGray;
            }

            //  Diable Saved File Controls
            SavedFrames_Box.Enabled = !enable;
            SavedFramesBoxIndex_UpDown.Enabled = !enable;
            SavedFrameIndex_Label.Enabled = !enable;
        }
        private void LiveView_rButton_CheckedChanged(object sender, EventArgs e)
        {
            logger.Log(mLogger.LogLevel.INFO, "Viewer", "Live feed enabled. Displaying most recent frame and updating in real-time.");
            EnableLiveFeed(true);
        }
        private void SavedFrames_rButton_CheckedChanged(object sender, EventArgs e)
        {
            logger.Log(mLogger.LogLevel.INFO, "Viewer", "Displaying saved frames.");
            EnableLiveFeed(false);
        }
        private void Viewer_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.Log(mLogger.LogLevel.INFO, "Viewer", "______________________________");
            logger.Log(mLogger.LogLevel.INFO, "Viewer", "________Viewer Exiting________");
            logger.Log(mLogger.LogLevel.INFO, "Viewer", "______________________________");
        }
        private void LiveCatchup_Button_Click(object sender, EventArgs e)
        {
            _livePlayerState = LivePlayerState.Play;
            LiveViewStatus_Label.Text = "Play";

            _isSyncing = true;
            LiveFrames_Bar.Value = _liveFrames.Count;
            _isSyncing = false;
        }
        private void LiveNext_Button_Click(object sender, EventArgs e)
        {
            if (_livePlayerState == LivePlayerState.Play)
                return;

            LiveFrames_Bar.Value = Math.Min(LiveFrames_Bar.Value + 1, LiveFrames_Bar.Maximum);
        }
        private void LivePause_Button_Click(object sender, EventArgs e)
        {
            if ((_livePlayerState == LivePlayerState.Disconected) || (_livePlayerState == LivePlayerState.Pause))
                return;

            _livePlayerState = LivePlayerState.Pause;
            LiveViewStatus_Label.Text = "Pause";
        }
        private void LivePrevious_Button_Click(object sender, EventArgs e)
        {
            if (_livePlayerState == LivePlayerState.Play)
            {
                _livePlayerState = LivePlayerState.Pause;
                LiveViewStatus_Label.Text = "Pause";
            }

            LiveFrames_Bar.Value = Math.Max(LiveFrames_Bar.Value - 1, LiveFrames_Bar.Minimum);
        }
        private void LiveRewind_Button_Click(object sender, EventArgs e)
        {
            LivePause_Button_Click(sender, e);
        }
        
        private enum LivePlayerState
        {
            Play,
            Pause,
            Rewind,
            Disconected
        }
        private static JsonSerializerOptions GetJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            options.Converters.Add(new Point2fConverter());
            options.Converters.Add(new Point3fConverter());
            options.Converters.Add(new UnixMillisecondsDateTimeConverter());

            return options;
        }
        private void LiveFrames_Bar_ValueChanged(object sender, EventArgs e)
        {
            if (LiveView_rButton.Checked)
            {
                if (!_isSyncing)
                {
                    if (_livePlayerState == LivePlayerState.Play)
                    {
                        _livePlayerState = LivePlayerState.Pause;
                        LiveViewStatus_Label.Text = "Pause";
                    }
                }

                _currentFrame = _liveFrames.ElementAtOrDefault(LiveFrames_Bar.Value - 1);
                LiveViewInfo_Label.Text = $"{LiveFrames_Bar.Value}/{LiveFrames_Bar.Maximum}";
                LiveViewTimestamp_Label.Text = $"{_currentFrame?.Timestamp:HH:mm:ss.fff}";
                //LiveFramesInfo_Label.Text = $"{LiveFrames_Bar.Value}/{_liveFrames.Count}";Disconected
                UpdateViewers();
            }
        }
        private void Autofit_Button_Click(object sender, EventArgs e)
        {
            AutofitViewers();
        }
    }
    public enum ImageType
    {
        Capture,
        Yolo11Pose,
        Yolo11Object,
        FaceRec,
        ChArUco,
        Chessboard
    }

    public class RigFrame
    {
        public int commandID { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.MinValue;

        public List<CameraFrame> camFrames { get; set; } = new();

        public List<YoloPoseReconstruction>? poseRecs { get; set; } = new();
        public List<YoloObjectReconstruction>? objectRecs { get; set; } = new();
        public List<FaceReconstruction>? faceRecs { get; set; } = new();
        public ChArUcoReconstruction? charucoRec { get; set; } = new();
        public ChessboardReconstruction? chessboardRec { get; set; } = new();
    }
    public class CameraFrame
    {
        public string sourceName { get; set; } = string.Empty;

        public List<YoloPoseDetection>? poseDets { get; set; } = new();
        public List<YoloObjectDetection>? objectDets { get; set; } = new();
        public List<FaceDetection>? faceDets { get; set; } = new();
        public ChArUcoDetection? charucoDet { get; set; } = new();
        public ChessboardDetection? chessboardDet {  get; set; } = new();
    }
    public class ChArUcoReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public List<int> charucoIds { get; set; } = new();
        public List<Point3f> charucoCorners { get; set; } = new();
        public List<float> cornerReproductionError { get; set; } = new();
    }
    public class ChessboardReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public List<int> chessboardIds { get; set; } = new();
        public List<Point3f> chessboardCorners { get; set; } = new();
    }
    public class FaceReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Point3f BoxCenter { get; set; } = new(0, 0, 0);
        public float BoxSize { get; set; } = 0.0f;
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public FaceReconstruction() { }

        public FaceReconstruction(long TimestampOffset, Point3f boxCenter, float boxSize, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(TimestampOffset).LocalDateTime;
            BoxCenter = boxCenter;
            BoxSize = boxSize;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloObjectReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Point3f BoxCenter { get; set; } = new(0, 0, 0);
        public float BoxSize { get; set; } = 0.0f;
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public YoloObjectReconstruction() { }

        public YoloObjectReconstruction(long timestampOffset, Point3f boxCenter, float boxSize, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            BoxCenter = boxCenter;
            BoxSize = boxSize;
            Confidence = confidence;
            ClassId = classId;
        }
    }    
    public class YoloPoseReconstruction
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Point3f BoxCenter { get; set; } = new(0, 0, 0);
        public float BoxSize { get; set; } = 0.0f;
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        // 17 keypoints for pose, empty for object detection
        public List<Point3f> Keypoints { get; set; } = new();
        public List<float> Kp_Confidences { get; set; } = new();

        public YoloPoseReconstruction() { }

        public YoloPoseReconstruction(long timestampOffset, Point3f boxCenter, float boxSize, float confidence, int classId, List<Point3f> keypoints, List<float> kp_confidences)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            BoxCenter = boxCenter;
            BoxSize = boxSize;
            Confidence = confidence;
            ClassId = classId;
            Keypoints = keypoints ?? new();
            Kp_Confidences = kp_confidences ?? new();
        }
    }
    public class ChArUcoDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public bool Valid { get; set; } = false;

        public List<int> CharucoIds { get; set; } = new();
        public List<Point2f> CharucoCorners { get; set; } = new();
    }
    public class ChessboardDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public bool Valid { get; set; } = false;

        public List<int> ChessboardIds { get; set; } = new();
        public List<Point2f> ChessboardCorners { get; set; } = new();
    }
    public class FaceDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Rect2f Box { get; set; } = new(0, 0, 0, 0);
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public FaceDetection() { }

        public FaceDetection(long timestampOffset, Rect2f box, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            Box = box;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloObjectDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Rect2f Box { get; set; } = new(0, 0, 0, 0);
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        public YoloObjectDetection() { }

        public YoloObjectDetection(long timestampOffset, Rect2f box, float confidence, int classId)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            Box = box;
            Confidence = confidence;
            ClassId = classId;
        }
    }
    public class YoloPoseDetection
    {
        public DateTime Timestamp { get; set; } = DateTime.MinValue;
        public Rect2f Box { get; set; } = new(0, 0, 0, 0);
        public float Confidence { get; set; } = 0.0f;
        public int ClassId { get; set; } = -1;

        // 17 keypoints for pose, empty for object detection
        public List<Point2f> Keypoints { get; set; } = new();
        public List<float> Kp_Confidences { get; set; } = new();

        public YoloPoseDetection() { }

        public YoloPoseDetection(long timestampOffset, Rect2f box, float confidence, int classId, List<Point2f> keypoints, List<float> kp_confidences)
        {
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestampOffset).LocalDateTime;
            Box = box;
            Confidence = confidence;
            ClassId = classId;
            Keypoints = keypoints;
            Kp_Confidences = kp_confidences;

        }
    }

    public class Point2fConverter : JsonConverter<Point2f>
    {
        public override Point2f Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                reader.Read();
                float x = reader.GetSingle();

                reader.Read();
                float y = reader.GetSingle();

                reader.Read(); // EndArray

                return new Point2f(x, y);
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                float x = 0, y = 0;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    string prop = reader.GetString()!;
                    reader.Read();

                    if (prop.Equals("x", StringComparison.OrdinalIgnoreCase))
                        x = reader.GetSingle();
                    else if (prop.Equals("y", StringComparison.OrdinalIgnoreCase))
                        y = reader.GetSingle();
                }

                return new Point2f(x, y);
            }

            throw new JsonException("Invalid Point2f format.");
        }

        public override void Write(Utf8JsonWriter writer, Point2f value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
    public class Point3fConverter : JsonConverter<Point3f>
    {
        public override Point3f Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float x = 0, y = 0, z = 0;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Point3f(x, y, z);

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string property = reader.GetString();
                    reader.Read();

                    switch (property.ToLower())
                    {
                        case "x":
                            x = reader.GetSingle();
                            break;
                        case "y":
                            y = reader.GetSingle();
                            break;
                        case "z":
                            z = reader.GetSingle();
                            break;
                    }
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Point3f value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteNumber("z", value.Z);
            writer.WriteEndObject();
        }
    }
    public class UnixMillisecondsDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            long ms = reader.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            long ms = new DateTimeOffset(value).ToUnixTimeMilliseconds();
            writer.WriteNumberValue(ms);
        }
    }
}
