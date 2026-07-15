using OpenCvSharp;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Ocsp;
using Renci.SshNet.Messages;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType; 

using mLogger;

namespace Dplus_Desktop
{
    public class FlexibleDateTimeConverter : JsonConverter<DateTime?>
    {
        private static readonly string[] formats = new[]
        {
            "yyyy-MM-dd HH:mm:ss",
            "MM-dd-yyyy hh:mm:sstt",
            "M-d-yyyy h:mm:sstt",
            "yyyy-MM-ddTHH:mm:ss",
        };

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? str = reader.GetString();
                if (DateTime.TryParseExact(str, formats, null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                {
                    return dt;
                }
            }
            return null; // fallback if invalid
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            else
                writer.WriteNullValue();
        }
    }
    public class AppSettings
    {
        // Remote directories
        public string RemoteHomePath { get; set; } = string.Empty;
        public string RemoteCapturesPath { get; set; } = string.Empty;
        public string RemoteReconstructionsPath { get; set; } = string.Empty;
        public string RemoteLogPath { get; set; } = string.Empty;
        public string RemoteModelsPath { get; set; } = string.Empty;

        // Local directories
        public string UploadDirectory { get; set; } = string.Empty;
        public string LocalLogPath { get; set; } = string.Empty;
        public string LocalModelsPath { get; set; } = string.Empty;
        public string SourceFilesDirectory { get; set; } = string.Empty;

        // Viewer settings
        public string NoImagePath { get; set; } = string.Empty;
        public int LiveFrameBufferLimit { get; set; } = 1;
        public int SavedFrameBufferLimit { get; set; } = 1;
        public string OrthographicViewerProfileToUse { get; set; } = "InitializerDefault";
        public List<OrthographicViewerSettings> OrthoViewerSettings { get; set; } = new List<OrthographicViewerSettings>();
        public OrthographicViewerSettings GetOrthographicViewerSettings()
        {
            string profileName = OrthographicViewerProfileToUse;
            if (profileName == string.Empty || profileName == "" || profileName == "InitializerDefault")
                return new OrthographicViewerSettings { profileName = "DesignFallback", MinGridSpacing = 20, MaxGridSpacing = 100, GridSpacing = 10 };

            var settings = OrthoViewerSettings.FirstOrDefault(s => s.profileName == profileName);
            if (settings == null)
            {
                throw new ArgumentException($"No OrthographicViewerSettings found with profileName {profileName}.");
            }
            return settings;
        }

        // MQTT Settings
        public string HubTelemetryTopic { get; set; } = string.Empty;
        public string HubCommandTopic { get; set; } = string.Empty;
        public string NodeTelemetryTopic { get; set; } = string.Empty;
        public string NodeCommandTopic { get; set; } = string.Empty;

        // Hub Behavior
        public bool ForceExtrinsicsRecalibration { get; set; } = false;
        public float RepropuctionErrThreshAtExtrinsicsCalcualtion { get; set; } = 0.0f;
        public int MaxMQTTQueueSize { get; set; } = 0;
        public bool SaveReconstructions { get; set; } = false;
        public bool BroadcastReconstructions { get; set; } = false;

        // Node Behavior
        public int MaxFrameLatenessMs { get; set; } = 0;
        public bool ForceIntrinsicsRecalibration { get; set; } = false;

        // Detector Settings
        public List<YoloPoseDetectorProfile> YoloPoseDetProfiles { get; set; } = new();
        public YoloPoseDetectorProfile getYoloPoseDetectorProfileByClusterID(string clusterID)
        {
            string targetProfileName = GetClusterProfileByName(clusterID).YoloPoseProfileToUse;
            var profile = YoloPoseDetProfiles.FirstOrDefault(p => p.name == targetProfileName);
            if (profile == null)
            {
                throw new ArgumentException($"No YoloPoseDetectorProfile found with name '{clusterID}'.");
            }
            return profile;
        }
        public List<YoloObjectDetectorProfile> YoloObjectDetProfiles { get; set; } = new();
        public YoloObjectDetectorProfile getYoloObjectDetectorProfileByClusterID(string clusterID)
        {
            var profile = YoloObjectDetProfiles.FirstOrDefault(p => p.name == GetClusterProfileByName(clusterID).YoloObjectProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No YoloObjectDetectorProfile found with name '{clusterID}'.");
            }
            return profile;
        }
        public List<FaceIDDetectorProfile> FaceIDDetProfiles { get; set; } = new();
        public FaceIDDetectorProfile getFaceIDDetectorProfileByClusterID(string clusterID)
        {
            var profile = FaceIDDetProfiles.FirstOrDefault(p => p.name == GetClusterProfileByName(clusterID).FaceIDProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No FaceIDDetectorSettings found with name '{clusterID}'.");
            }
            return profile;
        }
        public List<ChArUcoBoardDetectorProfile> ChArUcoBoardDetProfiles { get; set; } = new();
        public ChArUcoBoardDetectorProfile getChArUcoBoardDetectorProfileByClusterID(string clusterID)
        {
            var profile = ChArUcoBoardDetProfiles.FirstOrDefault(p => p.name == GetClusterProfileByName(clusterID).ChArUcoDetProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No ChArUcoBoardDetectorSettings found with name '{clusterID}'.");
            }
            return profile;
        }
        public ChArUcoBoardDetectorProfileForHubSetting getChArUcoBoardDetectorProfileForHubSettingByClusterID(string clusterID)
        {
            var profile = ChArUcoBoardDetProfiles.FirstOrDefault(p => p.name == GetClusterProfileByName(clusterID).ChArUcoDetProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No ChArUcoBoardDetectorSettings found with name '{clusterID}'.");
            }
            ChArUcoBoardDetectorProfileForHubSetting returnValue = new ChArUcoBoardDetectorProfileForHubSetting
            {
                name = profile.name,
                useChArUcoBoardDetection = profile.useChArUcoBoardDetection,
                saveChArUcoBoardDetections = profile.saveChArUcoBoardDetections,
                chArUcoBoard = GetChArUcoBoardParametersByName(profile.chArUcoBoardToUse),
                RepErrThreshAtReconstruction = profile.RepErrThreshAtReconstruction
            };
            return returnValue;
        }
        public List<ChessboardDetectorProfile> ChessboardDetProfiles { get; set; } = new();
        public ChessboardDetectorProfile getChessboardDetectorProfileByClusterID(string clusterID)
        {
            var profile = ChessboardDetProfiles.FirstOrDefault(p => p.name == GetClusterProfileByName(clusterID).ChessboardDetProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No ChessboardDetectorSettings found with name '{clusterID}'.");
            }
            return profile;
        }
        public ChessboardDetectorProfileForHubSetting getChessboardDetectorProfileForHubSettingByClusterID(string clusterID)
        {
            var profile = ChessboardDetProfiles.FirstOrDefault(p => p.name == GetClusterProfileByName(clusterID).ChessboardDetProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No ChessboardDetectorSettings found with name '{clusterID}'.");
            }
            ChessboardDetectorProfileForHubSetting returnValue = new ChessboardDetectorProfileForHubSetting
            {
                name = profile.name,
                useChessboardDetection = profile.useChessboardDetection,
                saveChessboardDetections = profile.saveChessboardDetections,
                chessboard = GetChessboardParametersByName(profile.chessboardToUse)
            };
            return returnValue;
        }

        // Collections
        public List<Device> Hubs { get; set; } = new();
        public List<Device> Nodes { get; set; } = new();
        public Device? GetDeviceByName(string name)
        {
            var device = Hubs.FirstOrDefault(d => d.Name == name) ?? Nodes.FirstOrDefault(d => d.Name == name);
            if (device == null)
            {
                throw new ArgumentException($"No device found with name {name}.");
            }
            return device;
        }
        public int GetNodeIndex(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                OutputText("GetNodeIndex: Name cannot be null or empty.", mLogger.LogLevel.ERROR);
                return -1;
            }

            name = name.Trim().ToLower();

            // Must start with "node" or "hub"
            if (!(name.StartsWith("node") || name.StartsWith("hub")))
            {
                OutputText($"GetNodeIndex: Invalid prefix in '{name}'. Must start with 'node' or 'hub'.", mLogger.LogLevel.ERROR);
                return -1;
            }

            // Extract numeric portion
            string numberPart = name.StartsWith("node")
                ? name.Substring(4)
                : name.Substring(3);

            if (string.IsNullOrWhiteSpace(numberPart))
            {
                OutputText($"GetNodeIndex: No numeric index found in '{name}'.", mLogger.LogLevel.ERROR);
                return -1;
            }

            // Ensure it's a valid integer
            if (!int.TryParse(numberPart, out int index))
            {
                OutputText($"GetNodeIndex: Invalid numeric index in '{name}'.", mLogger.LogLevel.ERROR);
                return -1;
            }

            return index;
        }
        public int GetNodeIndex(Device device)
        {
            return GetNodeIndex(device.Name);
        }
        public List<Device> GetNodesByClusterID(string clusterID, bool getHub = false, bool onlyActive = true)
        {
            var nodes = Nodes.Where(d => d.ClusterID == clusterID && (!onlyActive || d.isActive)).ToList();
            if (getHub)
            {
                var hub = Hubs.FirstOrDefault(h => h.ClusterID == clusterID);
                if (hub != null && (!onlyActive || hub.isActive))
                {
                    nodes.Insert(0, hub);
                }
            }
            if (nodes.Count == 0)
            {
                throw new ArgumentException($"No nodes found with ClusterID {clusterID}.");
            }
            return nodes;
        }

        public List<Intrinsics> Intrinsics { get; set; } = new();
        public Intrinsics GetIntrinsicsForCameraID(int cameraID)
        {
            var intr = Intrinsics
                .Where(i => i.CameraIDnumber == cameraID)
                .MinBy(i => i.Rms);

            if (intr != null)
                return intr;

            return new Intrinsics
            {
                CameraIDnumber = cameraID,
                Rms = double.MaxValue,
                ImageWidth = 0,
                ImageHeight = 0,
                K = Array.Empty<double[]>(),
                Dist = Array.Empty<double>()
            };
        }
        public Intrinsics GetIntrinsicsForNode(string nodeName)
        {
            var device = GetDeviceByName(nodeName);
            if (device == null)
            {
                throw new ArgumentException($"No device found with name {nodeName}.");
            }
            if (device.CameraIDnumber <= 0)
            {
                throw new ArgumentException($"Device {nodeName} does not have a valid CameraIDnumber.");
            }
            return GetIntrinsicsForCameraID(device.CameraIDnumber);
        }
        public List<Extrinsics> Extrinsics { get; set; } = new();
        public Extrinsics GetExtrinsicsForNode(string baseNodeName,string targetNodeName)
        {
            Extrinsics returnValue;

            if (baseNodeName == null)
            {
                throw new ArgumentNullException(nameof(baseNodeName), "Base node name cannot be null.");
            }
            if (targetNodeName == null)
            {
                throw new ArgumentNullException(nameof(targetNodeName), "Target node name cannot be null.");
            }

            returnValue = Extrinsics.FirstOrDefault(e => e.baseNodeName == baseNodeName && e.targetNodeName == targetNodeName);

            if (returnValue == null)
            {
                throw new ArgumentException($"No extrinsics found for nodes {baseNodeName} and {targetNodeName}.");
            }

            return returnValue;
        }

        public List<SourceFile> SourceFiles { get; set; } = new();
        public List<RuntimeFile> RuntimeFiles { get; set; } = new();
        public List<ModelFile> Models { get; set; } = new();
        public string ClusterProfileToUse { get; set; } = string.Empty;
        public List<ClusterProfile> ClusterProfiles { get; set; } = new();
        public ClusterProfile GetClusterProfile()
        {
            if (string.IsNullOrEmpty(ClusterProfileToUse))
            {
                throw new ArgumentException("No ClusterProfileToUse specified in settings.");
            }
            var profile = ClusterProfiles.FirstOrDefault(p => p.profileName == ClusterProfileToUse);
            if (profile == null)
            {
                throw new ArgumentException($"No ClusterProfile found with name {ClusterProfileToUse}.");
            }
            return profile;
        }
        public ClusterProfile GetClusterProfileByName(string profileName)
        {
            var profile = ClusterProfiles.FirstOrDefault(p => p.profileName == profileName);
            if (profile == null)
            {
                throw new ArgumentException($"No ClusterProfile found with name {profileName}.");
            }
            return profile;
        }
        public List<ChessboardParameters> Chessboards { get; set; } = new();
        public ChessboardParameters GetChessboardParametersByName(string name)
        {
            var board = Chessboards.FirstOrDefault(b => b.name == name);
            if (board == null)
            {
                throw new ArgumentException($"No ChessBoardParamters found with name {name}.");
            }
            return board;
        }
        public ChessboardParameters GetChessboardParametersForClusterProfile(ClusterProfile profile)
        {
            //ClusterProfile cProfile = GetClusterProfileByName(profile.profileName);
            ChessboardDetectorProfile chessboardSettings = getChessboardDetectorProfileByClusterID(profile.profileName);
            return GetChessboardParametersByName(chessboardSettings.chessboardToUse);
        }
        public List<ChArUcoBoardParameters> chArUcoBoards { get; set; } = new();
        public ChArUcoBoardParameters GetChArUcoBoardParametersByName(string name)
        {
            var board = chArUcoBoards.FirstOrDefault(b => b.name == name);
            if (board == null)
            {
                throw new ArgumentException($"No ChArUcoBoardParameters found with name {name}.");
            }
            return board;
        }
        public ChArUcoBoardParameters GetChArUcoBoardParametersForClusterProfile(ClusterProfile profile)
        {
            //ClusterProfile cProfile = GetClusterProfileByName(profile.profileName);
            ChArUcoBoardDetectorProfile chessboardSettings = getChArUcoBoardDetectorProfileByClusterID(profile.profileName);
            return GetChArUcoBoardParametersByName(chessboardSettings.chArUcoBoardToUse);
        }

        private void OutputText(string text, mLogger.LogLevel level)
        {
            text = text.TrimEnd('\r', '\n');
            //text = text + "\n";

            Logger.Instance.Log(level, "Settings", text);
        }
    }

    public class Device
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string ClusterID { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string APAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int CameraIDnumber { get; set; } = 0;
    }
    public class Intrinsics
    {
        private const double FLT_EPS = 1e-9;

        public int CameraIDnumber { get; set; } = 0;
        public double Rms { get; set; } = double.MaxValue;
        public int ImageWidth { get; set; } = 640;
        public int ImageHeight { get; set; } = 480;

        public double[][] K { get; set; } =
        {
        new double[3],
        new double[3],
        new double[3]
        };

        public double[] Dist { get; set; } = Array.Empty<double>();
        public static bool operator ==(Intrinsics a, Intrinsics b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a.CameraIDnumber != b.CameraIDnumber ||
                a.ImageWidth != b.ImageWidth ||
                a.ImageHeight != b.ImageHeight)
                return false;

            if (Math.Abs(a.Rms - b.Rms) > FLT_EPS)
                return false;

            // Compare K
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Math.Abs(a.K[i][j] - b.K[i][j]) > FLT_EPS)
                        return false;
                }
            }

            // Compare Dist
            if (a.Dist.Length != b.Dist.Length)
                return false;

            for (int i = 0; i < a.Dist.Length; i++)
            {
                if (Math.Abs(a.Dist[i] - b.Dist[i]) > FLT_EPS)
                    return false;
            }

            return true;
        }
        public static bool operator !=(Intrinsics a, Intrinsics b)
        {
            return !(a == b);
        }
        public override bool Equals(object obj)
        {
            if (obj is not Intrinsics other)
                return false;

            return this == other;
        }
        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(CameraIDnumber);
            hash.Add(Rms);
            hash.Add(ImageWidth);
            hash.Add(ImageHeight);

            foreach (var row in K)
                foreach (var v in row)
                    hash.Add(v);

            foreach (var v in Dist)
                hash.Add(v);

            return hash.ToHashCode();
        }
        // RMS-based ordering
        public static bool operator <(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms < b.Rms;
        }
        public static bool operator >(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms > b.Rms;
        }
        public static bool operator <=(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms <= b.Rms;
        }
        public static bool operator >=(Intrinsics a, Intrinsics b)
        {
            if (a is null || b is null)
                throw new ArgumentNullException();

            return a.Rms >= b.Rms;
        }

        
        public PointF Project(Point2f normalized)
        {
            double fx = K[0][0];
            double fy = K[1][1];
            double cx = K[0][2];
            double cy = K[1][2];

            float x = (float)(fx * normalized.X + cx);
            float y = (float)(fy * normalized.Y + cy);

            return new PointF(x, y);
        }
        public Rect2f Project(Rect2f normalizedBox)
        {
            var tl = Project(new Point2f(normalizedBox.X, normalizedBox.Y));
            var br = Project(new Point2f(
                normalizedBox.X + normalizedBox.Width,
                normalizedBox.Y + normalizedBox.Height));

            return new Rect2f(
                tl.X,
                tl.Y,
                br.X - tl.X,
                br.Y - tl.Y
            );
        }
    }
    public class Extrinsics
    {
        private const double FLT_EPS = 1e-9;
        public string baseNodeName { get; set; } = string.Empty;
        public string targetNodeName { get; set; } = string.Empty;
        public double[][] R { get; set; }
        public double[] t { get; set; }

        public static Extrinsics Identity(string nodeName)
        {
            return new Extrinsics
            {
                baseNodeName = nodeName,
                targetNodeName = nodeName,
                R = new double[][]
                {
                new double[] { 1, 0, 0 },
                new double[] { 0, 1, 0 },
                new double[] { 0, 0, 1 }
                },
                t = new double[] { 0, 0, 0 }
            };
        }

        public static bool operator ==(Extrinsics a, Extrinsics b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            if (a.baseNodeName != b.baseNodeName ||
                a.targetNodeName != b.targetNodeName)
                return false;

            // Compare R
            if (a.R.Length != b.R.Length)
                return false;

            for (int i = 0; i < a.R.Length; i++)
            {
                if (a.R[i].Length != b.R[i].Length)
                    return false;

                for (int j = 0; j < a.R[i].Length; j++)
                {
                    if (Math.Abs(a.R[i][j] - b.R[i][j]) > FLT_EPS)
                        return false;
                }
            }

            // Compare t
            if (a.t.Length != b.t.Length)
                return false;

            for (int i = 0; i < a.t.Length; i++)
            {
                if (Math.Abs(a.t[i] - b.t[i]) > FLT_EPS)
                    return false;
            }

            return true;
        }

        public static bool operator !=(Extrinsics a, Extrinsics b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is not Extrinsics other)
                return false;

            return this == other;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            hash.Add(baseNodeName);
            hash.Add(targetNodeName);

            foreach (var row in R)
                foreach (var v in row)
                    hash.Add(v);

            foreach (var v in t)
                hash.Add(v);

            return hash.ToHashCode();
        }
    }

    public class SourceFile
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime? LastUploadTime { get; set; }
        public bool IsForHub { get; set; } = false;
        public bool IsForNode { get; set; } = false;
    }
    public class RuntimeFile
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsForNode { get; set; } = false;
        public string Path { get; set; } = string.Empty;
    }
    public class ModelFile
    {
        public string ModelName { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public DateTime? LastPushTime { get; set; } = null;
    }

    public class YoloPoseDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        // Behavior
        public bool useModel { get; set; } = false;
        public bool saveWholeDetectionImage { get; set; } = false;
        public bool savePartialDetectionImage { get; set; } = false;

        // Constants
        public string modelPath { get; set; } = string.Empty;
        public int cocoPKcount { get; set; } = 17;

        // Thresholds
        public double detectConfThreshold { get; set; } = 0.0;
        public double kpDetectThreshold { get; set; } = 0.0;
        public double nmsThreshold { get; set; } = 0.0;
        public double iouThreshold { get; set; } = 0.0;
        public double ReconstructionThreshold { get; set; } = 0.0;
    }
    public class YoloObjectDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        // Behavior
        public bool useModel { get; set; } = false;
        public bool saveWholeDetectionImage { get; set; } = false;
        public bool savePartialDetectionImage { get; set; } = false;
        // Constants
        public string modelPath { get; set; } = string.Empty;
        public string classes { get; set; } = string.Empty;
        // Thresholds
        public double objectConfidence { get; set; } = 0.0;
        public double iouThreshold { get; set; } = 0.0;
    }
    public class FaceIDDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        public bool useHaarFaceDetection { get; set; } = false;
        public bool saveHaarDetections { get; set; } = false;
        public string haarFaceModel { get; set; } = string.Empty;
        public bool useLBPHFaceRecognition { get; set; } = false;
        public bool saveLBPHRecognitions { get; set; } = false;
        public string lbphFaceRecognizeModel { get; set; } = string.Empty;
        public string FaceClassNames { get; set; } = string.Empty;
    }
    public class ChArUcoBoardDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        public bool useChArUcoBoardDetection { get; set; } = false;
        public bool saveChArUcoBoardDetections { get; set; } = false;
        public string chArUcoBoardToUse { get; set; } = string.Empty;
        public int RepErrThreshAtReconstruction { get; set; } = 0;
    }
    public class ChessboardDetectorProfile
    {
        public string name { get; set; } = string.Empty;
        public bool useChessboardDetection { get; set; } = false;
        public bool saveChessboardDetections { get; set; } = false;
        public string chessboardToUse { get; set; } = string.Empty;
    }

    public class ChessboardParameters
    {
        public string name { get; set; } = string.Empty;
        public int squaresX { get; set; } = 0;
        public int squaresY { get; set; } = 0;
        public float squareLength { get; set; } = 0.0f;
    }
    public class ChArUcoBoardParameters
    {
        public string name { get; set; } = string.Empty;
        public int squaresX { get; set; } = 0;
        public int squaresY { get; set; } = 0;
        public int minDetections { get; set; } = 10;
        public float squareLength { get; set; } = 0.0f;
        public float markerLength { get; set; } = 0.0f;
        public int dictionaryID { get; set; } = 0;
    }
    public class ClusterProfile
    {
        public string profileName { get; set; } = string.Empty;
        public List<Extrinsics> extrinsics { get; set; } = new();
        public int intrinsicsCaptureCount { get; set; } = 0;
        public int extrinsicsCaptureCount { get; set; } = 0;

        public string YoloPoseProfileToUse { get; set; } = string.Empty;
        public string YoloObjectProfileToUse { get; set; } = string.Empty;
        public string FaceIDProfileToUse { get; set; } = string.Empty;
        public string ChArUcoDetProfileToUse { get; set; } = string.Empty;
        public string ChessboardDetProfileToUse { get; set; } = string.Empty;

        //public string chessboardToUse { get; set; } = string.Empty;
        //public string chArUcoBoardToUse { get; set; } = string.Empty;
        //public bool useHaarFaceDetection { get; set; } = false;
        //public bool saveHaarDetections { get; set; } = false;
        //public string haarFaceModel { get; set; } = string.Empty;
        //public bool useLBPHFaceRecognition { get; set; } = false;
        //public bool saveLBPHRecognitions { get; set; } = false;
        //public string lbphFaceRecognizeModel { get; set; } = string.Empty;
        //public bool useYolo11Pose { get; set; } = false;
        //public bool saveYolo11PoseDetections { get; set; } = false;
        //public string yolo11PoseModel { get; set; } = string.Empty;
        //public double yolo11PoseConfidence { get; set; } = 0.0;
        //public double yolo11PoseIouThreshold { get; set; } = 0.0;
        //public bool useYolo11Object { get; set; } = false;
        //public bool saveYolo11ObjectDetections { get; set; } = false;
        //public string yolo11ObjectModel { get; set; } = string.Empty;
        //public string yolo11ObjectClasses { get; set; } = string.Empty;
        //public double yolo11ObjectConfidence { get; set; } = 0.0;
        //public double yolo11ObjectIouThreshold { get; set; } = 0.0;
        public bool captureOnStartup { get; set; } = false;
        public bool captureEachFrame { get; set; } = false;
        public bool captureEachDetection { get; set; } = false;
        public bool captureNewDetection { get; set; } = false;
        public double targetFrameRate { get; set; } = 0.0;
        public string indicatorType { get; set; } = string.Empty;
        public int introSequenceIterations { get; set; } = 0;
        public int introSequenceDelay { get; set; } = 0;
    }
    public class OrthographicViewerSettings
    {
        public string profileName { get; set; } = string.Empty;
        public int MinGridSpacing { get; set; } = 1;
        public int MaxGridSpacing { get; set; } = 1;
        public float GridSpacing { get; set; } = 10;
    }

    public class HubSettings
    {
        public string name { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string clusterID { get; set; } = string.Empty;
        public string hubName { get; set; } = string.Empty;
        public string hubIPaddress { get; set; } = string.Empty;
        public string hubTelemetryTopic { get; set; } = string.Empty;
        public string hubCommandTopic { get; set; } = string.Empty;
        public string nodeTelemetryTopic { get; set; } = string.Empty;
        public string nodeCommandTopic { get; set; } = string.Empty;
        public List<NodeInfoForHubSettings> nodes { get; set; } = new();
        public List<Intrinsics> intrinsics { get; set; } = new();
        public List<Extrinsics> extrinsics { get; set; } = new();
        public string rootDir { get; set; } = string.Empty;
        public string captureDir { get; set; } = string.Empty;
        public string srcDir { get; set; } = string.Empty;
        public string logDir { get; set; } = string.Empty;
        public string modelDir { get; set; } = string.Empty;

        public bool forceExtrinsicsRecalibration { get; set; } = false;
        public float repropuctionErrThreshAtExtrinsicsCalcualtion { get; set; } = 0.0f;
        public int maxMQTTQueueSize { get; set; } = 0;
        public bool saveReconstructions { get; set; } = false;
        public bool broadcastReconstructions { get; set; } = false;

        public int extrinsicsCaptureCount { get; set; } = 0;

        public YoloPoseDetectorProfile yoloPoseDetSettings { get; set; } = new YoloPoseDetectorProfile();
        public YoloObjectDetectorProfile yoloObjectDetSettings { get; set; } = new YoloObjectDetectorProfile();
        public FaceIDDetectorProfile faceIDDetSettings { get; set; } = new FaceIDDetectorProfile(); 
        public ChArUcoBoardDetectorProfileForHubSetting chArUcoBoardDetSettings { get; set; } = new ChArUcoBoardDetectorProfileForHubSetting();
        public ChessboardDetectorProfileForHubSetting chessboardDetSettings { get; set; } = new ChessboardDetectorProfileForHubSetting();
        //public string chArUcoBoardToUse { get; set; } = string.Empty;
        //public List<ChArUcoBoardParameters> chArUcoBoards { get; set; } = new();
        //public bool useHaarFaceDetection { get; set; } = false;
        //public bool saveHaarDetections { get; set; } = false;
        //public string haarFaceModel { get; set; } = string.Empty;
        //public bool useLBPHFaceRecognition { get; set; } = false;
        //public bool saveLBPHRecognitions { get; set; } = false;
        //public string lbphFaceRecognizeModel { get; set; } = string.Empty;
        //public bool useYolo11Pose { get; set; } = false;
        //public bool saveYolo11PoseDetections { get; set; } = false;
        //public string yolo11PoseModel { get; set; } = string.Empty;
        //public double yolo11PoseConfidence { get; set; } = 0.0;
        //public double yolo11PoseIouThreshold { get; set; } = 0.0;
        //public bool useYolo11Object { get; set; } = false;
        //public bool saveYolo11ObjectDetections { get; set; } = false;
        //public string yolo11ObjectModel { get; set; } = string.Empty;
        //public string yolo11ObjectClasses { get; set; } = string.Empty;
        //public double yolo11ObjectConfidence { get; set; } = 0.0;
        //public double yolo11ObjectIouThreshold { get; set; } = 0.0;
        public bool captureOnStartup { get; set; } = false;
        public bool captureEachFrame { get; set; } = false;
        public bool captureEachDetection { get; set; } = false;
        public bool captureNewDetection { get; set; } = false;
        public double targetFrameRate { get; set; } = 0.0;
        public string indicatorType { get; set; } = string.Empty;
        public int introSequenceIterations { get; set; } = 0;
        public int introSequenceDelay { get; set; } = 0;
    }
    public class NodeSettings
    {
        public string name { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string clusterID { get; set; } = string.Empty;
        public string hubName { get; set; } = string.Empty;
        public string hubIPaddress { get; set; } = string.Empty;
        public string nodeTelemetryTopic { get; set; } = string.Empty;
        public string nodeCommandTopic { get; set; } = string.Empty;
        public string rootDir { get; set; } = string.Empty;
        public string captureDir { get; set; } = string.Empty;
        public string srcDir { get; set; } = string.Empty;
        public string logDir { get; set; } = string.Empty;
        public string modelDir { get; set; } = string.Empty;

        public int maxFrameLatenessMs { get; set; } = 0;
        public bool forceIntrinsicsRecalibration { get; set; } = false;
        public Intrinsics? intrinsics { get; set; } = new Intrinsics();
        public int intrinsicsCaptureCount { get; set; } = 0;

        public YoloPoseDetectorProfile yoloPoseDetSettings { get; set; } = new YoloPoseDetectorProfile();
        public YoloObjectDetectorProfile yoloObjectDetSettings { get; set; } = new YoloObjectDetectorProfile();
        public FaceIDDetectorProfile faceIDDetSettings { get; set; } = new FaceIDDetectorProfile();
        public ChArUcoBoardDetectorProfileForHubSetting chArUcoBoardDetSettings { get; set; } = new ChArUcoBoardDetectorProfileForHubSetting();
        public ChessboardDetectorProfileForHubSetting chessboardDetSettings { get; set; } = new ChessboardDetectorProfileForHubSetting();

        //public string chessboardToUse { get; set; } = string.Empty;
        //public List<ChessboardParameters> chessboards { get; set; } = new();
        //public string chArUcoBoardToUse { get; set; } = string.Empty;
        //public List<ChArUcoBoardParameters> chArUcoBoards { get; set; } = new();
        //public bool useHaarFaceDetection { get; set; } = false;
        //public bool saveHaarDetections { get; set; } = false;
        //public string haarFaceModel { get; set; } = string.Empty;
        //public bool useLBPHFaceRecognition { get; set; } = false;
        //public bool saveLBPHRecognitions { get; set; } = false;
        //public string lbphFaceRecognizeModel { get; set; } = string.Empty;
        //public bool useYolo11Pose { get; set; } = false;
        //public bool saveYolo11PoseDetections { get; set; } = false;
        //public string yolo11PoseModel { get; set; } = string.Empty;
        //public double yolo11PoseConfidence { get; set; } = 0.0;
        //public double yolo11PoseIouThreshold { get; set; } = 0.0;
        //public bool useYolo11Object { get; set; } = false;
        //public bool saveYolo11ObjectDetections { get; set; } = false;
        //public string yolo11ObjectModel { get; set; } = string.Empty;
        //public string yolo11ObjectClasses { get; set; } = string.Empty;
        //public double yolo11ObjectConfidence { get; set; } = 0.0;
        //public double yolo11ObjectIouThreshold { get; set; } = 0.0;
        public bool captureOnStartup { get; set; } = false;
        public bool captureEachFrame { get; set; } = false;
        public bool captureEachDetection { get; set; } = false;
        public bool captureNewDetection { get; set; } = false;
        public double targetFrameRate { get; set; } = 0.0;
        public string indicatorType { get; set; } = string.Empty;
        public int introSequenceIterations { get; set; } = 0;
        public int introSequenceDelay { get; set; } = 0;
    }
    public class NodeInfoForHubSettings
    {
        public string name { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string IPAddress { get; set; } = string.Empty;
        public Intrinsics? intrinsics { get; set; } = new Intrinsics();
    }
    public class ChArUcoBoardDetectorProfileForHubSetting
    {
        public string name { get; set; } = string.Empty;
        public bool useChArUcoBoardDetection { get; set; } = false;
        public bool saveChArUcoBoardDetections { get; set; } = false;
        public ChArUcoBoardParameters chArUcoBoard { get; set; } = new ChArUcoBoardParameters();
        public int RepErrThreshAtReconstruction { get; set; } = 10;
    }
    public class ChessboardDetectorProfileForHubSetting
    {
        public string name { get; set; } = string.Empty;
        public bool useChessboardDetection { get; set; } = false;
        public bool saveChessboardDetections { get; set; } = false;
        public ChessboardParameters chessboard { get; set; } = new ChessboardParameters();
    }

    internal static class Settings
    {
        const string ManagerSettingsFilePath = @"C:\Users\jerem\OneDrive\Documents\Projects\Programming\apps\Dplus\Desktop\managerSettings.json";

        public static AppSettings All { get; private set; } = new AppSettings();

        public static bool isLoaded { get; private set; } = false;

        public static void LoadSettings()
        {
            if (!File.Exists(ManagerSettingsFilePath))
            {
                isLoaded  = false;
                return;
            }

            string json = File.ReadAllText(ManagerSettingsFilePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleDateTimeConverter() }
            };
            All = JsonSerializer.Deserialize<AppSettings>(json, options) ?? new AppSettings();
            isLoaded = true;
        }
        public static int MergeNewCalibrationData(string HubCalibrationSettingsFile)
        {
            int updated = 0;

            if (!File.Exists(HubCalibrationSettingsFile))
            {
                return updated;
            }

            string json = File.ReadAllText(HubCalibrationSettingsFile);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleDateTimeConverter() }
            };
            HubSettings newData = JsonSerializer.Deserialize<HubSettings>(json, options) ?? new HubSettings();

            // Merge Intrinsics
            foreach (var intr in newData.intrinsics)
            {
                if (!All.Intrinsics.Contains(intr))
                {
                    All.Intrinsics.Add(intr);
                    updated ++;
                }
            }

            // Merge Extrinsics
            foreach (var extr in newData.extrinsics)
            {
                if (!All.Extrinsics.Contains(extr))
                {
                    All.Extrinsics.Add(extr);
                    updated ++;
                }
            }

            // Save if Updated
            if (updated > 0) 
            {
                SaveSettings();
            }

            return updated;
        }
        public static void SaveSettings()
        {
            string json = JsonSerializer.Serialize(All, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ManagerSettingsFilePath, json);
        }
        public static void SaveHubSettings(Device device, ClusterProfile profile, string filePath)
        {
            if (device.Role != "Hub")
            {
                throw new ArgumentException($"Device {device.Name} must have role 'Hub' to get HubSettings.");
            }
            HubSettings returnValue = new HubSettings();

            returnValue.name = device.Name;
            returnValue.role = device.Role;
            returnValue.clusterID = device.ClusterID;
            returnValue.hubName = device.Name;
            returnValue.hubIPaddress = device.APAddress;

            returnValue.hubTelemetryTopic = All.HubTelemetryTopic;
            returnValue.hubCommandTopic = All.HubCommandTopic;
            returnValue.nodeTelemetryTopic = All.NodeTelemetryTopic;
            returnValue.nodeCommandTopic = All.NodeCommandTopic;

            foreach (var nodeDevice in Settings.All.Nodes.Where(n => n.ClusterID == device.ClusterID))
            {
                NodeInfoForHubSettings nodeSettings = new NodeInfoForHubSettings
                {
                    name = nodeDevice.Name,
                    role = nodeDevice.Role,
                    isActive = nodeDevice.isActive,
                    IPAddress = nodeDevice.APAddress,
                };
                if (nodeDevice.CameraIDnumber > 0)
                {
                    nodeSettings.intrinsics = All.GetIntrinsicsForCameraID(nodeDevice.CameraIDnumber); /*new Intrinsics
                    {
                        CameraIDnumber = nodeDevice.Intrinsics.CameraIDnumber,
                        Rms = nodeDevice.Intrinsics.Rms,
                        ImageWidth = nodeDevice.Intrinsics.ImageWidth,
                        ImageHeight = nodeDevice.Intrinsics.ImageHeight,
                        K = nodeDevice.Intrinsics.K,
                        Dist = nodeDevice.Intrinsics.Dist
                    }; */
                }
                returnValue.nodes.Add(nodeSettings);
            }

            var clusterNodeNames = Settings.All.Nodes.Where(n => n.ClusterID == device.ClusterID).Select(n => n.Name).ToHashSet();
            foreach (var intr in Settings.All.Intrinsics)
            {
                returnValue.intrinsics.Add(new Intrinsics
                {
                    //baseNodeName = intr.baseNodeName,
                    //targetNodeName = intr.targetNodeName,
                    //R = intr.R,
                    //t = intr.t
                    CameraIDnumber = intr.CameraIDnumber,
                    Rms = intr.Rms,
                    ImageWidth = intr.ImageWidth,
                    ImageHeight = intr.ImageHeight,
                    K = intr.K,
                    Dist = intr.Dist
                });
            }
            foreach (var extr in Settings.All.Extrinsics)
            {
                returnValue.extrinsics.Add(new Extrinsics
                {
                    baseNodeName = extr.baseNodeName,
                    targetNodeName = extr.targetNodeName,
                    R = extr.R,
                    t = extr.t
                });
            }
            returnValue.rootDir = All.RemoteHomePath;
            returnValue.captureDir = All.RemoteCapturesPath;
            returnValue.srcDir = All.UploadDirectory;
            returnValue.logDir = All.RemoteLogPath;
            returnValue.modelDir = All.RemoteModelsPath;

            returnValue.forceExtrinsicsRecalibration = All.ForceExtrinsicsRecalibration;
            returnValue.repropuctionErrThreshAtExtrinsicsCalcualtion = All.RepropuctionErrThreshAtExtrinsicsCalcualtion;
            returnValue.maxMQTTQueueSize = All.MaxMQTTQueueSize;
            returnValue.saveReconstructions = All.SaveReconstructions;
            returnValue.broadcastReconstructions = All.BroadcastReconstructions;

            returnValue.extrinsicsCaptureCount = profile.extrinsicsCaptureCount;

            returnValue.yoloPoseDetSettings = All.getYoloPoseDetectorProfileByClusterID(profile.profileName); //profile.YoloPoseProfileToUse != string.Empty ? All.YoloPoseDetProfiles.First(p => p.name == profile.YoloPoseProfileToUse) : new YoloPoseDetectorProfile();
            returnValue.yoloObjectDetSettings = All.getYoloObjectDetectorProfileByClusterID(profile.profileName); //profile.YoloObjectProfileToUse != string.Empty ? All.YoloObjectDetProfiles.First(p => p.name == profile.YoloObjectProfileToUse) : new YoloObjectDetectorProfile();
            returnValue.faceIDDetSettings = All.getFaceIDDetectorProfileByClusterID(profile.profileName); //profile.FaceIDProfileToUse != string.Empty ? All.FaceIDDetProfiles.First(p => p.name == profile.FaceIDProfileToUse) : new FaceIDDetectorProfile();
            returnValue.chArUcoBoardDetSettings = All.getChArUcoBoardDetectorProfileForHubSettingByClusterID(profile.profileName); //profile.ChArUcoBoardProfileToUse != string.Empty ? All.ChArUcoBoardDetProfilesForHubSettings.First(p => p.name == profile.ChArUcoBoardProfileToUse) : new ChArUcoBoardDetectorProfileForHubSetting();
            returnValue.chessboardDetSettings = All.getChessboardDetectorProfileForHubSettingByClusterID(profile.profileName); //profile.ChessboardProfileToUse != string.Empty ? All.ChessboardDetProfilesForHubSettings.First(p => p.name == profile.ChessboardProfileToUse) : new ChessboardDetectorProfileForHubSetting();

            //returnValue.chArUcoBoardToUse = profile.chArUcoBoardToUse;
            ////returnValue.chArUcoBoards = profile.chArUcoBoards;
            //foreach (ChArUcoBoardParameters board in All.chArUcoBoards)
            //{
            //    ChArUcoBoardParameters newBoard = new ChArUcoBoardParameters
            //    {
            //        name = board.name,
            //        squaresX = board.squaresX,
            //        squaresY = board.squaresY,
            //        minDetections = board.minDetections,
            //        squareLength = board.squareLength,
            //        markerLength = board.markerLength,
            //        dictionaryID = board.dictionaryID
            //    };
            //    returnValue.chArUcoBoards.Add(newBoard);
            //}
            //returnValue.useHaarFaceDetection = profile.useHaarFaceDetection;
            //returnValue.saveHaarDetections = profile.saveHaarDetections;
            //returnValue.haarFaceModel = profile.haarFaceModel;
            //returnValue.useLBPHFaceRecognition = profile.useLBPHFaceRecognition;
            //returnValue.saveLBPHRecognitions = profile.saveLBPHRecognitions;
            //returnValue.lbphFaceRecognizeModel = profile.lbphFaceRecognizeModel;
            //returnValue.useYolo11Pose = profile.useYolo11Pose;
            //returnValue.saveYolo11PoseDetections = profile.saveYolo11PoseDetections;
            //returnValue.yolo11PoseModel = profile.yolo11PoseModel;
            //returnValue.yolo11PoseConfidence = profile.yolo11PoseConfidence;
            //returnValue.yolo11PoseIouThreshold = profile.yolo11PoseIouThreshold;
            //returnValue.useYolo11Object = profile.useYolo11Object;
            //returnValue.saveYolo11ObjectDetections = profile.saveYolo11ObjectDetections;
            //returnValue.yolo11ObjectModel = profile.yolo11ObjectModel;
            //returnValue.yolo11ObjectClasses = profile.yolo11ObjectClasses;
            //returnValue.yolo11ObjectConfidence = profile.yolo11ObjectConfidence;
            //returnValue.yolo11ObjectIouThreshold = profile.yolo11ObjectIouThreshold;
            returnValue.captureOnStartup = profile.captureOnStartup;
            returnValue.captureEachFrame = profile.captureEachFrame;
            returnValue.captureEachDetection = profile.captureEachDetection;
            returnValue.captureNewDetection = profile.captureNewDetection;
            returnValue.targetFrameRate = profile.targetFrameRate;
            returnValue.indicatorType = profile.indicatorType;
            returnValue.introSequenceIterations = profile.introSequenceIterations;
            returnValue.introSequenceDelay = profile.introSequenceDelay;

            File.WriteAllText(filePath, JsonSerializer.Serialize(returnValue, new JsonSerializerOptions { WriteIndented = true }));
        }
        public static void SaveNodeSettings(Device device, ClusterProfile profile, string filePath)
        {
            if (device.Role != "Node")
            {
                throw new ArgumentException($"Device {device.Name} must have role 'Node' to get NodeSettings.");
            }
            NodeSettings returnValue = new NodeSettings();
            Device hub;
            try
            {
                hub = Settings.All.Hubs.First(h => h.ClusterID == device.ClusterID);

                returnValue.name = device.Name;
                returnValue.role = device.Role;
                returnValue.isActive = device.isActive;
                returnValue.clusterID = device.ClusterID;
                returnValue.hubName = hub.Name;
                returnValue.hubIPaddress = hub.APAddress;

                returnValue.nodeTelemetryTopic = All.NodeTelemetryTopic;
                returnValue.nodeCommandTopic = All.NodeCommandTopic;

                returnValue.rootDir = All.RemoteHomePath;
                returnValue.captureDir = All.RemoteCapturesPath;
                returnValue.srcDir = All.UploadDirectory;
                returnValue.logDir = All.RemoteLogPath;
                returnValue.modelDir = All.RemoteModelsPath;

                returnValue.maxFrameLatenessMs = All.MaxFrameLatenessMs;
                returnValue.forceIntrinsicsRecalibration = All.ForceIntrinsicsRecalibration;

                returnValue.intrinsics = All.GetIntrinsicsForCameraID(device.CameraIDnumber);
                returnValue.intrinsicsCaptureCount = profile.intrinsicsCaptureCount;


                returnValue.yoloPoseDetSettings = All.getYoloPoseDetectorProfileByClusterID(profile.profileName); //profile.YoloPoseProfileToUse != string.Empty ? All.YoloPoseDetProfiles.First(p => p.name == profile.YoloPoseProfileToUse) : new YoloPoseDetectorProfile();
                returnValue.yoloObjectDetSettings = All.getYoloObjectDetectorProfileByClusterID(profile.profileName); //profile.YoloObjectProfileToUse != string.Empty ? All.YoloObjectDetProfiles.First(p => p.name == profile.YoloObjectProfileToUse) : new YoloObjectDetectorProfile();
                returnValue.faceIDDetSettings = All.getFaceIDDetectorProfileByClusterID(profile.profileName); //profile.FaceIDProfileToUse != string.Empty ? All.FaceIDDetProfiles.First(p => p.name == profile.FaceIDProfileToUse) : new FaceIDDetectorProfile();
                returnValue.chArUcoBoardDetSettings = All.getChArUcoBoardDetectorProfileForHubSettingByClusterID(profile.profileName); //profile.ChArUcoBoardProfileToUse != string.Empty ? All.ChArUcoBoardDetProfilesForHubSettings.First(p => p.name == profile.ChArUcoBoardProfileToUse) : new ChArUcoBoardDetectorProfileForHubSetting();
                returnValue.chessboardDetSettings = All.getChessboardDetectorProfileForHubSettingByClusterID(profile.profileName); //profile.ChessboardProfileToUse != string.Empty ? All.ChessboardDetProfilesForHubSettings.First(p => p.name == profile.ChessboardProfileToUse) : new ChessboardDetectorProfileForHubSetting();

                //returnValue.chessboardToUse = profile.chessboardToUse;
                //foreach (ChessboardParameters board in All.Chessboards)
                //{
                //    returnValue.chessboards.Add(new ChessboardParameters
                //    {
                //        name = board.name,
                //        squaresX = board.squaresX,
                //        squaresY = board.squaresY,
                //        squareLength = board.squareLength
                //    });
                //}
                //returnValue.chArUcoBoardToUse = profile.chArUcoBoardToUse;
                //foreach (ChArUcoBoardParameters board in All.chArUcoBoards)
                //{
                //    returnValue.chArUcoBoards.Add(new ChArUcoBoardParameters
                //    {
                //        name = board.name,
                //        squaresX = board.squaresX,
                //        squaresY = board.squaresY,
                //        minDetections = board.minDetections,
                //        squareLength = board.squareLength,
                //        markerLength = board.markerLength,
                //        dictionaryID = board.dictionaryID
                //    });
                //}
                //returnValue.useHaarFaceDetection = profile.useHaarFaceDetection;
                //returnValue.saveHaarDetections = profile.saveHaarDetections;
                //returnValue.haarFaceModel = profile.haarFaceModel;
                //returnValue.useLBPHFaceRecognition = profile.useLBPHFaceRecognition;
                //returnValue.saveLBPHRecognitions = profile.saveLBPHRecognitions;
                //returnValue.lbphFaceRecognizeModel = profile.lbphFaceRecognizeModel;
                //returnValue.useYolo11Pose = profile.useYolo11Pose;
                //returnValue.saveYolo11PoseDetections = profile.saveYolo11PoseDetections;
                //returnValue.yolo11PoseModel = profile.yolo11PoseModel;
                //returnValue.yolo11PoseConfidence = profile.yolo11PoseConfidence;
                //returnValue.yolo11PoseIouThreshold = profile.yolo11PoseIouThreshold;
                //returnValue.useYolo11Object = profile.useYolo11Object;
                //returnValue.saveYolo11ObjectDetections = profile.saveYolo11ObjectDetections;
                //returnValue.yolo11ObjectModel = profile.yolo11ObjectModel;
                //returnValue.yolo11ObjectClasses = profile.yolo11ObjectClasses;
                //returnValue.yolo11ObjectConfidence = profile.yolo11ObjectConfidence;
                //returnValue.yolo11ObjectIouThreshold = profile.yolo11ObjectIouThreshold;
                returnValue.captureOnStartup = profile.captureOnStartup;
                returnValue.captureEachFrame = profile.captureEachFrame;
                returnValue.captureEachDetection = profile.captureEachDetection;
                returnValue.captureNewDetection = profile.captureNewDetection;
                returnValue.targetFrameRate = profile.targetFrameRate;
                returnValue.indicatorType = profile.indicatorType;
                returnValue.introSequenceIterations = profile.introSequenceIterations;
                returnValue.introSequenceDelay = profile.introSequenceDelay;

                File.WriteAllText(filePath, JsonSerializer.Serialize(returnValue, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException($"No Hub found for Node {device.Name} with ClusterID {device.ClusterID}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving NodeSettings for {device.Name}: {ex.Message}");
            }
        }
    }
}