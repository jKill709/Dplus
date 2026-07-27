//using Dplus_Desktop.SettingsManager;
using Dplus_Desktop.SettingsManager;
using jCommunicator;
using Microsoft.Extensions.Logging;
using mLogger;
using System.Text.RegularExpressions;
using static OpenCvSharp.ML.DTrees;

namespace Dplus_Desktop
{
    // Manages one cluster of raspberry pis using a jCommunicator.Communicator
    class ClusterManager
    {
        public Device _hub { get; }
        Communicator _hubCom;

        List<Device> _nodes { get; }

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
            //checkSSHDevice(currentCluster, true);
        }

        public ServiceStatus CheckSystem()
        {
            if (CheckSSH())
            {
                ServiceStatus returnValue;// = ServiceStatus.Active;
                ServiceStatus hubValue = CheckDeviceServiceStatus(_hub);

                returnValue = hubValue;
                foreach (Device node in _nodes)
                {
                    ServiceStatus nodeValue = CheckDeviceServiceStatus(node);
                    if (nodeValue > returnValue)
                        returnValue = nodeValue;
                }
                return returnValue;
            }
            else
                return ServiceStatus.Error;
        }
        public bool CheckSSH(bool verbose = false)
        {
            string host = _hubCom._host;
            string username = _hubCom._username;

            if (verbose)
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Checking SSH connection to device {host} as {username}...\n");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isCnctd = false;

            try
            {
                if (_hubCom.Connect())
                {
                    if (verbose)
                        logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Successfully connected to {host} in {sw.ElapsedMilliseconds} ms.\n");
                    isCnctd = true;
                }
                else
                {
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Failed to connect to {host} without error.\n");
                }
            }
            catch (Renci.SshNet.Common.SshAuthenticationException authEx)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Authentication failed for {username}@{host}: {authEx.Message}\n");
            }
            catch (Renci.SshNet.Common.SshConnectionException connEx)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Connection error to {host}: {connEx.Message}\n");
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Socket error while connecting to {host}: {sockEx.Message}\n");
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Unexpected error for {host}: {ex.GetType().Name} - {ex.Message}\n");
            }
            finally
            {
                sw.Stop();

                if (verbose)
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Total connection attempt time for {host}: {sw.ElapsedMilliseconds} ms.\n");
            }

            return isCnctd;
        }
        public ServiceStatus CheckDeviceServiceStatus(string deviceName)
        {
            Device? device = Settings.All.GetDeviceByName(deviceName);
            if (device == null)
                return ServiceStatus.Error; 
            return CheckDeviceServiceStatus(device);
        }
        public ServiceStatus CheckDeviceServiceStatus(Device device)
        {
            string serviceName;
            bool isHub;
            if (device.Role == "Hub")
            {
                serviceName = "hub.service";
                isHub = true;
            }
            else if (device.Role == "Node")
            {
                serviceName = "device.service";
                isHub = false;
            }
            else
                return ServiceStatus.Error;

            try
            {
                if (!isHub)
                    if (!_hubCom.PingNode(device.APAddress))
                        return ServiceStatus.Error;

                string result = "";
                try
                {
                    if (isHub)
                        result = _hubCom.ExecuteHubCommand($"systemctl is-active {serviceName}");
                    else
                        result = _hubCom.ExecuteNodeCommand($"systemctl is-active {serviceName}", device.APAddress, device.Username).Trim();
                }
                catch (Exception ex)
                {
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Error checking service status on device '{device.Name}' ({device.APAddress}): {ex.Message}\n");
                    if (ex.InnerException != null)
                        logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Inner exception: {ex.InnerException.Message}\n");

                    result = "failed";
                }

                switch (result)
                {
                    case "active":
                        return ServiceStatus.Active;

                    case "inactive":
                        return ServiceStatus.Inactive;

                    case "failed":
                        return ServiceStatus.Failed;

                    case "activating":
                        return ServiceStatus.Activating;

                    case "deactivating":
                        return ServiceStatus.Deactivating;

                    default:
                        logger.Log(mLogger.LogLevel.WARN, "ClusterManager", $"Unknown service state '{result}' for device '{device.Name}'.\n");
                        return ServiceStatus.Error;
                
                }
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Unhandled error while processing device '{device.Name}': {ex.Message}\n");
                if (ex.InnerException != null)
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Inner exception: {ex.InnerException.Message}\n");
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Stack Trace:\n{ex.StackTrace}\n");
                return ServiceStatus.Error;
            }
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
                    remotePath = Path.Combine(Settings.All.UploadDirectory, "hub/", file.FileName).Replace("\\", "/");
                else
                    remotePath = Path.Combine(Settings.All.UploadDirectory, "node/", file.FileName).Replace("\\", "/");

                if (_hubCom.IsConnected)
                {
                    if (_hubCom.HubFileExists(remotePath))
                        file.LastUploadTime = _hubCom.HubFileLastModified(remotePath);
                    else
                        file.LastUploadTime = DateTime.MinValue;
                }
                if (File.Exists(filePath))
                    file.LastModifiedTime = File.GetLastWriteTime(filePath);
                else
                    file.LastUploadTime = DateTime.MinValue;
            }
        }
        private void LoadRuntimeFiles()
        {
            if (_hubCom.IsConnected)
            {
                foreach (RuntimeFile file in Settings.All.RuntimeFiles)
                {
                    if (file.FileName.Contains('.')) // Separates bin files from .json files.  No other files should be in this data structure.
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
                            file.LastCompliedTime = File.GetLastWriteTime(Settings.All.SourceFilesDirectory + "Node1Settings.json");
                            file.LastPushedTime = _hubCom.NodeFileLastModified("/home/camcpp/src/nodeSettings.json", "10.0.0.11");
                        }
                    }
                    else
                    {
                        //bin file

                        if (!file.IsForNode)
                        {
                            file.LastCompliedTime = _hubCom.HubFileLastModified("/home/camcpp/hub");
                            file.LastPushedTime = _hubCom.HubFileLastModified("/home/camcpp/hub");

                            file.LastSourceChangeTime = DateTime.MinValue;
                            foreach (SourceFile sFile in Settings.All.SourceFiles)
                            {
                                if (sFile.IsForHub)
                                {
                                    if (sFile.LastModifiedTime > file.LastSourceChangeTime)
                                    {
                                        file.LastSourceChangeTime = sFile.LastModifiedTime;
                                    }
                                }
                            }
                        }
                        else
                        {
                            file.LastCompliedTime = _hubCom.HubFileLastModified("/home/camcpp/node");
                            file.LastPushedTime = _hubCom.NodeFileLastModified("/home/camcpp/node", "10.0.0.11");

                            file.LastSourceChangeTime = DateTime.MinValue;
                            foreach (SourceFile sFile in Settings.All.SourceFiles)
                            {
                                if (sFile.IsForNode)
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
            }        
        }
        private void LoadModelFiles()
        {
            
            foreach (ModelFile file in Settings.All.Models)
            {
                file.LastModifiedTime = File.GetLastWriteTime(Path.Combine(Settings.All.LocalModelsPath, file.ModelName));
                if (_hubCom.IsConnected)
                {
                    file.LastPushTime = _hubCom.NodeFileLastModified(Path.Combine(Settings.All.RemoteModelsPath, file.ModelType, file.ModelName).Replace("\\", "/"), "10.0.0.11");
                }
            }
        }

        public void UploadFiles()
        {
            UploadSourceFiles();
            UploadModelFiles();

            LoadManagedFiles();
        }
        private void UploadSourceFiles()
        {
            //List<SourceFile> itemsToProcess = Settings.All.SourceFiles;

            foreach (SourceFile file in Settings.All.SourceFiles)
            {
                if (file.LastModifiedTime > file.LastUploadTime)
                {
                    string localFile = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
                    string remoteHubFile = Path.Combine(Settings.All.UploadDirectory, "hub/", file.FileName).Replace("\\", "/"); // normalize to Linux paths
                    string remoteNodeFile = Path.Combine(Settings.All.UploadDirectory, "node/", file.FileName).Replace("\\", "/"); // normalize to Linux paths

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
            }

            Settings.SaveSettings();
        }
        private void UploadModelFiles()
        {
            foreach (ModelFile file in Settings.All.Models)
            {
                if (file.LastModifiedTime > file.LastPushTime)
                {
                    string localFile = Path.Combine(Settings.All.LocalModelsPath, file.ModelType, file.ModelName);

                    // Try uploading to currentCluster
                    string remoteFile = Path.Combine(Settings.All.RemoteModelsPath, file.ModelType, file.ModelName).Replace("\\", "/");

                    try
                    {
                        _hubCom.DeleteHubFile(remoteFile);
                        _hubCom.CopyPCtoHub(localFile, remoteFile);
                        foreach (Device node in _nodes)
                        {
                            _hubCom.DeleteNodeFile(remoteFile, node.APAddress);
                            _hubCom.CopyHubToNode(remoteFile, remoteFile, node.APAddress, node.Username);
                        }
                        logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Uploaded '{localFile}' → '{remoteFile}' to all nodes");
                    }
                    catch (Exception ex)
                    {
                        logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Upload failed for {file.ModelName} on device {_hubCom._host}: {ex.Message}\n");
                    }

                }            }

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
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error downloading log files: " + ex.Message + '\n');
            }

            try
            {
                DownloadCaptures();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error downloading capture files: " + ex.Message + '\n');
            }

            try
            {
                DownloadSavedFrames();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error downloading frame files: " + ex.Message + '\n');
            }

            try
            {
                DownloadCalibration();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error downloading calibrationfiles: " + ex.Message + '\n');
            }

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Download process completed.\n");
        }
        private bool DownloadCalibration()
        {
            string HubSettingsStartingPath = "/home/camcpp/src/hubSettings.json";
            string BaseLocalLogDir = Settings.All.LocalLogPath;
            string HubCalibrationSettingsFile = Path.Combine(BaseLocalLogDir, _hub.Name, "hubCalibrationSettings.json");

            if (_hubCom.CopyHubToPC(HubSettingsStartingPath.Trim(), HubCalibrationSettingsFile, false))
            {
                var info = new FileInfo(HubCalibrationSettingsFile);

                int newDataPoints = Settings.MergeNewCalibrationData(HubCalibrationSettingsFile);
                return true;
            }
            else
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{_hub.Name}] ⚠ Download failed: {HubCalibrationSettingsFile}\n");
                return false;
            }
        }
        private bool DownloadLogs()
        {
            bool hadErrors = false;
            string remoteLogDir = Settings.All.RemoteLogPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;

            // Hub logs
            {
                string[] remoteFiles = _hubCom.GetListOfHubFiles(remoteLogDir, "log");

                if (remoteFiles.Length == 0)
                {
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"No .log files found on {_hub.Name}.\n");
                }
                else
                {
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Found {remoteFiles.Length} .log files on {_hub.Name}.\n");

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
                            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"[{_hub.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                            if (!(GetDateFromLogFilename(fileName) == DateTime.Today))
                            {
                                if (_hubCom.DeleteHubFile(remoteFilePath.Trim(), false))
                                {
                                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"[{_hub.Name}] 🗑 Deleted remote log file: {remoteFilePath}\n");
                                }
                                else
                                {
                                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{_hub.Name}] ⚠ Successfully downloaded, but could not delete remote log file: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                        }
                        else
                        {
                            logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{_hub.Name}] ⚠ Download failed: {localFilePath}\n");
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

                //OutputText($"Checking logs on {device.Name} ({device.APAddress})...\n", LogLevel.INFO);

                // Ask remote system for a list of .log files
                //string cmd = $"ls -1 {remoteLogDir}*.log 2>/dev/null";
                //string result = _hubCom.ExecuteNodeCommand(cmd, device.APAddress, device.Username);
                //result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string[] remoteFiles = _hubCom.GetListOfNodeFiles(remoteLogDir, "log", node.APAddress, node.Username);

                if (remoteFiles.Length == 0)
                {
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"No .log files found on {node.Name}.\n");
                    continue;
                }
                else
                {
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Found {remoteFiles.Length} .log files on {node.Name}.\n");

                    foreach (string remoteFilePath in remoteFiles)
                    {
                        string fileName = Path.GetFileName(remoteFilePath.Trim());
                        string localFilePath = Path.Combine(deviceLocalLogDir, fileName);

                        //OutputText($"[{device.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                        if (_hubCom.CopyNodeToPC(remoteFilePath.Trim(), localFilePath, node.APAddress, false))
                        {
                            var info = new FileInfo(localFilePath);
                            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"[{node.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                            if (!(GetDateFromLogFilename(fileName) == DateTime.Today))
                            {
                                if (_hubCom.DeleteNodeFile(remoteFilePath.Trim(), node.APAddress, false))//, device.Username, false))
                                {
                                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"[{node.Name}] 🗑 Deleted remote log file: {remoteFilePath}\n");
                                }
                                else
                                {
                                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{node.Name}] ⚠ Successfully downloaded, but could not delete remote log file: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                        }
                        else
                        {
                            logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{node.Name}] ⚠ Download reported success but file not found: {localFilePath}\n");
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

                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Checking Captures on {node.Name} ({node.APAddress})...\n");

                // Ask remote system for a list of .png files for each capture type
                foreach (string type in captureTypes)
                {
                    string remoteTypeDir = Path.Combine(remoteCapturesDir, type);
                    string localTypeDir = Path.Combine(deviceLocalCapturesDir, type);
                    if (!Directory.Exists(localTypeDir))
                        Directory.CreateDirectory(localTypeDir);

                    //string cmd = $"ls -1 {remoteTypeDir}/*.png 2>/dev/null";
                    //string result = _hubCom.ExecuteNodeCommand(cmd, device.APAddress, device.Username);
                    //string[] remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] remoteFiles = _hubCom.GetListOfNodeFiles(remoteTypeDir, "log", node.APAddress, node.Username);

                    if (remoteFiles.Length == 0)
                    {
                        logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"No {type} files found on {node.Name}.\n");
                        continue;
                    }
                    else
                    {
                        logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Found {remoteFiles.Length} {type} files on {node.Name}.\n");

                        foreach (string remoteFilePath in remoteFiles)
                        {
                            string fileName = Path.GetFileName(remoteFilePath.Trim());
                            string localFilePath = Path.Combine(localTypeDir, fileName);

                            //OutputText($"[{device.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                            if (_hubCom.CopyNodeToPC(remoteFilePath.Trim(), localFilePath, node.APAddress))
                            {
                                if (_hubCom.DeleteNodeFile(remoteFilePath.Trim(), node.APAddress, false))//, device.Username, false))
                                {
                                    var info = new FileInfo(localFilePath);
                                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"[{node.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                                }
                                else
                                {
                                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{node.Name}] ⚠ Successfully downloaded, but could not delete remote file after download: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                            else
                            {
                                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{node.Name}] ⚠ Download failed: {localFilePath}\n");
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
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Checking reconstructions on {_hub.Name}...\n");

                // Ask remote system for a list of .json files
                //string cmd = $"ls -1 {remoteReconstructionsDir}*.json 2>/dev/null";
                //string result = _hubCom.ExecuteHubCommand(cmd);
                //var remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string[] remoteFiles = _hubCom.GetListOfHubFiles(remoteReconstructionsDir, "json");

                if (remoteFiles.Length == 0)
                {
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"No reconstruction files found on {_hub.Name}.\n");
                }
                else
                {
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Found {remoteFiles.Length} reconstruction files on {_hub.Name}.\n");

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
                                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"[{_hub.Name}] ✔ Successfully downloaded reconstruction file: {remoteFilePath}\n");
                                //OutputText($"[{_hub.Name}] 🗑 Deleted reconstruction file: {remoteFilePath}\n", LogLevel.INFO);
                            }
                            else
                            {
                                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{_hub.Name}] ⚠ Successfully downloaded, but could not delete reconstruction file: {remoteFilePath}\n");
                                hadErrors = true;
                            }
                        }
                        else
                        {
                            logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"[{_hub.Name}] ⚠ Download failed: {localFilePath}\n");
                            hadErrors = true;
                        }
                    }
                }
            }

            return !hadErrors;
        }

        private void BackupBinFiles()
        {
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Backuping up previous bin files");
            _hubCom.DeleteHubFile("/home/camcpp/previous_hub");
            _hubCom.MoveHubFile("/home/camcpp/hub", "/home/camcpp/previous_hub");
            _hubCom.DeleteHubFile("/home/camcpp/previous_node");
            _hubCom.MoveHubFile("/home/camcpp/node", "/home/camcpp/previous_node");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Bin files backed up");
        }
        public void ManualRecompile(bool backupFirst)
        {
            stopMain();

            if (backupFirst)
                BackupBinFiles();

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Ready to recompile manually.  Please Run:\n");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "time make -C /home/camcpp/build\n");
        }
        public void AutoRecompile(bool backupFirst)
        {
            stopMain();

            if (backupFirst)
                BackupBinFiles();

            //currentCluster.ExecuteCommand("g++ -o device -g /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include"); // debug
            //currentCluster.ExecuteHubCommand("g++ -O2 -o device  /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include");    // release

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Recompiling...");
            _hubCom.ExecuteHubCommand("make -C /home/camcpp/build");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Recompilation Complete");
        }
        public void CreateSettingsFiles()
        {
            logger.Log(mLogger.LogLevel.INFO, "Uploader", "Creating new settings files...\n");

            string hubPath = Settings.All.SourceFilesDirectory + "hubSettings.json";
            string backupHubPath = Settings.All.SourceFilesDirectory + "hubSettings_backup.json";
            if (File.Exists(hubPath))
            {
                logger.Log(mLogger.LogLevel.INFO, "Uploader", "Saving new hubSettings_backup.json\n");
                File.Copy(hubPath, backupHubPath, true);
            }
            logger.Log(mLogger.LogLevel.INFO, "Uploader", "Saving new hubSettings.json\n");
            ClusterProfile? profile = Settings.All.ClusterProfiles.FirstOrDefault(p => p.profileName == Settings.All.ClusterProfileToUse);
            if (profile != null)
                Settings.SaveHubSettings(_hub, profile, hubPath);
            else
            {
                logger.Log(mLogger.LogLevel.ERROR, "Uploader", "Could not find correct culster profile.  Check 'ManagerSettings.json'");
                return;
            }

            foreach (Device node in _nodes)
            {
                if (node.ClusterID == _hub.ClusterID)
                {
                    string nodePath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings.json";
                    string backupNodePath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings_backup.json";
                    if (File.Exists(nodePath))
                    {
                        logger.Log(mLogger.LogLevel.INFO, "Uploader", $"Saving new {node.Name}Settings_backup.json\n");
                        File.Copy(nodePath, backupNodePath, true);
                    }

                    logger.Log(mLogger.LogLevel.INFO, "Uploader", $"Saving new {node.Name}Settings.json\n");
                    Settings.SaveNodeSettings(node, Settings.All.ClusterProfiles.FirstOrDefault(p => p.profileName == Settings.All.ClusterProfileToUse), nodePath);
                }
            }
            logger.Log(mLogger.LogLevel.INFO, "Uploader", "Settings files creation complete.\n");
        }
        public void DistributeRuntimeFiles()
        {
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Distributing Runtime files");
            //  Copy Hub's settings file
            string hubSettingsStartingPath = Settings.All.SourceFilesDirectory + "hubSettings.json";
            string hubSettingsEndingPath = "/home/camcpp/src/hubSettings.json";
            _hubCom.CopyPCtoHub(hubSettingsStartingPath, hubSettingsEndingPath, false);

            //  Copy binary and settings file to each Node
            string nodeBinFile = "/home/camcpp/node";
            string nodeSettingsEndingPath = "/home/camcpp/src/nodeSettings.json";
            foreach (Device node in _nodes)
                if (node.isActive)
                {
                    string nodeSettingsStartingPath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings.json";
                    _hubCom.CopyHubToNode(nodeBinFile, nodeBinFile, node.APAddress, node.Username);
                    _hubCom.CopyPCtoNode(nodeSettingsStartingPath, nodeSettingsEndingPath, node.APAddress, false);
                }

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Runtime File Distribution Complete");
        }

        public void startMain()
        {
            _hubCom.ExecuteHubCommand("sudo systemctl start hub.service");
            logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Starting {_hub.Name}:hub.service");
        
            foreach (Device node in _nodes)
            {
                _hubCom.ExecuteNodeCommand($"sudo systemctl start node.service", node.APAddress, node.Username);
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Starting {node.Name}:node.service");
            }
        }
        public void stopMain()
        {
            _hubCom.ExecuteHubCommand("sudo systemctl stop hub.service");
            logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Stopping {_hub.Name}:hub.service");

            foreach (Device node in _nodes)
            {
                _hubCom.ExecuteNodeCommand($"sudo systemctl stop node.service", node.APAddress, node.Username);
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Stopping {node.Name}:node.service");
            }
        }

        public void RebootCluster()
        {
            try
            {
                // Reboot each Node
                foreach (Device node in _nodes)
                {
                    _hubCom.ExecuteHubCommand($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown -r +1\" > /dev/null 2>&1 &");
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{node.Name} at {node.APAddress} is rebooting.");
                }

                _hubCom.ExecuteHubCommand("sudo shutdown -r now");
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{_hub.Name} is rebooting.");

                _hubCom.Disconnect();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error: " + ex.Message);
            }
        }
        public void ShutdownCluster()
        {
            try
            {
                // Shutdown each Node
                foreach (Device node in _nodes)
                {
                    _hubCom.ExecuteHubCommand($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown now\" > /dev/null 2>&1 &");
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{node.Name} at {node.APAddress} is shutting down.");
                }
                    
                _hubCom.ExecuteHubCommand("sudo shutdown now");
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{_hub.Name} is shutting down.");

                _hubCom.Disconnect();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error Message:   " + ex.Message);
                if (ex.InnerException != null)
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Inner Ex:        " + ex.InnerException.Message);
            }
        }
    }
}