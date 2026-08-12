using System.Text.Json;

namespace Dplus_Desktop.Config
{
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
        public static async Task<int> MergeNewCalibrationData(string HubCalibrationSettingsFile)
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