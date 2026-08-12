using mLogger;

namespace Dplus_Desktop.Config
{
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
                OutputText("GetNodeIndex: logName cannot be null or empty.", mLogger.LogLevel.ERROR);
                return -1;
            }

            name = name.Trim().ToLower();

            // Must start with "node" or "Hub"
            if (!(name.StartsWith("node") || name.StartsWith("Hub")))
            {
                OutputText($"GetNodeIndex: Invalid prefix in '{name}'. Must start with 'node' or 'Hub'.", mLogger.LogLevel.ERROR);
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
                throw new ArgumentException($"No Nodes found with ClusterID {clusterID}.");
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
        public Extrinsics GetExtrinsicsForNode(string baseNodeName, string targetNodeName)
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
                throw new ArgumentException($"No extrinsics found for Nodes {baseNodeName} and {targetNodeName}.");
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
}
