using Dplus_Desktop.SettingsManager;
using jCommunicator;
using mLogger;
using Renci.SshNet.Messages;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using static OpenCvSharp.ML.DTrees;



namespace Dplus_Desktop
{
    public partial class Uploader : Form
    {
        Dictionary<string, ClusterManager> clusters;
        ClusterManager currentCluster;

        DateTime LastUploadTime;

        private Logger logger = Logger.Instance;

        public Uploader()
        {
            InitializeComponent();

            AddLogSource("Uploader");
            AddLogSource("ClusterManager");

            logger.LogHeading(LogLevel.INFO, "Uploader", "Uploader Initialing");

            //LastUploadTime = DateTime.MinValue;
            clusters = new Dictionary<string, ClusterManager>();

            foreach (Device hub in Settings.All.Hubs)
            {
                clusters.Add(hub.ClusterID, new ClusterManager(hub, Settings.All.GetNodesByClusterID(hub.ClusterID)));
                Clusters_Box.Items.Add(hub.ClusterID);
            }

            if (Clusters_Box.Items.Count == 0)
            {
                MessageBox.Show("No hubs configured. Please configure settings first.");
                throw new Exception("No hubs configured. Please configure settings first.");
            }
            else
            {
                Clusters_Box.SelectedIndex = 0;
                currentCluster.CheckSSH(true);
            }

            UpdateManagedFilesBoxes();
        }
        private void Uploader_FormClosing(object sender, FormClosingEventArgs e)
        {
            //SaveManagedFiles();

            logger.LogHeading(LogLevel.INFO, "Uploader", "Uploader Exiting");
        }

        private void AddLogSource(string source, Color color = default, bool andModules = true)
        {
            logger.AddSource(source, color, andModules);
            logger.Log(mLogger.LogLevel.INFO, "Uploader", $"Added source '{source}' to _tbSink");
        }
        //private void SaveManagedFiles()
        //{
        //    SaveSourceFiles();
        //    SaveModelFiles();
        //}
        //private void SaveSourceFiles()
        //{
        //    try
        //    {
        //        foreach (ListViewItem item in SourceFiles_Box.Items)
        //        {
        //            if (item.SubItems.Count < 3)
        //                continue;

        //            string fileNameOnly = item.SubItems[0].Text.Trim();  // now just FileName
        //            string lastUpload = item.SubItems[1].Text.Trim();
        //            string lastModified = item.SubItems[2].Text.Trim();

        //            foreach (SourceFile val in Settings.All.SourceFiles)
        //            {
        //                if (val.FileName == fileNameOnly)
        //                {
        //                    if (DateTime.TryParse(lastUpload, out var parsedUpload))
        //                        val.LastUploadTime = parsedUpload;
        //                    // lastModified is calculated dynamically, not persisted to JSON
        //                    break;
        //                }
        //            }
        //        }

        //        Settings.SaveSettings();
        //        logger.Log(LogLevel.INFO, "Uploader", "Source files saved successfully.\n");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error saving file: " + ex.Message);
        //        logger.Log(LogLevel.ERROR, "Uploader", "Error saving file: " + ex.Message + '\n');
        //    }
        //}
        //private void SaveModelFiles()
        //{
        //    try
        //    {
        //        foreach (ListViewItem item in ModelFiles_Box.Items)
        //        {
        //            if (item.SubItems.Count < 2)
        //                continue;

        //            string modelName = item.SubItems[0].Text.Trim();
        //            string modelType = item.SubItems[1].Text.Trim();
        //            // LastModified is calculated dynamically, not persisted

        //            foreach (ModelFile val in Settings.All.Models)
        //            {
        //                if (val.ModelName == modelName && val.ModelType == modelType)
        //                {
        //                    // Currently no LastUploadTime in schema for models
        //                    // If you decide to track uploads later, you'd add it here
        //                    break;
        //                }
        //            }
        //        }

        //        Settings.SaveSettings();
        //        logger.Log(LogLevel.INFO, "Uploader", "Model files saved successfully.\n");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error saving file: " + ex.Message);
        //        logger.Log(LogLevel.ERROR, "Uploader", "Error saving file: " + ex.Message + '\n');
        //    }
        //}

        private void LoadNodes()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Loading nodes for " + Clusters_Box.SelectedItem.ToString() + "\n");
            Device hub = Settings.All.Hubs[Clusters_Box.SelectedIndex];

            Nodes_Box.Items.Clear();
            foreach (Device node in Settings.All.GetNodesByClusterID(hub.ClusterID))
            {
                ListViewItem item = new ListViewItem(node.Name);
                item.SubItems.Add(node.APAddress);
                Nodes_Box.Items.Add(item);
            }

            HighlightNodes();
        }
        private void UpdateManagedFilesBoxes()
        {
            if (Settings.isLoaded == false)
            {
                Upload_Button.Enabled = false;
                RecheckStatus_Button.Enabled = false;
                Reboot_Button.Enabled = false;
                Shutdown_Button.Enabled = false;

                MessageBox.Show("Settings not loaded. Please configure settings first.");
                return;
            }
            else
            {
                Upload_Button.Enabled = true;
                RecheckStatus_Button.Enabled = true;
                Reboot_Button.Enabled = true;
                Shutdown_Button.Enabled = true;

                UpdateSourceFiles_Box();
                UpdateRuntimeFiles_Box();
                UpdateModels_Box();
            }
        }
        private void UpdateSourceFiles_Box()
        {
            SourceFiles_Box.Items.Clear();

            foreach (SourceFile file in Settings.All.SourceFiles)
            {
                // Get field values
                string filePath = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
                string lastUploadedTime = file.LastUploadTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                string lastModifiedTime = file.LastModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

                // Determine color
                Color color = Color.Black;

                try
                {
                    if (file.LastModifiedTime.HasValue)
                    {
                        if (file.LastModifiedTime > file.LastUploadTime)
                            color = Color.Yellow;       // Needs Upload
                        else
                            color = Color.Green;        // Good to go
                    }
                    else
                    {
                        color = Color.Red;              // No last modified (file likley missing)
                    }
                }
                catch
                {
                    color = Color.DarkRed;              // Error
                }

                // Build ListViewItem
                var item = new ListViewItem(file.FileName);
                item.UseItemStyleForSubItems = true;
                item.BackColor = color;
                item.SubItems.Add(lastUploadedTime);
                item.SubItems.Add(lastModifiedTime);
                item.SubItems.Add(file.IsForHub ? "Yes" : "No");
                item.SubItems.Add(file.IsForNode ? "Yes" : "No");
                item.UseItemStyleForSubItems = true;
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                    sub.BackColor = color;

                // Add item to ListView
                SourceFiles_Box.Items.Add(item);
            }
        }
        private void UpdateRuntimeFiles_Box()
        {
            RuntimeFiles_Box.Items.Clear();

            foreach (RuntimeFile file in Settings.All.RuntimeFiles)
            {
                // Get field values
                string lastSourceChange = file.LastSourceChangeTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                string lastCompiledTimeString = file.LastCompliedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                string pushedTimeString = file.LastPushedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

                // Determine color
                Color color = Color.Black;

                if (file.LastSourceChangeTime.HasValue && file.LastCompliedTime.HasValue && file.LastPushedTime.HasValue)
                {
                    if (file.LastSourceChangeTime > file.LastCompliedTime)
                        color = Color.Yellow;       // Needs to be compiled
                    else if (file.LastCompliedTime > file.LastPushedTime)
                        color = Color.Orange;       // Need to be distributed
                    else
                        color = Color.Green;        // Good to go
                }
                else
                {
                    color = Color.Red;              // No last modified (cluster likeley not available)
                }

                // Build ListViewItem
                var item = new ListViewItem(file.FileName);
                item.SubItems.Add(lastSourceChange);
                item.SubItems.Add(lastCompiledTimeString);
                item.SubItems.Add(pushedTimeString);
                item.UseItemStyleForSubItems = true;
                RuntimeFiles_Box.Items.Add(item);
            }
        }
        private void UpdateModels_Box()
        {
            ModelFiles_Box.Items.Clear();

            foreach (ModelFile file in Settings.All.Models)
            {
                // Get field values
                string localPath = Path.Combine(Settings.All.LocalModelsPath, file.ModelType, file.ModelName);
                string lastPushTime = file.LastPushTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                string lastModifiedTime = file.LastModifiedTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";

                // Determine color
                Color color = Color.Black;

                if (file.LastModifiedTime.HasValue && file.LastPushTime.HasValue)
                {
                    if (file.LastModifiedTime > file.LastPushTime)
                        color = Color.Yellow;       // Needs to be compiled
                    else
                        color = Color.Green;        // Good to go
                }
                else
                {
                    color = Color.Red;              // No last modified and/or pushed time (file likeley missing)
                }

                // Create a new ListViewItem with file name, type, last modified
                var item = new ListViewItem(file.ModelName);
                item.SubItems.Add(file.ModelType);
                item.SubItems.Add(lastPushTime);
                item.SubItems.Add(lastModifiedTime);
                item.UseItemStyleForSubItems = true;

                ModelFiles_Box.Items.Add(item);
            }
        }

        private void Reselect_Button_Click(object sender, EventArgs e)
        {
            UpdateManagedFilesBoxes();
        }
        private void HighlightNodes()
        {
            foreach (ListViewItem nodeItem in Nodes_Box.Items)
            {
                Device? nodeDevice = Settings.All.Nodes.Find(d => d.Name == nodeItem.SubItems[0].Text);
                Color color = Color.Black;

                if (nodeDevice != null)
                {
                    ServiceStatus status = currentCluster.CheckDeviceServiceStatus(nodeDevice);
                    switch (status)
                    {
                        case ServiceStatus.Active:
                            color = Color.Green;
                            break;

                        case ServiceStatus.Inactive:
                            color = Color.Yellow;
                            break;

                        case ServiceStatus.Failed:
                            color = Color.Red;
                            break;

                        case ServiceStatus.Activating:
                            color = Color.Green;
                            break;

                        case ServiceStatus.Deactivating:
                            color = Color.Yellow;
                            break;

                        case ServiceStatus.Error:
                            color = Color.DarkRed;
                            break;

                        default:
                            color = Color.Gray;
                            break;
                    }

                    // set color for entire row
                    nodeItem.BackColor = color;
                }
                else
                {
                    color = Color.DarkRed;
                    logger.Log(LogLevel.ERROR, "Uploader", $"Device not found in Settings.All.Nodes for name '{nodeItem.SubItems[0].Text}'.\n");
                }
                
                // set color for entire row
                nodeItem.BackColor = color;
                //foreach (ListViewItem.ListViewSubItem sub in nodeItem.SubItems)
                //{
                //    sub.BackColor = color;
                //}
            }
        }
        private void CheckServiceStatus_Button_Click(object sender, EventArgs e)
        {
            checkServiceStatus(currentCluster);
        }
        private void checkServiceStatus(ClusterManager com)
        {
            string hubServiceName = "hub.service";
            string nodeServiceName = "node.service";
            ServiceStatus hubResult;
                logger.Log(LogLevel.INFO, "Uploader", $"Checking status of {hubServiceName}...\n");

            // Run systemctl command to get status
            try
            {
                hubResult = com.CheckDeviceServiceStatus(com._hub.Name);
                logger.Log(LogLevel.INFO, "Uploader", $"Status of {hubServiceName} on Hub: {hubResult}\n");
                foreach (ListViewItem node in Nodes_Box.Items)
                {
                    Device? nodeDevice = Settings.All.Nodes.Find(d => d.Name == node.SubItems[0].Text);
                    if (nodeDevice != null && nodeDevice.isActive)
                    {
                        ServiceStatus nodeResult = com.CheckDeviceServiceStatus(nodeDevice.Name);
                        logger.Log(LogLevel.INFO, "Uploader", $"Status of {nodeServiceName} on {nodeDevice.Name}: {nodeResult}\n");
                        switch (nodeResult)
                        {
                            case ServiceStatus.Inactive:
                            case ServiceStatus.Deactivating:
                                node.BackColor = Color.Yellow;
                                break;

                            case ServiceStatus.Active:
                            case ServiceStatus.Activating:
                                node.BackColor = Color.Green;
                                break;
                        
                            case ServiceStatus.Failed:
                                node.BackColor = Color.Red;
                                break;

                            case ServiceStatus.Error:
                                node.BackColor = Color.DarkRed;
                                break;
                        }
                    }
                    else
                    {
                        node.BackColor = Color.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"Error checking service status: {ex.Message}\n");
                hubResult = ServiceStatus.Failed;
            }

            switch (hubResult)
            {
                case ServiceStatus.Active:
                    logger.Log(LogLevel.INFO, "Uploader", $"{hubServiceName} is running.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Green;
                    Status_SourceCode_Label.ForeColor = Color.Orange;

                    StartService_Button.Enabled = false;
                    StopService_Button.Enabled = true;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case ServiceStatus.Inactive:
                    logger.Log(LogLevel.WARN, "Uploader", $"{hubServiceName} is stopped.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Orange;
                    Status_SourceCode_Label.ForeColor = Color.Green;

                    StartService_Button.Enabled = true;
                    StopService_Button.Enabled = false;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case ServiceStatus.Failed:
                    logger.Log(LogLevel.ERROR, "Uploader", $"{hubServiceName} has failed.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Red;
                    Status_SourceCode_Label.ForeColor = Color.Green;

                    StartService_Button.Enabled = true;
                    StopService_Button.Enabled = false;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case ServiceStatus.Activating:
                    logger.Log(LogLevel.INFO, "Uploader", $"{hubServiceName} is starting up.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Green;
                    Status_SourceCode_Label.ForeColor = Color.Orange;

                    StartService_Button.Enabled = false;
                    StopService_Button.Enabled = true;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case ServiceStatus.Deactivating:
                    logger.Log(LogLevel.INFO, "Uploader", $"{hubServiceName} is stopping.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Red;
                    Status_SourceCode_Label.ForeColor = Color.Green;

                    StartService_Button.Enabled = true;
                    StopService_Button.Enabled = false;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                default:
                    logger.Log(LogLevel.WARN, "Uploader", $"{hubServiceName} is in an unknown state: {hubResult}\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Red;
                    Status_SourceCode_Label.ForeColor = Color.Green;

                    StartService_Button.Enabled = false;
                    StopService_Button.Enabled = false;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;
            }
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            currentCluster.CheckSSH(true);
            UpdateManagedFilesBoxes();
        }

        private void setGUISSHStatus(bool isConnected)
        {
            Color color = isConnected ? Color.Green : Color.Red;
            Status_SSHSCP_Label.ForeColor = color;
            Status_RaspberryPi_Label.ForeColor = color;
            Status_Connction_Label.ForeColor = color;
            Status_SourceCode_Label.ForeColor = color;
            if (Status_SystemCTLService_Label.ForeColor == SystemColors.ControlText)
            {
                checkServiceStatus(clusters[Clusters_Box.Text]);
            }
            RecheckStatus_Button.Enabled = isConnected;
            Reboot_Button.Enabled = isConnected;
            Shutdown_Button.Enabled = isConnected;
            Upload_Button.Enabled = isConnected;
            ManualRecompile_Button.Enabled = isConnected;
            if (!isConnected)
            {
                StartService_Button.Enabled = false;
                StopService_Button.Enabled = false;
                CheckServiceStatus_Button.Enabled = false;
            }
        }

        private void Upload_Button_Click(object sender, EventArgs e)
        {
            UpdateManagedFilesBoxes();

            try
            {
                logger.Log(LogLevel.INFO, "Uploader", "Starting Upload Process.\n");

                // Ensure connection
                currentCluster.CheckSSH(true);

                // Upload files
                currentCluster.UploadFiles();


                logger.Log(LogLevel.INFO, "Uploader", "Saving Data\n");
                //SaveManagedFiles();
                logger.Log(LogLevel.INFO, "Uploader", "Upload process completed.\n");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error: " + ex.Message + '\n');
                logger.Log(LogLevel.INFO, "Uploader", "Upload Failed.\n");
            }
        }

        private void DownloadFiles_Button_Click(object sender, EventArgs e)
        {
            currentCluster.DownloadFiles();
        }

        private void ManualRecompile_Button_Click(object sender, EventArgs e)
        {
            currentCluster.ManualRecompile();
            //currentCluster.testFileMethods(Settings.All.SourceFilesDirectory + "file.txt", Settings.All.Nodes[0].APAddress, Settings.All.Nodes[0].Username);
        }
        private void AutoRecompile_Button1_Click(object sender, EventArgs e)
        {
            currentCluster.AutoRecompile();

            currentCluster.DistributeRuntimeFiles();
        }

        private void DistributeRuntimeFiles_Button_Click(object sender, EventArgs e)
        {
            currentCluster.DistributeRuntimeFiles();
        }
        
        private void RunMain_Button_Click(object sender, EventArgs e)
        {
            currentCluster.startMain();
        }

        private void StopService_Button_Click(object sender, EventArgs e)
        {
            currentCluster.stopMain();

            currentCluster.DownloadFiles();
        }

        private void Reboot_Button_Click(object sender, EventArgs e)
        {
            currentCluster.RebootCluster();
        }
        private void Shutdown_Button_Click(object sender, EventArgs e)
        {
            currentCluster.ShutdownCluster();
        }

        private void Clusters_Box_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentCluster = clusters[Clusters_Box.Text];

            LoadNodes();
        }

        private void CreateSettingsfiles_Button_Click(object sender, EventArgs e)
        {
            logger.Log(LogLevel.INFO, "Uploader", "Creating new settings files...\n");
            string hubPath = Settings.All.SourceFilesDirectory + "hubSettings.json";
            string backupHubPath = Settings.All.SourceFilesDirectory + "hubSettings_backup.json";
            if (File.Exists(hubPath))
            {
                logger.Log(LogLevel.INFO, "Uploader", "Saving new hubSettings_backup.json\n");
                File.Copy(hubPath, backupHubPath, true);
            }
            logger.Log(LogLevel.INFO, "Uploader", "Saving new hubSettings.json\n");
            Settings.SaveHubSettings(Settings.All.Hubs[Clusters_Box.SelectedIndex], Settings.All.ClusterProfiles.FirstOrDefault(p => p.profileName == Settings.All.ClusterProfileToUse), hubPath);


            foreach (Device node in Settings.All.Nodes)
            {
                if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID)
                {
                    string nodePath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings.json";
                    string backupNodePath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings_backup.json";
                    if (File.Exists(nodePath))
                    {
                        logger.Log(LogLevel.INFO, "Uploader", $"Saving new {node.Name}Settings_backup.json\n");
                        File.Copy(nodePath, backupNodePath, true);
                    }

                    logger.Log(LogLevel.INFO, "Uploader", $"Saving new {node.Name}Settings.json\n");
                    Settings.SaveNodeSettings(node, Settings.All.ClusterProfiles.FirstOrDefault(p => p.profileName == Settings.All.ClusterProfileToUse), nodePath);
                }
            }
            logger.Log(LogLevel.INFO, "Uploader", "Settings files creation complete.\n");


        }

    }
}
