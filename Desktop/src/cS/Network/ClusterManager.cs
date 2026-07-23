//using Dplus_Desktop.SettingsManager;
using Dplus_Desktop.SettingsManager;
using jCommunicator;
using mLogger;
using System.Text.RegularExpressions;

namespace Dplus_Desktop
{
    // Manages one cluster of raspberry pis
    class ClusterManager
    {
        Device _hub;
        Communicator _hubCom;

        List<Device> _nodes;

        DateTime LastUploadTime;

        Logger logger = Logger.Instance;

        public ClusterManager(Device hub, List<Device> nodes)
        {
            LastUploadTime = DateTime.MinValue;

            _hub = hub;
            _hubCom = new Communicator(hub.IPAddress, hub.Username, hub.Password);

            _nodes = new List<Device>();
            if (nodes != null && nodes.Count > 0)
                foreach (Device node in nodes)
                {
                    _nodes.Add(node);
                    if (node.isActive)
                    {
                        _hubCom.AddNodeSFTP(node.APAddress, node.Username);
                    }
                }

            LoadManagedFiles();
            //checkSSHDevice(currentHub, true);
        }

        private void LoadManagedFiles()
        {
            LoadSourceFiles();
            LoadRuntimeFiles();
            LoadModelFiles();

            Settings.SaveSettings();
        }
        private void LoadSourceFiles()
        {
            foreach (SourceFile file in Settings.All.SourceFiles)
            {
                string filePath = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
                string remotePath;
                if (file.IsForHub)
                    remotePath = Path.Combine(Settings.All.UploadDirectory, "_hub/", file.FileName).Replace("\\", "/");
                else
                    remotePath = Path.Combine(Settings.All.UploadDirectory, "node/", file.FileName).Replace("\\", "/");

                if (_hubCom.HubFileExists(remotePath))
                    file.LastUploadTime = _hubCom.HubFileLastModified(remotePath);
                else
                    file.LastUploadTime = DateTime.MinValue;

                if (File.Exists(filePath))
                    file.LastUploadTime = File.GetLastWriteTime(filePath);
                else
                    file.LastUploadTime = DateTime.MinValue;
            }
        }
        private void LoadRuntimeFiles()
        {
            foreach (RuntimeFile file in Settings.All.RuntimeFiles)
            {
                if (file.FileName.Contains('.'))
                {
                    //Settings file
                    if (file.FileName == "hubSettings.json")
                    {
                        file.LastSourceChangeTime = File.GetLastWriteTime("C:\\Users\\jerem\\OneDrive\\Documents\\Projects\\Programming\\apps\\Dplus\\Desktop\\managerSettings.json");
                        file.LastCompliedTime = File.GetLastWriteTime(Settings.All.SourceFilesDirectory + "hubSettings.json");
                        file.LastPushedTime = _hubCom.HubFileLastModified("/home/camcpp/src/hubSettings.json");
                    }
                    else if (file.FileName == "nodeSettings.json")
                    {
                        file.LastSourceChangeTime = File.GetLastWriteTime("C:\\Users\\jerem\\OneDrive\\Documents\\Projects\\Programming\\apps\\Dplus\\Desktop\\managerSettings.json");
                        file.LastCompliedTime = File.GetLastWriteTime(Settings.All.SourceFilesDirectory + "node1Settings.json");
                        file.LastPushedTime = _hubCom.NodeFileLastModified("/home/camcpp/src/hubSettings.json", "Node1");
                    }
                }
                else
                {
                    //bin file

                    bool isHub;
                    if (!file.IsForNode)
                    {
                        isHub = true;
                        file.LastCompliedTime = _hubCom.HubFileLastModified("/home/camcpp/_hub");
                        file.LastPushedTime = _hubCom.HubFileLastModified("/home/camcpp/_hub");
                    }
                    else
                    {
                        isHub = false;
                        file.LastCompliedTime = _hubCom.HubFileLastModified("/home/camcpp/node");
                        file.LastPushedTime = _hubCom.NodeFileLastModified("/home/camcpp/node", "Node1");
                    }

                    foreach (SourceFile sFile in Settings.All.SourceFiles)
                    {
                        if (isHub == sFile.IsForHub)
                        {
                            if (sFile.LastModifiedTime > file.LastSourceChangeTime)
                            {
                                file.LastSourceChangeTime = sFile.LastModifiedTime;
                            }
                        }
                    }
                }
            }
        }
        private void LoadModelFiles()
        {
            foreach (ModelFile file in Settings.All.Models)
            {
                file.LastModifiedTime = File.GetLastWriteTime(Path.Combine(Settings.All.LocalModelsPath, file.ModelName));
                file.LastPushTime = _hubCom.NodeFileLastModified(Path.Combine(Settings.All.RemoteModelsPath, file.ModelName), "Node1");
            }
        }

        public void UploadFiles()
        {
            UploadSourceFiles();
            UploadModelFiles();
        }
        private void UploadSourceFiles()
        {
            //List<SourceFile> itemsToProcess = Settings.All.SourceFiles;

            foreach (SourceFile file in Settings.All.SourceFiles)
            {
                string localFile = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
                string remoteHubFile = Path.Combine(Settings.All.UploadDirectory, "_hub/", file.FileName)
                                       .Replace("\\", "/"); // normalize to Linux paths
                string remoteNodeFile = Path.Combine(Settings.All.UploadDirectory, "node/", file.FileName)
                                       .Replace("\\", "/"); // normalize to Linux paths

                if (file.IsForHub)
                {
                    // Delete old file
                    _hubCom.DeleteHubFile(remoteHubFile);

                    // Upload new file
                    _hubCom.CopyPCtoHub(localFile, remoteHubFile);
                }
                if (file.IsForNode)
                {
                    // Delete old file
                    _hubCom.DeleteHubFile(remoteNodeFile);

                    // Upload new file
                    _hubCom.CopyPCtoHub(localFile, remoteNodeFile);
                }

                // Update settings
                DateTime now = DateTime.Now;
                file.LastUploadTime = now; // LastUploadTime column

            }

            Settings.SaveSettings();
        }
        private void UploadModelFiles()
        {
            foreach (ModelFile file in Settings.All.Models)
            {
                string localFile = Path.Combine(Settings.All.LocalModelsPath, file.ModelType, file.ModelName);

                // Try uploading to currentHub
                string remoteFile = Path.Combine(Settings.All.RemoteModelsPath, file.ModelType, file.ModelName).Replace("\\", "/");

                try
                {
                    _hubCom.CopyPCtoHub(localFile, remoteFile);
                    foreach (Device node in _nodes)
                    {
                        _hubCom.CopyHubToNode(remoteFile, remoteFile, node.APAddress, node.Username);
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.ERROR, "ClusterManager", $"Upload failed for {file.ModelName} on device {_hubCom._host}: {ex.Message}\n");
                }
            }

            Settings.SaveSettings();
        }

        public void DownloadFiles()
        {
            try
            {
                DownloadLogs();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading log files: " + ex.Message + '\n');
            }

            try
            {
                DownloadCaptures();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading capture files: " + ex.Message + '\n');
            }

            try
            {
                DownloadSavedFrames();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading frame files: " + ex.Message + '\n');
            }

            try
            {
                DownloadCalibration();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading calibrationfiles: " + ex.Message + '\n');
            }

            logger.Log(LogLevel.INFO, "Uploader", "Download process completed.\n");
        }
        private bool DownloadCalibration()
        {
            bool returnValue = false;
            string HubSettingsStartingPath = "/home/camcpp/src/hubSettings.json";
            string BaseLocalLogDir = Settings.All.LocalLogPath;
            string HubCalibrationSettingsFile = Path.Combine(BaseLocalLogDir, _hub.Name, "hubCalibrationSettings.json");

            if (_hubCom.CopyHubToPC(HubSettingsStartingPath.Trim(), HubCalibrationSettingsFile, false))
            {
                var info = new FileInfo(HubCalibrationSettingsFile);

                int newDataPoints = Settings.MergeNewCalibrationData(HubCalibrationSettingsFile);
                returnValue = true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"[{_hub.Name}] ⚠ Download failed: {HubCalibrationSettingsFile}\n");
                returnValue = false;
            }

            return returnValue;
        }
        private bool DownloadLogs()
        {
            bool hadErrors = false;
            string remoteLogDir = Settings.All.RemoteLogPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;

            // Hub logs
            {
                //OutputText($"Checking logs on {_hub.Name}...\n", LogLevel.INFO);

                // Ask remote system for a list of .log files
                string cmd = $"ls -1 {remoteLogDir}*.log 2>/dev/null";
                string result = _hubCom.ExecuteHubCommand(cmd);

                string[] remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (remoteFiles.Length == 0)
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"No .log files found on {_hub.Name}.\n");
                }
                else
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"Found {remoteFiles.Length} .log files on {_hub.Name}.\n");

                    foreach (string remoteFilePath in remoteFiles)
                    {
                        string baseLocalHubLogDir = Path.Combine(baseLocalLogDir, _hub.Name, "Logs");
                        if (!Directory.Exists(baseLocalHubLogDir))
                            Directory.CreateDirectory(baseLocalHubLogDir);

                        string fileName = Path.GetFileName(remoteFilePath.Trim());
                        string localFilePath = Path.Combine(baseLocalHubLogDir, fileName);

                        //OutputText($"[{_hub.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                        if (_hubCom.CopyHubToPC(remoteFilePath.Trim(), localFilePath, false))
                        {
                            var info = new FileInfo(localFilePath);
                            logger.Log(LogLevel.INFO, "Uploader", $"[{_hub.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                            if (!(GetDateFromLogFilename(fileName) == DateTime.Today))
                            {
                                if (_hubCom.DeleteHubFile(remoteFilePath.Trim(), false))
                                {
                                    logger.Log(LogLevel.INFO, "Uploader", $"[{_hub.Name}] 🗑 Deleted remote log file: {remoteFilePath}\n");
                                }
                                else
                                {
                                    logger.Log(LogLevel.ERROR, "Uploader", $"[{_hub.Name}] ⚠ Successfully downloaded, but could not delete remote log file: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                        }
                        else
                        {
                            logger.Log(LogLevel.ERROR, "Uploader", $"[{_hub.Name}] ⚠ Download failed: {localFilePath}\n");
                            hadErrors = true;
                        }
                    }
                }
            }

            foreach (Device node in _nodes)
            {
                // Create per-device local log folder
                string deviceLocalLogDir = Path.Combine(baseLocalLogDir, node.Name, "Logs");
                if (!Directory.Exists(deviceLocalLogDir))
                    Directory.CreateDirectory(deviceLocalLogDir);

                //OutputText($"Checking logs on {node.Name} ({node.APAddress})...\n", LogLevel.INFO);

                // Ask remote system for a list of .log files
                string cmd = $"ls -1 {remoteLogDir}*.log 2>/dev/null";
                string result = _hubCom.ExecuteNodeCommand(cmd, node.APAddress, node.Username);

                string[] remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (remoteFiles.Length == 0)
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"No .log files found on {node.Name}.\n");
                    continue;
                }
                else
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"Found {remoteFiles.Length} .log files on {node.Name}.\n");

                    foreach (string remoteFilePath in remoteFiles)
                    {
                        string fileName = Path.GetFileName(remoteFilePath.Trim());
                        string localFilePath = Path.Combine(deviceLocalLogDir, fileName);

                        //OutputText($"[{node.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                        if (_hubCom.CopyNodeToPC(remoteFilePath.Trim(), localFilePath, node.APAddress, false))
                        {
                            var info = new FileInfo(localFilePath);
                            logger.Log(LogLevel.INFO, "Uploader", $"[{node.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                            if (!(GetDateFromLogFilename(fileName) == DateTime.Today))
                            {
                                if (_hubCom.DeleteNodeFile(remoteFilePath.Trim(), node.APAddress, false))//, node.Username, false))
                                {
                                    logger.Log(LogLevel.INFO, "Uploader", $"[{node.Name}] 🗑 Deleted remote log file: {remoteFilePath}\n");
                                }
                                else
                                {
                                    logger.Log(LogLevel.ERROR, "Uploader", $"[{node.Name}] ⚠ Successfully downloaded, but could not delete remote log file: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                        }
                        else
                        {
                            logger.Log(LogLevel.ERROR, "Uploader", $"[{node.Name}] ⚠ Download reported success but file not found: {localFilePath}\n");
                            hadErrors = true;
                        }
                    }                    
                }
            }
            return !hadErrors;
        }
        public static DateTime? GetDateFromLogFilename(string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
                return null;

            // Get just the filename (no directory)
            string fileName = Path.GetFileNameWithoutExtension(logFilePath);

            // Match the date pattern YYYY-MM-DD at the end of the filename
            var match = Regex.Match(fileName, @"(\d{4}-\d{2}-\d{2})$");
            if (!match.Success)
                return null;

            if (DateTime.TryParseExact(match.Value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime logDate))
                return logDate;

            return null;
        }
        private bool DownloadCaptures()
        {
            bool hadErrors = false;
            string[] captureTypes = new string[] { "Captures", "Charuco", "Chessboard", "Face", "Motion", "Preprocessed", "Startup", "YoloObject", "YoloPose" };
            string remoteCapturesDir = Settings.All.RemoteCapturesPath;
            string baseLocalCapturesDir = Settings.All.LocalLogPath;

            foreach (Device node in _nodes)
            {                
                // Create per-device local log folder
                string deviceLocalCapturesDir = Path.Combine(baseLocalCapturesDir, node.Name);

                logger.Log(LogLevel.INFO, "Uploader", $"Checking Captures on {node.Name} ({node.APAddress})...\n");

                // Ask remote system for a list of .png files for each capture type
                foreach (string type in captureTypes)
                {
                    string remoteTypeDir = Path.Combine(remoteCapturesDir, type);
                    string localTypeDir = Path.Combine(deviceLocalCapturesDir, type);
                    if (!Directory.Exists(localTypeDir))
                        Directory.CreateDirectory(localTypeDir);

                    string cmd = $"ls -1 {remoteTypeDir}/*.png 2>/dev/null";
                    string result = _hubCom.ExecuteNodeCommand(cmd, node.APAddress, node.Username);

                    var remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                    if (remoteFiles.Length == 0)
                    {
                        logger.Log(LogLevel.INFO, "Uploader", $"No {type} files found on {node.Name}.\n");
                        continue;
                    }
                    else
                    {
                        logger.Log(LogLevel.INFO, "Uploader", $"Found {remoteFiles.Length} {type} files on {node.Name}.\n");

                        foreach (string remoteFilePath in remoteFiles)
                        {
                            string fileName = Path.GetFileName(remoteFilePath.Trim());
                            string localFilePath = Path.Combine(localTypeDir, fileName);

                            //OutputText($"[{node.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                            if (_hubCom.CopyNodeToPC(remoteFilePath.Trim(), localFilePath, node.APAddress))
                            {
                                if (_hubCom.DeleteNodeFile(remoteFilePath.Trim(), node.APAddress, false))//, node.Username, false))
                                {
                                    var info = new FileInfo(localFilePath);
                                    logger.Log(LogLevel.INFO, "Uploader", $"[{node.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                                }
                                else
                                {
                                    logger.Log(LogLevel.ERROR, "Uploader", $"[{node.Name}] ⚠ Successfully downloaded, but could not delete remote file after download: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                            else
                            {
                                logger.Log(LogLevel.ERROR, "Uploader", $"[{node.Name}] ⚠ Download failed: {localFilePath}\n");
                                hadErrors = true;
                            }
                        }
                    }
                }
            }
            return !hadErrors;
        }
        private bool DownloadSavedFrames()
        {
            bool hadErrors = false;
            string remoteReconstructionsDir = Settings.All.RemoteReconstructionsPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;

            // Hub reconstructions
            {
                logger.Log(LogLevel.INFO, "Uploader", $"Checking reconstructions on {_hub.Name}...\n");

                // Ask remote system for a list of .json files
                string cmd = $"ls -1 {remoteReconstructionsDir}*.json 2>/dev/null";
                string result = _hubCom.ExecuteHubCommand(cmd);

                var remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (remoteFiles.Length == 0)
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"No reconstruction files found on {_hub.Name}.\n");
                }
                else
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"Found {remoteFiles.Length} reconstruction files on {_hub.Name}.\n");

                    foreach (string remoteFilePath in remoteFiles)
                    {
                        string baseLocalHubReconstructionsDir = Path.Combine(baseLocalLogDir, _hub.Name, "Reconstructions");
                        if (!Directory.Exists(baseLocalHubReconstructionsDir))
                            Directory.CreateDirectory(baseLocalHubReconstructionsDir);

                        string fileName = Path.GetFileName(remoteFilePath.Trim());
                        string localFilePath = Path.Combine(baseLocalHubReconstructionsDir, fileName);

                        //OutputText($"[{_hub.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                        if (_hubCom.CopyHubToPC(remoteFilePath.Trim(), localFilePath))
                        {
                            var info = new FileInfo(localFilePath);
                            //OutputText($"[{_hub.Name}] ✔ Verified: {localFilePath} ({info.Length} bytes)\n", LogLevel.INFO);
                            if (_hubCom.DeleteHubFile(remoteFilePath.Trim(), false))
                            {
                                logger.Log(LogLevel.INFO, "Uploader", $"[{_hub.Name}] ✔ Successfully downloaded reconstruction file: {remoteFilePath}\n");
                                //OutputText($"[{_hub.Name}] 🗑 Deleted reconstruction file: {remoteFilePath}\n", LogLevel.INFO);
                            }
                            else
                            {
                                logger.Log(LogLevel.ERROR, "Uploader", $"[{_hub.Name}] ⚠ Successfully downloaded, but could not delete reconstruction file: {remoteFilePath}\n");
                                hadErrors = true;
                            }
                        }
                        else
                        {
                            logger.Log(LogLevel.ERROR, "Uploader", $"[{_hub.Name}] ⚠ Download failed: {localFilePath}\n");
                            hadErrors = true;
                        }
                    }
                }
            }

            return !hadErrors;
        }

        public void ManualRecompile()
        {
            stopMain();

            //currentHub.DeleteFile("/home/camcpp/previous_node_d");
            //currentHub.MoveFile("/home/camcpp/node", "/home/camcpp/previous_node_d");
            //currentHub.ExecuteCommand("g++ -o node -g /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include"); // debug

            _hubCom.DeleteHubFile("/home/camcpp/previous_hub");
            _hubCom.MoveHubFile("/home/camcpp/hub", "/home/camcpp/previous_hub");
            _hubCom.DeleteHubFile("/home/camcpp/previous_node");
            _hubCom.MoveHubFile("/home/camcpp/node", "/home/camcpp/previous_node");

            logger.Log(LogLevel.INFO, "Uploader", "Ready to recompile manually.  Please Run:\n");
            logger.Log(LogLevel.INFO, "Uploader", "time make -C /home/camcpp/build\n");
        }
        public void AutoRecompile()
        {
            stopMain();

            //currentHub.DeleteFile("/home/camcpp/previous_node_d");
            //currentHub.MoveFile("/home/camcpp/node", "/home/camcpp/previous_node_d");
            //currentHub.ExecuteCommand("g++ -o node -g /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include"); // debug

            _hubCom.DeleteHubFile("/home/camcpp/previous_hub");
            _hubCom.MoveHubFile("/home/camcpp/hub", "/home/camcpp/previous_hub");
            _hubCom.DeleteHubFile("/home/camcpp/previous_node");
            _hubCom.MoveHubFile("/home/camcpp/node", "/home/camcpp/previous_node");

            //currentHub.ExecuteHubCommand("g++ -O2 -o node  /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include");    // release
            _hubCom.ExecuteHubCommand("make -C /home/camcpp/build");
        }
        public void startMain()
        {
            _hubCom.ExecuteHubCommand("sudo systemctl start hub.service");

            foreach (Device node in _nodes)
            {
                _hubCom.ExecuteNodeCommand($"sudo systemctl start node.service", node.APAddress, node.Username);
            }
        }
        public void stopMain()
        {
            _hubCom.ExecuteHubCommand("sudo systemctl stop hub.service");

            foreach (Device node in _nodes)
                _hubCom.ExecuteNodeCommand($"sudo systemctl stop node.service", node.APAddress, node.Username);
        }

        public void RebootCluster()
        {
            try
            {
                // Reboot each Node
                foreach (Device node in _nodes)
                    _hubCom.ExecuteHubCommand($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown -r +1\" > /dev/null 2>&1 &");
                _hubCom.ExecuteHubCommand("sudo shutdown -r now");

                _hubCom.Disconnect();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error: " + ex.Message + '\n');
            }
        }
        public void ShutdownCluster()
        {
            try
            {
                // Shutdown each Node
                foreach (Device node in _nodes)
                    _hubCom.ExecuteHubCommand($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown now\" > /dev/null 2>&1 &");
                    
                _hubCom.ExecuteHubCommand("sudo shutdown now");

                _hubCom.Disconnect();
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error: " + ex.Message + '\n');
            }
        }
    }
}