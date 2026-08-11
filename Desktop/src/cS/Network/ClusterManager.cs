using Dplus_Desktop.SettingsManager;
using jCommunicator;
using mLogger;
using System.Text.RegularExpressions;

namespace Dplus_Desktop
{
    // Manages one Dplus_Embedded cluster (1 Hub and many Nodes) using a jCommunicator.Communicator.  Provides Dplus specific functionality.
    public class ClusterManager
    {
        public event EventHandler<EventArgs>? Connected;
        public event EventHandler<EventArgs>? Disconnected;

        // The Hub for this cluster
        public Device _hub { get; }

        //The Communicator Object for this cluster
        Communicator _hubCom;

        // A list of node objects representing each node in the cluster
        List<Device> _nodes { get; }

        // An mLogger object to send logging output to
        Logger logger = Logger.Instance;

        public ClusterManager(Device hub, List<Device> nodes)
        {
            logger.AddSource("ClusterManager");

            _hub = hub;
            _hubCom = new Communicator(hub.IPAddress, hub.Username, hub.Password);

            _nodes = new List<Device>();
            if ((nodes != null) && (nodes.Count > 0))
                foreach (Device node in nodes)
                {
                    _nodes.Add(node);
                }

            _hubCom.Connected += hubCom_Connected;
            _hubCom.Disconnected += hubCom_Disconnected;
        }

        private void hubCom_Disconnected(object? sender, EventArgs e)
        {
            Disconnected?.Invoke(sender, e);
        }
        private void hubCom_Connected(object? sender, EventArgs e)
        {
            Connected?.Invoke(this, e);
        }

        public async Task<bool> ConnectAsync()
        {
            await _hubCom.ConnectAsync();

            if ((_hubCom.IsConnected))
                foreach (Device node in _nodes)
                {
                    if (node.isActive)
                    {
                        await _hubCom.AddNodeTunnelAsync(node.APAddress, node.Username, node.Password, true);
                    }
                }

            await LoadManagedFiles();
            return await CheckSSH(true);
        }
        public async Task<ClusterStatus> CheckSystem()
        {
            bool isConnected = await CheckSSH();

            if (isConnected)
            {
                ServiceStatus hubValue = await CheckDeviceServiceStatus(_hub);
                Dictionary<string, ServiceStatus> nodeValues = new Dictionary<string, ServiceStatus>();
                foreach (Device node in _nodes)
                {
                    if (node.isActive)
                        nodeValues.Add(node.Name, await CheckDeviceServiceStatus(node));
                }
                return new ClusterStatus(true,
                                         nodeValues.Count,
                                         hubValue,
                                         nodeValues);
            }
            else
            {
                Dictionary<string, ServiceStatus> nodeValues = new Dictionary<string, ServiceStatus>();
                foreach (Device node in _nodes)
                {
                    nodeValues.Add(node.Name, ServiceStatus.Failed);
                }
                return new ClusterStatus(false,
                                         _nodes.Count,
                                         ServiceStatus.Failed,
                                         nodeValues);
            }
        }
        public async Task<bool> CheckSSH(bool verbose = false)
        {
            string host = _hubCom._host;
            string username = _hubCom._username;

            if (verbose)
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Checking SSH connection to device {host} as {username}...");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isCnctd = false;

            try
            {
                if (await _hubCom.ConnectAsync())
                {
                    if (verbose)
                        logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Successfully connected to {host} in {sw.ElapsedMilliseconds} ms.");
                    isCnctd = true;
                }
                else
                {
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Failed to connect to {host} without error.");
                }
            }
            catch (Renci.SshNet.Common.SshAuthenticationException authEx)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Authentication failed for {username}@{host}: {authEx.Message}");
            }
            catch (Renci.SshNet.Common.SshConnectionException connEx)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Connection error to {host}: {connEx.Message}");
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Socket error while connecting to {host}: {sockEx.Message}");
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Unexpected error for {host}: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                sw.Stop();

                if (verbose)
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Total connection attempt time for {host}: {sw.ElapsedMilliseconds} ms.");
            }

            return isCnctd;
        }
        public async Task<ServiceStatus> CheckDeviceServiceStatus(string deviceName)
        {
            Device? device = Settings.All.GetDeviceByName(deviceName);
            if (device == null)
                return ServiceStatus.Error; 
            return await CheckDeviceServiceStatus(device);
        }
        public async Task<ServiceStatus> CheckDeviceServiceStatus(Device device)
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
                    if (!await _hubCom.PingNodeAsync(device.APAddress))
                        return ServiceStatus.Error;

                string result = "";
                try
                {
                    if (isHub)
                        result = await _hubCom.ExecuteHubCommandAsync($"systemctl is-active {serviceName}");
                    else
                        result = await _hubCom.ExecuteNodeCommandAsync($"systemctl is-active {serviceName}", device.APAddress, device.Username);
                }
                catch (Exception ex)
                {
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Error checking service status on device '{device.Name}' ({device.APAddress}): {ex.Message}");
                    if (ex.InnerException != null)
                        logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Inner exception: {ex.InnerException.Message}");

                    result = "failed";
                }

                switch (result.Trim())
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
                        logger.Log(mLogger.LogLevel.WARN, "ClusterManager", $"Unknown service state '{result}' for device '{device.Name}'.");
                        return ServiceStatus.Error;
                
                }
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Unhandled error while processing device '{device.Name}': {ex.Message}");
                if (ex.InnerException != null)
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Inner exception: {ex.InnerException.Message}");
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Stack Trace:{ex.StackTrace}");
                return ServiceStatus.Error;
            }
        }

        private async Task LoadManagedFiles()
        {
            await LoadSourceFiles();
            await LoadRuntimeFiles();
            await LoadModelFiles();

            Settings.SaveSettings();
        }
        private async Task LoadSourceFiles()
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
                    if (await _hubCom.HubFileExists(remotePath))
                        file.LastUploadTime = await _hubCom.HubFileLastModified(remotePath);
                    else
                        file.LastUploadTime = DateTime.MinValue;
                }

                if (File.Exists(filePath))
                    file.LastModifiedTime = File.GetLastWriteTime(filePath);
                else
                    file.LastUploadTime = DateTime.MinValue;
            }
        }
        private async Task LoadRuntimeFiles()
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
                            file.LastPushedTime = await _hubCom.HubFileLastModified("/home/camcpp/src/hubSettings.json");
                        }
                        else if (file.FileName == "nodeSettings.json")
                        {
                            file.LastSourceChangeTime = File.GetLastWriteTime("C:\\Users\\jerem\\OneDrive\\Documents\\Projects\\Programming\\apps\\Dplus\\Desktop\\managerSettings.json");
                            file.LastCompliedTime = File.GetLastWriteTime(Settings.All.SourceFilesDirectory + "Node1Settings.json");
                            file.LastPushedTime = await _hubCom.NodeFileLastModified("/home/camcpp/src/nodeSettings.json", _nodes.First().APAddress);
                        }
                    }
                    else
                    {
                        //bin file

                        if (!file.IsForNode)
                        {
                            file.LastCompliedTime = await _hubCom.HubFileLastModified("/home/camcpp/hub");
                            file.LastPushedTime = await _hubCom.HubFileLastModified("/home/camcpp/hub");

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
                            file.LastCompliedTime = await _hubCom.HubFileLastModified("/home/camcpp/node");
                            file.LastPushedTime = await _hubCom.NodeFileLastModified("/home/camcpp/node", _nodes.First().APAddress);

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
        private async Task LoadModelFiles()
        {            
            foreach (ModelFile file in Settings.All.Models)
            {
                file.LastModifiedTime = File.GetLastWriteTime(Path.Combine(Settings.All.LocalModelsPath, file.ModelName));
                if (_hubCom.IsConnected)
                {
                    file.LastPushTime = await _hubCom.NodeFileLastModified(Path.Combine(Settings.All.RemoteModelsPath, file.ModelType, file.ModelName).Replace("\\", "/"), "10.0.0.11");
                }
            }
        }

        public async Task UploadFiles()
        {
            await UploadSourceFiles();
            await UploadModelFiles();

            await LoadManagedFiles();
        }
        private async Task UploadSourceFiles()
        {
            //List<SourceFile> itemsToProcess = Settings.All.SourceFiles;

            foreach (SourceFile file in Settings.All.SourceFiles)
            {
                if (file.LastModifiedTime > file.LastUploadTime)
                {
                    string localFile = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
                    string remoteHubFile = Path.Combine(Settings.All.UploadDirectory, "hub/", file.FileName).Replace("\\", "/"); // normalize to Linux paths
                    string remoteNodeFile = Path.Combine(Settings.All.UploadDirectory, "node/", file.FileName).Replace("\\", "/"); // normalize to Linux paths

                    if (file.LastModifiedTime > file.LastUploadTime)
                    {
                        if (file.IsForHub)
                            await _hubCom.PCtoHubAsync(new ClusterFileIOCommand(localFile, remoteHubFile, ClusterFileIOCommandType.Upload));
                        
                    
                        if (file.IsForNode)
                            await _hubCom.PCtoHubAsync(new ClusterFileIOCommand(localFile, remoteNodeFile, ClusterFileIOCommandType.Upload));
                        
                    }

                    // Update settings
                    file.LastUploadTime = DateTime.Now; ; // LastUploadTime column
                }
            }

            Settings.SaveSettings();
        }
        private async Task UploadModelFiles()
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
                        await _hubCom.DeleteHubFile(remoteFile);
                        await _hubCom.PCtoHubAsync(new ClusterFileIOCommand(localFile, remoteFile, ClusterFileIOCommandType.Upload));
                        foreach (Device node in _nodes)
                        {
                            await _hubCom.DeleteNodeFile(remoteFile, node.APAddress);
                            await _hubCom.CopyHubToNode(remoteFile, remoteFile, node.APAddress, node.Username);
                        }

                        file.LastPushTime = DateTime.Now;
                        logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Uploaded '{localFile}' → '{remoteFile}' to all nodes");
                    }
                    catch (Exception ex)
                    {
                        logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", $"Upload failed for {file.ModelName} on device {_hubCom._host}: {ex.Message}");
                    }

                }            
            }

            Settings.SaveSettings();
        }

        public async Task DownloadFiles()
        {
            List<Task<bool>> downloadTasks = new List<Task<bool>>();

            downloadTasks.Add(DownloadHubFiles());
            foreach (Device node in _nodes) 
                if (node.isActive)
                    downloadTasks.Add(DownloadNodeFiles(node));

            await Task.WhenAll(downloadTasks);

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Download process completed.");
        }
        private async Task<bool> DownloadCalibration()
        {
            string HubSettingsStartingPath = "/home/camcpp/src/hubSettings.json";
            string baseLocalLogDir = Settings.All.LocalLogPath;
            string HubCalibrationSettingsFile = Path.Combine(baseLocalLogDir, _hub.Name, "hubCalibrationSettings.json");
            ClusterFileIOCommand command = new ClusterFileIOCommand(HubSettingsStartingPath.Trim(), HubCalibrationSettingsFile, ClusterFileIOCommandType.Download);

            if ((await _hubCom.PCtoHubAsync(command)).MainProcedureSucceeded)
            {
                int newDataPoints = await Settings.MergeNewCalibrationData(HubCalibrationSettingsFile);
                if (newDataPoints > 0)
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager_DownloadCalibration", $"Calibration checked.  {newDataPoints} new data points integrated.");
                else
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager_DownloadCalibration", "Calibration checked.  No new data.");

                return true;
            }
            else
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager_DownloadCalibration", $"[{_hub.Name}] ⚠ Download failed: {HubCalibrationSettingsFile}");
                return false;
            }
        }
        private async Task<bool> DownloadHubFiles()
        {
            //List<Task<bool>> hubTasks = new List<Task<bool>>();
            List<bool> hubResults = new List<bool>();

            // Logs
            string remoteLogDir = Settings.All.RemoteLogPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;

            string baseLocalHubLogDir = Path.Combine(baseLocalLogDir, _hub.Name, "Logs");
            //hubTasks.Add(RetrieveHubDirectory(baseLocalHubLogDir, remoteLogDir, "log"));
            hubResults.Add(await RetrieveHubDirectory(baseLocalHubLogDir, remoteLogDir, "log"));

            // Reconstructions
            string remoteReconstructionsDir = Settings.All.RemoteReconstructionsPath;
            string baseLocalReconstructionDir = Settings.All.LocalLogPath;
            string localHubReconstructionsDir = Path.Combine(baseLocalReconstructionDir, _hub.Name, "Reconstructions");
            if (!Directory.Exists(localHubReconstructionsDir))
                Directory.CreateDirectory(localHubReconstructionsDir);
            //hubTasks.Add(RetrieveHubDirectory(localHubReconstructionsDir, remoteReconstructionsDir, "json"));
            hubResults.Add(await RetrieveHubDirectory(localHubReconstructionsDir, remoteReconstructionsDir, "json"));

            // Calibration
            //hubTasks.Add(DownloadCalibration());
            hubResults.Add(await DownloadCalibration());

            // Wrap-up
            //await Task.WhenAll(hubTasks);

            foreach (bool task in hubResults)
            {
                if (!task)
                    return false;
            }
            return true;
        }

        private async Task<bool> DownloadNodeFiles(Device node)
        {
            //List<Task<bool>> nodeResults = new List<Task<bool>>();
            List<bool> nodeResults = new List<bool>();

            string remoteLogDir = Settings.All.RemoteLogPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;
            
            // Logs
            string deviceLocalLogDir = Path.Combine(baseLocalLogDir, node.Name, "Logs");
            if (!Directory.Exists(deviceLocalLogDir))
                Directory.CreateDirectory(deviceLocalLogDir);
            //nodeResults.Add(RetrieveNodeDirectory(deviceLocalLogDir, remoteLogDir, node, "log"));
            nodeResults.Add(await RetrieveNodeDirectory(deviceLocalLogDir, remoteLogDir, node, "log"));

            // Captures
            string[] captureTypes = new string[] { "Captures", "Charuco", "Chessboard", "Face", "Motion", "Preprocessed", "Startup", "YoloObject", "YoloPose" };
            string remoteCapturesDir = Settings.All.RemoteCapturesPath;
            string baseLocalCapturesDir = Settings.All.LocalLogPath;

            // Create per-device local log folder
            string deviceLocalCapturesDir = Path.Combine(baseLocalCapturesDir, node.Name);

            // Ask remote system for a list of .png files for each capture type
            foreach (string type in captureTypes)
            {
                string remoteTypeDir = Path.Combine(remoteCapturesDir, type);
                string localTypeDir = Path.Combine(deviceLocalCapturesDir, type);
                if (!Directory.Exists(localTypeDir))
                    Directory.CreateDirectory(localTypeDir);

                List<LinuxFileInfo> remoteFiles = await _hubCom.GetListOfNodeFiles(remoteTypeDir, "png", node.APAddress, node.Username);
                //nodeResults.Add(RetrieveNodeDirectory(localTypeDir, remoteTypeDir, node, "png"));
                nodeResults.Add(await RetrieveNodeDirectory(localTypeDir, remoteTypeDir, node, "png"));
            }

            // Wrap-up
            //await Task.WhenAll(nodeResults);

            foreach (bool task in nodeResults)
            {
                if (!task)
                    return false;
            }
            return true;
        }
        private async Task<bool> DownloadLogs()
        {
            string remoteLogDir = Settings.All.RemoteLogPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;

            string baseLocalHubLogDir = Path.Combine(baseLocalLogDir, _hub.Name, "Logs");

            List<Task<bool>> logTasks = new List<Task<bool>>();
            // Hub logs
            logTasks.Add(RetrieveHubDirectory(baseLocalHubLogDir, remoteLogDir, "log"));

            foreach (Device node in _nodes)
            {
                // Create per-device local log folder
                string deviceLocalLogDir = Path.Combine(baseLocalLogDir, node.Name, "Logs");
                if (!Directory.Exists(deviceLocalLogDir))
                    Directory.CreateDirectory(deviceLocalLogDir);

                logTasks.Add(RetrieveNodeDirectory(deviceLocalLogDir, remoteLogDir, node, "log"));
            }

            await Task.WhenAll(logTasks);

            foreach (Task<bool> task in logTasks) {
                if (!await task)
                    return false;
            }

            return true;
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
                return DateTime.MinValue;

            if (DateTime.TryParseExact(match.Value, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime logDate))
                return logDate;

            return DateTime.MinValue;
        }
        private async Task<bool> RetrieveHubDirectory(string baseLocalDir, string remoteDir, string fileExtension)
        {
            bool hadErrors = false;
            List<LinuxFileInfo> remoteFiles = await _hubCom.GetListOfHubFiles(remoteDir, fileExtension);

            if (remoteFiles.Count == 0)
            {
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"No .{fileExtension} files found on {_hub.Name}.");
            }
            else
            {
                if (!Directory.Exists(baseLocalDir))
                    Directory.CreateDirectory(baseLocalDir);

                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Found {remoteFiles.Count} .{fileExtension} files on {_hub.Name}.");
                
                List<ClusterFileIOCommand> commands = new List<ClusterFileIOCommand>();
                foreach (LinuxFileInfo file in remoteFiles)
                {
                    string localFile = baseLocalDir + '\\' + file.Name.Split('/').Last();
                    if (file.Name.EndsWith(_hub.Name + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log"))
                        commands.Add(new ClusterFileIOCommand(file.Name, localFile, ClusterFileIOCommandType.Download));
                    else
                        commands.Add(new ClusterFileIOCommand(file.Name, localFile, ClusterFileIOCommandType.Download, false, false, true, false));
                }

                await _hubCom.PCtoHubAsync(commands);
            }

            return hadErrors;
            
        }
        private async Task<bool> RetrieveNodeDirectory(string baseLocalDir, string remoteDir, Device node, string fileExtension)
        {
            bool hadErrors = false;
            List<LinuxFileInfo> remoteFiles = await _hubCom.GetListOfNodeFiles(remoteDir, fileExtension, node.APAddress, node.Username);

            if (remoteFiles.Count == 0)
            {
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"No .{fileExtension} files found on {node.Name}.");
            }
            else
            {
                if (!Directory.Exists(baseLocalDir))
                    Directory.CreateDirectory(baseLocalDir);

                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Found {remoteFiles.Count} .{fileExtension} files on {node.Name}.");

                List<ClusterFileIOCommand> commands = new List<ClusterFileIOCommand>();
                foreach (LinuxFileInfo file in remoteFiles)
                {
                    string localFile = baseLocalDir + '\\' + file.Name.Split('/').Last();
                    if (file.Name.EndsWith(node.Name + "_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log"))
                        commands.Add(new ClusterFileIOCommand(file.Name, localFile, ClusterFileIOCommandType.Download));
                    else
                        commands.Add(new ClusterFileIOCommand(file.Name, localFile, ClusterFileIOCommandType.Download, false, false, true, false));
                }
                await _hubCom.PCtoNodeAsync(commands, node.APAddress);
            }

            return hadErrors;

        }

        private async Task BackupBinFiles()
        {
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Backuping up previous bin files");
            await _hubCom.DeleteHubFile("/home/camcpp/previous_hub");
            await _hubCom.MoveHubFile("/home/camcpp/hub", "/home/camcpp/previous_hub");
            await _hubCom.DeleteHubFile("/home/camcpp/previous_node");
            await _hubCom.MoveHubFile("/home/camcpp/node", "/home/camcpp/previous_node");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Bin files backed up");
        }
        public async Task ManualRecompile(bool backupFirst)
        {
            await stopMain();

            if (backupFirst)
                await BackupBinFiles();

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Ready to recompile manually.  Please Run:");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "time make -C /home/camcpp/build");
        }
        public async Task AutoRecompile(bool backupFirst)
        {
            await stopMain();

            if (backupFirst)
                await BackupBinFiles();

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Recompiling...");
            await _hubCom.ExecuteHubCommandAsync("make -C /home/camcpp/build");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Recompilation Complete");
        }
        public void CreateSettingsFiles()
        {
            logger.Log(mLogger.LogLevel.INFO, "Uploader", "Creating new settings files...");

            string hubPath = Settings.All.SourceFilesDirectory + "hubSettings.json";
            string backupHubPath = Settings.All.SourceFilesDirectory + "hubSettings_backup.json";
            if (File.Exists(hubPath))
            {
                logger.Log(mLogger.LogLevel.INFO, "Uploader", "Saving new hubSettings_backup.json");
                File.Copy(hubPath, backupHubPath, true);
            }
            logger.Log(mLogger.LogLevel.INFO, "Uploader", "Saving new hubSettings.json");
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
                        logger.Log(mLogger.LogLevel.INFO, "Uploader", $"Saving new {node.Name}Settings_backup.json");
                        File.Copy(nodePath, backupNodePath, true);
                    }

                    logger.Log(mLogger.LogLevel.INFO, "Uploader", $"Saving new {node.Name}Settings.json");
                    Settings.SaveNodeSettings(node, Settings.All.ClusterProfiles.FirstOrDefault(p => p.profileName == Settings.All.ClusterProfileToUse), nodePath);
                }
            }
            logger.Log(mLogger.LogLevel.INFO, "Uploader", "Settings files creation complete.");
        }
        public async Task DistributeRuntimeFiles()  // Needs more asyncing
        {
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Distributing Runtime files");
            //  Copy Hub's settings file
            string hubSettingsStartingPath = Settings.All.SourceFilesDirectory + "hubSettings.json";
            string hubSettingsEndingPath = "/home/camcpp/src/hubSettings.json";

            await _hubCom.PCtoHubAsync(new ClusterFileIOCommand(hubSettingsEndingPath, hubSettingsStartingPath, ClusterFileIOCommandType.Upload));

            //  Copy binary and settings file to each Node
            string nodeBinFile = "/home/camcpp/node";
            string nodeSettingsEndingPath = "/home/camcpp/src/nodeSettings.json";
            foreach (Device node in _nodes)
                if (node.isActive)
                {
                    string nodeSettingsStartingPath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings.json";
                    await _hubCom.CopyHubToNode(nodeBinFile, nodeBinFile, node.APAddress, node.Username);
                    await _hubCom.PCtoNodeAsync(new ClusterFileIOCommand(nodeSettingsEndingPath, nodeSettingsStartingPath, ClusterFileIOCommandType.Upload), node.APAddress, false);
                }

            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", "Runtime File Distribution Complete");
        }

        public async Task startMain()
        {
            await _hubCom.ExecuteHubCommandAsync("sudo systemctl start hub.service");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Starting service daemon on {_hub.Name}");
        
            foreach (Device node in _nodes)
            {
                await _hubCom.ExecuteNodeCommandAsync($"sudo systemctl start node.service", node.APAddress, node.Username);
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Starting service daemon on {node.Name}");
            }
        }
        public async Task stopMain()
        {
            await _hubCom.ExecuteHubCommandAsync("sudo systemctl stop hub.service");
            logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Stopping service daemon on {_hub.Name}");

            foreach (Device node in _nodes)
            {
                await _hubCom.ExecuteNodeCommandAsync($"sudo systemctl stop node.service", node.APAddress, node.Username);
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"Stopping service daemon on {node.Name}");
            }
        }

        public async Task RebootCluster()
        {
            try
            {
                // Reboot each Node
                foreach (Device node in _nodes)
                {
                    await _hubCom.ExecuteHubCommandAsync($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown -r +1\" > /dev/null 2>&1 &");
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{node.Name} at {node.APAddress} is rebooting.");
                }

                await _hubCom.ExecuteHubCommandAsync("sudo shutdown -r now");
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{_hub.Name} is rebooting.");

                await _hubCom.DisconnectAsync();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error: " + ex.Message);
            }
        }
        public async Task ShutdownCluster()
        {
            try
            {
                // Shutdown each Node
                foreach (Device node in _nodes)
                {
                    await _hubCom.ExecuteHubCommandAsync($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown now\" > /dev/null 2>&1 &");
                    logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{node.Name} at {node.APAddress} is shutting down.");
                }

                await _hubCom.ExecuteHubCommandAsync("sudo shutdown now");
                logger.Log(mLogger.LogLevel.INFO, "ClusterManager", $"{_hub.Name} is shutting down.");

                await _hubCom.DisconnectAsync();
            }
            catch (Exception ex)
            {
                logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Error Message:   " + ex.Message);
                if (ex.InnerException != null)
                    logger.Log(mLogger.LogLevel.ERROR, "ClusterManager", "Inner Ex:        " + ex.InnerException.Message);
            }
        }

        public async Task TestIsConnected()
        {
            for (int i = 0; i < 10; i++)
            {
                if ((await CheckSystem()).SSHConnected) //slow (150 ms?)
                    logger.Log(mLogger.LogLevel.DEBUG, "ClusterManager_IsConnected", $"Is Connected {i}");
                else
                    logger.Log(mLogger.LogLevel.DEBUG, "ClusterManager_IsConnected", $"Is not Connected {i}");
            }
        }
    }

}