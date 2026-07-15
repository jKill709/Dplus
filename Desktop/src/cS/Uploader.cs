using Renci.SshNet.Messages;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using mLogger;


namespace Dplus_Desktop
{
    public partial class Uploader : Form
    {
        Dictionary<string, mCommunicator> hubComs;
        mCommunicator currentHub;

        DateTime LastUploadTime;

        private Logger logger = Logger.Instance;
        private RichTextBoxSink _sink;

        public Uploader()
        {
            InitializeComponent();
            
            logger.AddSink(_sink);

            logger.LogHeading(LogLevel.INFO, "Uploader", "Uploader Initialing");

            LastUploadTime = DateTime.MinValue;
            hubComs = new Dictionary<string, mCommunicator>();

            foreach (Device hub in Settings.All.Hubs)
            {
                hubComs.Add(hub.ClusterID, new mCommunicator(hub.IPAddress, hub.Username, hub.Password));
                Clusters_Box.Items.Add(hub.ClusterID);
            }

            if (Clusters_Box.Items.Count == 0)
            {
                MessageBox.Show("No hubs configured. Please configure settings first.");
                throw new Exception("No hubs configured. Please configure settings first.");
            }

            // Load Nodes
            Clusters_Box.SelectedIndex = 0;

            foreach (Device node in Settings.All.Nodes)
            {
                if (node.isActive)
                {
                    hubComs[node.ClusterID].AddNodeSFTP(node.APAddress, node.Username);
                }
            }

            LoadManagedFiles();
            checkSSHDevice(currentHub, true);
        }
        private void Uploader_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveManagedFiles();

            logger.LogHeading(LogLevel.INFO, "Uploader", "Uploader Exiting");

            logger.RemoveSink(_sink);
        }

        private void SaveManagedFiles()
        {
            SaveSourceFiles();
            SaveModelFiles();
        }
        private void SaveSourceFiles()
        {
            try
            {
                foreach (ListViewItem item in SourceFiles_Box.Items)
                {
                    if (item.SubItems.Count < 3)
                        continue;

                    string fileNameOnly = item.SubItems[0].Text.Trim();  // now just FileName
                    string lastUpload = item.SubItems[1].Text.Trim();
                    string lastModified = item.SubItems[2].Text.Trim();

                    foreach (SourceFile val in Settings.All.SourceFiles)
                    {
                        if (val.FileName == fileNameOnly)
                        {
                            if (DateTime.TryParse(lastUpload, out var parsedUpload))
                                val.LastUploadTime = parsedUpload;
                            // lastModified is calculated dynamically, not persisted to JSON
                            break;
                        }
                    }
                }

                Settings.SaveSettings();
                logger.Log(LogLevel.INFO, "Uploader", "Source files saved successfully.\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving file: " + ex.Message);
                logger.Log(LogLevel.ERROR, "Uploader", "Error saving file: " + ex.Message + '\n');
            }
        }
        private void SaveModelFiles()
        {
            try
            {
                foreach (ListViewItem item in ModelFiles_Box.Items)
                {
                    if (item.SubItems.Count < 2)
                        continue;

                    string modelName = item.SubItems[0].Text.Trim();
                    string modelType = item.SubItems[1].Text.Trim();
                    // LastModified is calculated dynamically, not persisted

                    foreach (ModelFile val in Settings.All.Models)
                    {
                        if (val.ModelName == modelName && val.ModelType == modelType)
                        {
                            // Currently no LastUploadTime in schema for models
                            // If you decide to track uploads later, you'd add it here
                            break;
                        }
                    }
                }

                Settings.SaveSettings();
                logger.Log(LogLevel.INFO, "Uploader", "Model files saved successfully.\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving model: " + ex.Message);
                logger.Log(LogLevel.ERROR, "Uploader", "Error saving model: " + ex.Message + '\n');
            }
        }

        private void LoadNodes()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Loading nodes for " + Clusters_Box.SelectedItem.ToString() + "\n");
            Device hub = Settings.All.Hubs[Clusters_Box.SelectedIndex];

            Nodes_Box.Items.Clear();
            foreach (Device node in Settings.All.Nodes.Where(d => d.ClusterID == hub.ClusterID))
            {
                ListViewItem item = new ListViewItem(node.Name);
                item.SubItems.Add(node.APAddress);
                Nodes_Box.Items.Add(item);
            }

            HighlightNodes();
        }
        private void LoadManagedFiles()
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

                LoadSourceFiles();
                LoadRuntimeFiles();
                LoadModelFiles();
            }
        }
        private void LoadSourceFiles()
        {
            SourceFiles_Box.Items.Clear();

            foreach (SourceFile file in Settings.All.SourceFiles)
            {
                string filePath = Path.Combine(Settings.All.SourceFilesDirectory, file.FileName);
                string lastUploadedTime;

                string remotePath;
                if (file.IsForHub)
                    remotePath = Path.Combine(Settings.All.UploadDirectory, "hub/", file.FileName).Replace("\\", "/");
                else
                    remotePath = Path.Combine(Settings.All.UploadDirectory, "node/", file.FileName).Replace("\\", "/");

                if (currentHub.HubFileExists(remotePath))
                    lastUploadedTime = currentHub.HubFileLastModified(remotePath).ToString("yyyy-MM-dd HH:mm:ss");
                else
                    lastUploadedTime = "N/A";

                string lastModifiedTime = "N/A";
                if (File.Exists(filePath))
                {
                    DateTime modifiedTime = File.GetLastWriteTime(filePath);
                    lastModifiedTime = modifiedTime.ToString("yyyy-MM-dd HH:mm:ss");
                }

                var item = new ListViewItem(file.FileName);
                item.SubItems.Add(lastUploadedTime);//file.LastUploadTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A");
                item.SubItems.Add(lastModifiedTime);
                item.SubItems.Add(file.IsForHub ? "Yes" : "No");
                item.SubItems.Add(file.IsForNode ? "Yes" : "No");

                item.UseItemStyleForSubItems = true;

                SourceFiles_Box.Items.Add(item);

                if (file.LastUploadTime.HasValue && file.LastUploadTime.Value > LastUploadTime)
                {
                    LastUploadTime = file.LastUploadTime.Value;
                }
            }

            HighlightSourceFiles();
        }
        private void LoadRuntimeFiles()
        {
            RuntimeFiles_Box.Items.Clear();

            foreach (RuntimeFile rtFile in Settings.All.RuntimeFiles)
            {
                string lastSourceChange = "1985-10-27 08:00:00";
                string lastCompiledTimeString = "1985-10-27 08:00:00";
                string pushedTimeString = "1985-10-27 08:00:00";

                var item = new ListViewItem(rtFile.FileName);
                item.SubItems.Add(lastSourceChange);
                item.SubItems.Add(lastCompiledTimeString);
                item.SubItems.Add(pushedTimeString);
                item.UseItemStyleForSubItems = true;
                RuntimeFiles_Box.Items.Add(item);
            }

            HighlightRuntimeFiles();
        }
        private void LoadModelFiles()
        {
            ModelFiles_Box.Items.Clear();

            foreach (ModelFile model in Settings.All.Models)
            {
                string localPath = Path.Combine(Settings.All.LocalModelsPath, model.ModelType, model.ModelName);

                DateTime pushTime = model.LastPushTime.Value;
                string lastPushTime = pushTime.ToString("yyyy-MM-dd HH:mm:ss");

                string lastModifiedTime = "N/A";
                if (File.Exists(localPath))
                {
                    DateTime modifiedTime = File.GetLastWriteTime(localPath);
                    lastModifiedTime = modifiedTime.ToString("yyyy-MM-dd HH:mm:ss");
                }

                // Create a new ListViewItem with model name, type, last modified
                var item = new ListViewItem(model.ModelName);
                item.SubItems.Add(model.ModelType);
                item.SubItems.Add(lastPushTime);
                item.SubItems.Add(lastModifiedTime);

                item.UseItemStyleForSubItems = true;

                // ... get info from currentHub check the 'lastModified' time of /home/camcpp/models/modelName and adjust color appropriatley

                ModelFiles_Box.Items.Add(item);
            }

            HighlightModelFiles();
        }

        private void Reselect_Button_Click(object sender, EventArgs e)
        {
            HighlightModifiedFiles();
        }
        private void HighlightModifiedFiles()
        {
            HighlightSourceFiles();
            HighlightRuntimeFiles();
            HighlightModelFiles();
            HighlightNodes();
            checkServiceStatus(currentHub);
        }
        private void HighlightSourceFiles()
        {
            for (int i = 0; i < SourceFiles_Box.Items.Count; i++)
            {
                var item = SourceFiles_Box.Items[i];
                Color color = Color.Black;

                try
                {
                    string fileName = item.SubItems[0].Text;  // FileName column
                    string lastUploadText = item.SubItems[1].Text; // LastUploadTime column
                    string localPath = Path.Combine(Settings.All.SourceFilesDirectory, fileName);

                    if (File.Exists(localPath))
                    {
                        DateTime lastWrite = File.GetLastWriteTime(localPath);
                        if (DateTime.TryParse(lastUploadText, out var lastUpload) && lastWrite > lastUpload)
                        {
                            // highlight modified files
                            color = Color.Yellow;
                        }
                        else
                        {
                            // reset if not modified
                            color = Color.Green;
                        }

                        // update lastModifiedTime column dynamically
                        item.SubItems[2].Text = lastWrite.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        // highlight missing files differently
                        color = Color.Red;
                        item.SubItems[2].Text = "N/A";
                    }
                }
                catch
                {
                    // if something goes wrong, mark in DarkRed
                    color = Color.DarkRed;
                }

                // set color for entire row
                item.BackColor = color;
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                {
                    sub.BackColor = color;
                }
            }
        }
        private void HighlightRuntimeFiles()
        {
            foreach (ListViewItem item in RuntimeFiles_Box.Items)
            {
                DateTime? lastSourceChange = DateTime.MinValue;
                string lastSourceChangeString = "1985-10-27 08:00:00";
                DateTime? lastCompiledTime = DateTime.MinValue;
                string lastCompiledTimeString = "1985-10-27 08:00:00";
                DateTime? pushedTime = DateTime.MinValue;
                string pushedTimeString = "1985-10-27 08:00:00";

                Color color = Color.Black;
                try
                {
                    foreach (RuntimeFile rtFile in Settings.All.RuntimeFiles)
                    {
                        if (item.SubItems[0].Text == rtFile.FileName)
                        {
                            // Set lastSourceChange
                            if (rtFile.FileName.Contains('.'))
                            {
                                //Settings file

                                foreach (ListViewItem line in SourceFiles_Box.Items)
                                {
                                    if (rtFile.FileName == line.SubItems[0].Text)
                                    {
                                        if (DateTime.TryParse(line.SubItems[2].Text, out DateTime sourceModified))
                                        {
                                            lastSourceChange = sourceModified;
                                            lastSourceChangeString = lastSourceChange.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //bin file
                                int correctIndex = 0;
                                if (item.SubItems[0].Text == "hub")
                                    correctIndex = 3;
                                else if (item.SubItems[0].Text == "node")
                                    correctIndex = 4;

                                foreach (ListViewItem line in SourceFiles_Box.Items)
                                {
                                    if (line.SubItems[correctIndex].Text == "Yes")
                                    {
                                        if (DateTime.TryParse(line.SubItems[2].Text, out DateTime sourceModified))
                                        {
                                            if (sourceModified > lastSourceChange)
                                            {
                                                lastSourceChange = sourceModified;
                                                lastSourceChangeString = lastSourceChange.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                            }
                                        }
                                    }
                                }
                            }

                            // Set lastCompiledTime and pushedTime
                            if (!rtFile.IsForNode)
                            {
                                // for Hub

                                // Last Compiled Time
                                if (rtFile.FileName.Contains('.'))
                                {
                                    // for Settings file
                                    if (File.Exists(Settings.All.SourceFilesDirectory + "hubSettings.json"))
                                    {
                                        lastCompiledTime = File.GetLastWriteTime(Settings.All.SourceFilesDirectory + "hubSettings.json");
                                        lastCompiledTimeString = lastCompiledTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                }
                                else
                                {
                                    // for bin file
                                    if (currentHub.HubFileExists(rtFile.Path))
                                    {
                                        lastCompiledTime = currentHub.HubFileLastModified(rtFile.Path);
                                        lastCompiledTimeString = lastCompiledTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    else
                                    {
                                        color = Color.Red;
                                    }
                                }

                                // Last Pushed Time
                                if (currentHub.HubFileExists(rtFile.Path))
                                {
                                    pushedTime = currentHub.HubFileLastModified(rtFile.Path);
                                    pushedTimeString = pushedTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                }
                                else
                                {
                                    color = Color.Orange;
                                }
                            }
                            else
                            {
                                // for Node(s)!?!

                                // Last Compiled Time
                                if (rtFile.FileName.Contains('.'))
                                {
                                    // for Settings file
                                    if (File.Exists(Settings.All.SourceFilesDirectory + "Node1Settings.json"))
                                    {
                                        lastCompiledTime = File.GetLastWriteTime(Settings.All.SourceFilesDirectory + "Node1Settings.json");
                                        lastCompiledTimeString = lastCompiledTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                }
                                else
                                {
                                    // for bin file
                                    if (currentHub.HubFileExists(rtFile.Path))
                                    {
                                        lastCompiledTime = currentHub.HubFileLastModified(rtFile.Path);
                                        lastCompiledTimeString = lastCompiledTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                    }
                                    else
                                    {
                                        color = Color.Red;
                                    }
                                }

                                // Last Pushed Time
                                if (currentHub.NodeFileExists(rtFile.Path, Settings.All.Nodes.Find(n => n.Name == "Node1")?.APAddress ?? ""))
                                {
                                    pushedTime = currentHub.NodeFileLastModified(rtFile.Path, Settings.All.Nodes.Find(n => n.Name == "Node1")?.APAddress ?? "");
                                    pushedTimeString = pushedTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                }
                                else
                                {
                                    color = Color.Orange;
                                }


                                //// for Node(s)?!?

                                //// Last Compiled Time
                                //if (currentHub.HubFileExists(rtFile.StartingPath))
                                //{
                                //    lastCompiledTime = currentHub.HubFileLastModified(rtFile.StartingPath);
                                //    lastCompiledTimeString = lastCompiledTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                //}
                                //else
                                //{
                                //    color = Color.Red;
                                //}

                                //// Last Pushed Time
                                //if (currentHub.NodeFileExists(rtFile.EndingPath, Nodes_Box.Items[0].SubItems[1].Text))
                                //{
                                //    pushedTime = currentHub.NodeFileLastModified(rtFile.EndingPath, Nodes_Box.Items[0].SubItems[1].Text);
                                //    pushedTimeString = pushedTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
                                //}
                                //else
                                //{
                                //    color = Color.Orange;
                                //}
                            }

                            item.SubItems[1].Text = lastSourceChangeString;
                            item.SubItems[2].Text = lastCompiledTimeString;
                            item.SubItems[3].Text = pushedTimeString;
                        }
                    }
                }
                catch
                {
                    logger.Log(LogLevel.ERROR, "Uploader", $"Error during status check of Runtime Files");
                    item.SubItems[1].Text = "Error";
                    color = Color.DarkRed; // error occurred
                }

                if (color == Color.Black)
                {
                    if ((lastSourceChange <= lastCompiledTime) && (lastCompiledTime <= pushedTime))
                    {
                        color = Color.Green;
                    }
                    else
                    {
                        color = Color.Yellow;
                    }
                }

                // set color for entire row
                item.BackColor = color;
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                {
                    sub.BackColor = color;
                }
            }
        }
        private void HighlightModelFiles()
        {
            foreach (ListViewItem item in ModelFiles_Box.Items)
            {
                Color color = Color.Black;
                try
                {
                    string modelName = item.SubItems[0].Text; // ModelName column
                    string modelType = item.SubItems[1].Text; // ModelType column
                    string localPath = Path.Combine(Settings.All.LocalModelsPath, modelType, modelName);
                    if (File.Exists(localPath))
                    {
                        DateTime lastWrite = File.GetLastWriteTime(localPath);
                        item.SubItems[3].Text = lastWrite.ToString("yyyy-MM-dd HH:mm:ss");
                        if (DateTime.TryParse(item.SubItems[2].Text, out DateTime pushedDate))
                        {
                            if (lastWrite > pushedDate)
                                color = Color.Green; // model file exists
                            else
                                color = Color.Red; // model file exists but not newer than last push
                        }
                        else
                            color = Color.Gray;
                    }
                    else
                    {
                        item.SubItems[2].Text = "N/A";
                        color = Color.DarkRed; // model file missing
                    }
                }
                catch
                {
                    item.SubItems[2].Text = "Error";
                    color = Color.DarkRed; // error occurred
                }
                // set color for entire row
                item.BackColor = color;
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                {
                    sub.BackColor = color;
                }
            }
        }
        private void HighlightNodes()
        {
            foreach (ListViewItem nodeItem in Nodes_Box.Items)
            {
                Color color = Color.Black;
                try
                {
                    Device? nodeDevice = Settings.All.Nodes.Find(d => d.Name == nodeItem.SubItems[0].Text);

                    if (nodeDevice != null)
                    {
                        if (currentHub.PingNode(nodeDevice.APAddress))
                        {
                            logger.Log(LogLevel.INFO, "Uploader", $"Ping successful for node '{nodeDevice.Name}'. Checking service status...\n");

                            string result = "";
                            try
                            {
                                result = currentHub.ExecuteNodeCommand("systemctl is-active Node.service", nodeDevice.APAddress, nodeDevice.Username).Trim();

                                logger.Log(LogLevel.INFO, "Uploader", $"Service status for '{nodeDevice.Name}': {result}\n");
                            }
                            catch (Exception ex)
                            {
                                logger.Log(LogLevel.ERROR, "Uploader", $"Error checking service status on node '{nodeDevice.Name}' ({nodeDevice.APAddress}): {ex.Message}\n");
                                if (ex.InnerException != null)
                                    logger.Log(LogLevel.ERROR, "Uploader", $"Inner exception: {ex.InnerException.Message}\n");

                                result = "failed";
                            }

                            switch (result)
                            {
                                case "active":
                                    color = Color.Green;
                                    break;

                                case "inactive":
                                    color = Color.Yellow;
                                    break;

                                case "failed":
                                    color = Color.DarkRed;
                                    break;

                                case "activating":
                                    color = Color.Green;
                                    break;

                                case "deactivating":
                                    color = Color.Yellow;
                                    break;

                                default:
                                    color = Color.Gray;
                                    logger.Log(LogLevel.WARN, "Uploader", $"Unknown service state '{result}' for node '{nodeDevice.Name}'.\n");
                                    break;
                            }
                        }
                        else
                        {
                            color = Color.Red;
                            logger.Log(LogLevel.WARN, "Uploader", $"Ping failed for node '{nodeDevice.Name}' ({nodeDevice.APAddress}).\n");
                        }
                    }
                    else
                    {
                        color = Color.DarkRed;
                        logger.Log(LogLevel.ERROR, "Uploader", $"Device not found in Settings.All.Nodes for name '{nodeItem.SubItems[0].Text}'.\n");
                    }
                }
                catch (Exception ex)
                {
                    color = Color.DarkRed;
                    logger.Log(LogLevel.ERROR, "Uploader", $"Unhandled error while processing node '{nodeItem.SubItems[0].Text}': {ex.Message}\n");
                    if (ex.InnerException != null)
                        logger.Log(LogLevel.ERROR, "Uploader", $"Inner exception: {ex.InnerException.Message}\n");
                    logger.Log(LogLevel.ERROR, "Uploader", $"Stack Trace:\n{ex.StackTrace}\n");
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
            checkServiceStatus(hubComs[Clusters_Box.Text]);
        }
        private void checkServiceStatus(mCommunicator com)
        {
            string hubServiceName = "hub.service";
            string nodeServiceName = "node.service";
            string hubResult = "";
                logger.Log(LogLevel.INFO, "Uploader", $"Checking status of {hubServiceName}...\n");

            // Run systemctl command to get status
            try
            {
                hubResult = com.ExecuteHubCommand($"systemctl is-active {hubServiceName}").Trim();
                logger.Log(LogLevel.INFO, "Uploader", $"Status of {hubServiceName} on Hub: {hubResult}\n");
                foreach (ListViewItem node in Nodes_Box.Items)
                {
                    Device? nodeDevice = Settings.All.Nodes.Find(d => d.Name == node.SubItems[0].Text);
                    if (nodeDevice != null && nodeDevice.isActive)
                    {
                        string nodeResult = com.ExecuteNodeCommand($"systemctl is-active {nodeServiceName}", nodeDevice.APAddress, nodeDevice.Username).Trim();
                        logger.Log(LogLevel.INFO, "Uploader", $"Status of {nodeServiceName} on {nodeDevice.Name}: {nodeResult}\n");
                        if (nodeResult.Contains("inactive"))
                        {
                            node.BackColor = Color.Yellow;
                        }
                        else if (nodeResult.Contains("active"))
                        {
                            node.BackColor = Color.Green;
                        }
                        else if (nodeResult.Contains("failed"))
                        {
                            node.BackColor = Color.Red;
                        }
                    }
                    else
                    {
                        node.BackColor = Color.Gray;
                        logger.Log(LogLevel.ERROR, "Uploader", $"Device not found in Settings.All.Nodes for name '{node.SubItems[0].Text}'.\n");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"Error checking service status: {ex.Message}\n");
                hubResult = "failed";
            }

            switch (hubResult)
            {
                case "active":
                    logger.Log(LogLevel.INFO, "Uploader", $"{hubServiceName} is running.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Green;
                    Status_SourceCode_Label.ForeColor = Color.Orange;

                    StartService_Button.Enabled = false;
                    StopService_Button.Enabled = true;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case "inactive":
                    logger.Log(LogLevel.WARN, "Uploader", $"{hubServiceName} is stopped.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Orange;
                    Status_SourceCode_Label.ForeColor = Color.Green;

                    StartService_Button.Enabled = true;
                    StopService_Button.Enabled = false;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case "failed":
                    logger.Log(LogLevel.ERROR, "Uploader", $"{hubServiceName} has failed.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Red;
                    Status_SourceCode_Label.ForeColor = Color.Green;

                    StartService_Button.Enabled = true;
                    StopService_Button.Enabled = false;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case "activating":
                    logger.Log(LogLevel.INFO, "Uploader", $"{hubServiceName} is starting up.\n");

                    Status_SystemCTLService_Label.ForeColor = Color.Green;
                    Status_SourceCode_Label.ForeColor = Color.Orange;

                    StartService_Button.Enabled = false;
                    StopService_Button.Enabled = true;
                    CheckServiceStatus_Button.Enabled = true;
                    Upload_Button.Enabled = true;
                    ManualRecompile_Button.Enabled = true;
                    break;

                case "deactivating":
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
            checkSSHDevice(currentHub, true);
            HighlightModifiedFiles();
        }
        private bool checkSSHDevice(mCommunicator com, bool verbose)
        {
            bool allConnected = true;

            //foreach (var (device, communicator) in Settings.All.Hubs.Zip(hubs, (d, c) => (d, c)))
            //{
            string host = com._host;
            string username = com._username;

            if (verbose)
                logger.Log(LogLevel.INFO, "Uploader", $"Checking SSH connection to device {host} as {username}...\n");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isCnctd = false;

            try
            {
                if (com.Connect())
                {
                    if (verbose)
                        logger.Log(LogLevel.INFO, "Uploader", $"Successfully connected to {host} in {sw.ElapsedMilliseconds} ms.\n");
                    isCnctd = true;
                }
                else
                {
                    logger.Log(LogLevel.ERROR, "Uploader", $"Failed to connect to {host} without error.\n");
                    isCnctd = false;
                }
            }
            catch (Renci.SshNet.Common.SshAuthenticationException authEx)
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"Authentication failed for {username}@{host}: {authEx.Message}\n");
            }
            catch (Renci.SshNet.Common.SshConnectionException connEx)
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"Connection error to {host}: {connEx.Message}\n");
            }
            catch (System.Net.Sockets.SocketException sockEx)
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"Socket error while connecting to {host}: {sockEx.Message}\n");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"Unexpected error for {host}: {ex.GetType().Name} - {ex.Message}\n");
            }
            finally
            {
                sw.Stop();
                setGUISSHStatus(isCnctd);

                if (verbose)
                    logger.Log(LogLevel.INFO, "Uploader", $"Total connection attempt time for {host}: {sw.ElapsedMilliseconds} ms.\n");
            }

            if (!isCnctd)
                allConnected = false;
            //}

            return allConnected;
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
                checkServiceStatus(hubComs[Clusters_Box.Text]);
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
            HighlightModifiedFiles();

            try
            {
                logger.Log(LogLevel.INFO, "Uploader", "Starting Upload Process.\n");

                // Ensure connection
                checkSSHDevice(hubComs[Clusters_Box.Text], true);

                // Upload files
                UploadFiles();


                logger.Log(LogLevel.INFO, "Uploader", "Saving Data\n");
                SaveManagedFiles();
                logger.Log(LogLevel.INFO, "Uploader", "Upload process completed.\n");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error: " + ex.Message + '\n');
                logger.Log(LogLevel.INFO, "Uploader", "Upload Failed.\n");
            }
        }
        private void UploadFiles()
        {
            UploadSourceFiles();
            UploadModelFiles(); // Implement if needed
        }
        private void UploadSourceFiles()
        {

            //  This needs to be modified so that nodeSettings.json is modified for the target device
            // Determine which items to process
            var itemsToProcess = SourceFiles_Box.SelectedItems.Count > 0
                ? SourceFiles_Box.SelectedItems.Cast<ListViewItem>()
                : SourceFiles_Box.Items.Cast<ListViewItem>().Where(i => i.BackColor == Color.Yellow);

            foreach (ListViewItem item in itemsToProcess)
            {
                string fileName = item.SubItems[0].Text; // FileName column
                string localFile = Path.Combine(Settings.All.SourceFilesDirectory, fileName);
                string remoteHubFile = Path.Combine(Settings.All.UploadDirectory, "hub/", fileName)
                                       .Replace("\\", "/"); // normalize to Linux paths
                string remoteNodeFile = Path.Combine(Settings.All.UploadDirectory, "node/", fileName)
                                       .Replace("\\", "/"); // normalize to Linux paths

                if (item.SubItems[3].Text == "Yes")
                {
                    // Delete old file
                    currentHub.DeleteHubFile(remoteHubFile);

                    // Upload new file
                    logger.Log(LogLevel.INFO, "Uploader", $"Uploading '{localFile}' → '{remoteHubFile}'\n");
                    currentHub.CopyPCtoHub(localFile, remoteHubFile);
                }
                if (item.SubItems[4].Text == "Yes")
                {
                    // Delete old file
                    currentHub.DeleteHubFile(remoteNodeFile);

                    // Upload new file
                    logger.Log(LogLevel.INFO, "Uploader", $"Uploading '{localFile}' → '{remoteNodeFile}'\n");
                    currentHub.CopyPCtoHub(localFile, remoteNodeFile);
                }

                // Update ListView after upload
                string now = DateTime.Now.ToString("MMM-dd-yy HH:mm:ss");
                item.SubItems[1].Text = now; // LastUploadTime column
                item.BackColor = Color.Green;
                foreach (ListViewItem.ListViewSubItem sub in item.SubItems)
                {
                    sub.BackColor = Color.Green;
                }

                // Update settings
                var sourceFile = Settings.All.SourceFiles.FirstOrDefault(f => f.FileName == fileName);
                if (sourceFile != null)
                {
                    sourceFile.LastUploadTime = DateTime.Now;
                }
            }

            Settings.SaveSettings();
        }
        private void UploadModelFiles()
        {
            // Determine which items to process:
            // If something is selected, use those; otherwise use rows that have any device flagged as "missing" (red).
            var itemsToProcess = ModelFiles_Box.SelectedItems.Count > 0
                ? ModelFiles_Box.SelectedItems.Cast<ListViewItem>()
                : ModelFiles_Box.Items.Cast<ListViewItem>().Where(item =>
                    item.SubItems.Cast<ListViewItem.ListViewSubItem>().Any(sub => sub.BackColor == Color.Red));

            foreach (ListViewItem item in itemsToProcess)
            {
                string modelName = item.SubItems[0].Text; // ModelName column
                string modelType = item.SubItems[1].Text; // ModelType column
                string localFile = Path.Combine(Settings.All.LocalModelsPath, modelType, modelName);

                logger.Log(LogLevel.INFO, "Uploader", $"Uploading Model: '{modelName}'\n");

                // Try uploading to currentHub
                string remoteFile = Path.Combine(Settings.All.RemoteModelsPath, modelType, modelName).Replace("\\", "/");

                try
                {
                    currentHub.CopyPCtoHub(localFile, remoteFile);
                    foreach (Device node in Settings.All.Nodes.Where(d => d.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID))
                    {
                        currentHub.CopyHubToNode(remoteFile, remoteFile, node.APAddress, node.Username);
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.ERROR, "Uploader", $"Upload failed for {modelName} on device {currentHub._host}: {ex.Message}\n");
                }
            }

            Settings.SaveSettings();
        }

        private void DownloadFiles_Button_Click(object sender, EventArgs e)
        {
            DownloadFiles();
        }
        private void DownloadFiles()
        { 
            logger.Log(LogLevel.INFO, "Uploader", "Downloading Files...\n");
            try
            {
                if (DownloadLogs())
                {
                    logger.Log(LogLevel.INFO, "Uploader", "Logs downloaded successfully.\n");
                }
                else
                {
                    logger.Log(LogLevel.WARN, "Uploader", "Some errors occurred during download. Check logs for details.\n");
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading log files: " + ex.Message + '\n');
            }

            try
            {
                if (DownloadCaptures())
                {
                    logger.Log(LogLevel.INFO, "Uploader", "Captures downloaded successfully.\n");
                }
                else
                {
                    logger.Log(LogLevel.WARN, "Uploader", "Some errors occurred during capture download. Check logs for details.\n");
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading capture files: " + ex.Message + '\n');
            }

            try
            {
                if (DownloadSavedFrames())
                {
                    logger.Log(LogLevel.INFO, "Uploader", "Saved Frames downloaded successfully.\n");
                }
                else
                {
                    logger.Log(LogLevel.WARN, "Uploader", "Some errors occurred during saved frames download. Check logs for details.\n");
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading frame files: " + ex.Message + '\n');
            }

            try
            {
                if (DownloadCalibration())
                {
                    logger.Log(LogLevel.INFO, "Uploader", "Calibration files downloaded successfully.\n");
                }
                else
                {
                    logger.Log(LogLevel.WARN, "Uploader", "Some errors occurred during calibration download. Check logs for details.\n");
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error downloading calibrationfiles: " + ex.Message + '\n');
            }

            logger.Log(LogLevel.INFO, "Uploader", "Download process completed.\n");
        }
        private bool DownloadCalibration()
        {
            bool  returnValue = false;
            string HubSettingsStartingPath = "/home/camcpp/src/hubSettings.json";
            string BaseLocalLogDir = Settings.All.LocalLogPath;
            Device hub = Settings.All.Hubs[Clusters_Box.SelectedIndex];
            string HubCalibrationSettingsFile = Path.Combine(BaseLocalLogDir, hub.Name, "hubCalibrationSettings.json");

            if (currentHub.CopyHubToPC(HubSettingsStartingPath.Trim(), HubCalibrationSettingsFile, false))
            {
                var info = new FileInfo(HubCalibrationSettingsFile);
                logger.Log(LogLevel.INFO, "Uploader", $"[{hub.Name}] ✔ Successfully downloaded: {HubCalibrationSettingsFile} ({info.Length} bytes).  Merging into Settings Data...\n");

                int newDataPoints = Settings.MergeNewCalibrationData(HubCalibrationSettingsFile);
                if (newDataPoints > 0)
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"[{hub.Name}] ✔ Merged {newDataPoints} new calibration data point(s) into Settings.\n");
                }
                else
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"[{hub.Name}] No new calibration data found to merge.\n");
                }
                returnValue = true;
            }
            else
            {
                logger.Log(LogLevel.ERROR, "Uploader", $"[{hub.Name}] ⚠ Download failed: {HubCalibrationSettingsFile}\n");
                returnValue = false;
            }


            
            return returnValue;
        }
        private bool DownloadLogs()
        {
            bool hadErrors = false;
            string remoteLogDir = Settings.All.RemoteLogPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;
            Device hub = Settings.All.Hubs[Clusters_Box.SelectedIndex];

            // Hub logs
            {
                //OutputText($"Checking logs on {hub.Name}...\n", LogLevel.INFO);

                // Ask remote system for a list of .log files
                string cmd = $"ls -1 {remoteLogDir}*.log 2>/dev/null";
                string result = currentHub.ExecuteHubCommand(cmd);

                var remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (remoteFiles.Length == 0)
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"No .log files found on {hub.Name}.\n");
                }
                else
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"Found {remoteFiles.Length} .log files on {hub.Name}.\n");

                    foreach (string remoteFilePath in remoteFiles)
                    {
                        string baseLocalHubLogDir = Path.Combine(baseLocalLogDir, hub.Name, "Logs");
                        if (!Directory.Exists(baseLocalHubLogDir))
                            Directory.CreateDirectory(baseLocalHubLogDir);

                        string fileName = Path.GetFileName(remoteFilePath.Trim());
                        string localFilePath = Path.Combine(baseLocalHubLogDir, fileName);

                        //OutputText($"[{hub.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                        if (currentHub.CopyHubToPC(remoteFilePath.Trim(), localFilePath, false))
                        {
                            var info = new FileInfo(localFilePath);
                            logger.Log(LogLevel.INFO, "Uploader", $"[{hub.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                            if (!(GetDateFromLogFilename(fileName) == DateTime.Today))
                            {
                                if (currentHub.DeleteHubFile(remoteFilePath.Trim(), false))
                                {
                                    logger.Log(LogLevel.INFO, "Uploader", $"[{hub.Name}] 🗑 Deleted remote log file: {remoteFilePath}\n");
                                }
                                else
                                {
                                    logger.Log(LogLevel.ERROR, "Uploader", $"[{hub.Name}] ⚠ Successfully downloaded, but could not delete remote log file: {remoteFilePath}\n");
                                    hadErrors = true;
                                }
                            }
                        }
                        else
                        {
                            logger.Log(LogLevel.ERROR, "Uploader", $"[{hub.Name}] ⚠ Download failed: {localFilePath}\n");
                            hadErrors = true;
                        }
                    }
                }
            }


            foreach (Device node in Settings.All.Nodes)
            {
                if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID && node.isActive)
                {
                    // Create per-device local log folder
                    string deviceLocalLogDir = Path.Combine(baseLocalLogDir, node.Name, "Logs");
                    if (!Directory.Exists(deviceLocalLogDir))
                        Directory.CreateDirectory(deviceLocalLogDir);

                    //OutputText($"Checking logs on {node.Name} ({node.APAddress})...\n", LogLevel.INFO);

                    // Ask remote system for a list of .log files
                    string cmd = $"ls -1 {remoteLogDir}*.log 2>/dev/null";
                    string result = currentHub.ExecuteNodeCommand(cmd, node.APAddress, node.Username);

                    var remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

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
                            if (currentHub.CopyNodeToPC(remoteFilePath.Trim(), localFilePath, node.APAddress, false))
                            {
                                var info = new FileInfo(localFilePath);
                                logger.Log(LogLevel.INFO, "Uploader", $"[{node.Name}] ✔ Successfully downloaded: {localFilePath} ({info.Length} bytes)\n");
                                if (!(GetDateFromLogFilename(fileName) == DateTime.Today))
                                {
                                    if (currentHub.DeleteNodeFile(remoteFilePath.Trim(), node.APAddress, false))//, node.Username, false))
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

            foreach (Device node in Settings.All.Nodes)
            {
                if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID && node.isActive)
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
                        string result = currentHub.ExecuteNodeCommand(cmd, node.APAddress, node.Username);

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
                                if (currentHub.CopyNodeToPC(remoteFilePath.Trim(), localFilePath, node.APAddress))
                                {
                                    if (currentHub.DeleteNodeFile(remoteFilePath.Trim(), node.APAddress, false))//, node.Username, false))
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
            }
            return !hadErrors;
        }
        private bool DownloadSavedFrames()
        {
            bool hadErrors = false;
            string remoteReconstructionsDir = Settings.All.RemoteReconstructionsPath;
            string baseLocalLogDir = Settings.All.LocalLogPath;
            Device hub = Settings.All.Hubs[Clusters_Box.SelectedIndex];

            // Hub reconstructions
            {
                logger.Log(LogLevel.INFO, "Uploader", $"Checking reconstructions on {hub.Name}...\n");

                // Ask remote system for a list of .json files
                string cmd = $"ls -1 {remoteReconstructionsDir}*.json 2>/dev/null";
                string result = currentHub.ExecuteHubCommand(cmd);

                var remoteFiles = result.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (remoteFiles.Length == 0)
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"No reconstruction files found on {hub.Name}.\n");
                }
                else
                {
                    logger.Log(LogLevel.INFO, "Uploader", $"Found {remoteFiles.Length} reconstruction files on {hub.Name}.\n");

                    foreach (string remoteFilePath in remoteFiles)
                    {
                        string baseLocalHubReconstructionsDir = Path.Combine(baseLocalLogDir, hub.Name, "Reconstructions");
                        if (!Directory.Exists(baseLocalHubReconstructionsDir))
                            Directory.CreateDirectory(baseLocalHubReconstructionsDir);

                        string fileName = Path.GetFileName(remoteFilePath.Trim());
                        string localFilePath = Path.Combine(baseLocalHubReconstructionsDir, fileName);

                        //OutputText($"[{hub.Name}] Downloading {remoteFilePath} → {localFilePath}\n", LogLevel.INFO);
                        if (currentHub.CopyHubToPC(remoteFilePath.Trim(), localFilePath))
                        {
                            var info = new FileInfo(localFilePath);
                            //OutputText($"[{hub.Name}] ✔ Verified: {localFilePath} ({info.Length} bytes)\n", LogLevel.INFO);
                            if (currentHub.DeleteHubFile(remoteFilePath.Trim(), false))
                            {
                                logger.Log(LogLevel.INFO, "Uploader", $"[{hub.Name}] ✔ Successfully downloaded reconstruction file: {remoteFilePath}\n");
                                //OutputText($"[{hub.Name}] 🗑 Deleted reconstruction file: {remoteFilePath}\n", LogLevel.INFO);
                            }
                            else
                            {
                                logger.Log(LogLevel.ERROR, "Uploader", $"[{hub.Name}] ⚠ Successfully downloaded, but could not delete reconstruction file: {remoteFilePath}\n");
                                hadErrors = true;
                            }
                        }
                        else
                        {
                            logger.Log(LogLevel.ERROR, "Uploader", $"[{hub.Name}] ⚠ Download failed: {localFilePath}\n");
                            hadErrors = true;
                        }
                    }
                }
            }

            return !hadErrors;
        }

        private void ManualRecompile_Button_Click(object sender, EventArgs e)
        {
            ManualRecompile();
            //currentHub.testFileMethods(Settings.All.SourceFilesDirectory + "file.txt", Settings.All.Nodes[0].APAddress, Settings.All.Nodes[0].Username);
        }
        private void ManualRecompile()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Preping to recompiling project...\n");
            stopMain();

            //currentHub.DeleteFile("/home/camcpp/previous_node_d");
            //currentHub.MoveFile("/home/camcpp/node", "/home/camcpp/previous_node_d");
            //currentHub.ExecuteCommand("g++ -o node -g /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include"); // debug

            currentHub.DeleteHubFile("/home/camcpp/previous_hub");
            currentHub.MoveHubFile("/home/camcpp/hub", "/home/camcpp/previous_hub");
            currentHub.DeleteHubFile("/home/camcpp/previous_node");
            currentHub.MoveHubFile("/home/camcpp/node", "/home/camcpp/previous_node");

            logger.Log(LogLevel.INFO, "Uploader", "Ready to recompile manually.  Please Run:\n");
            logger.Log(LogLevel.INFO, "Uploader", "time make -C /home/camcpp/build\n");
        }
        private void AutoRecompile_Button1_Click(object sender, EventArgs e)
        {
            AutoRecompile();

            DistributeRuntimeFiles();
        }
        private void AutoRecompile()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Starting Automated Recompile.\n");
            stopMain();

            //currentHub.DeleteFile("/home/camcpp/previous_node_d");
            //currentHub.MoveFile("/home/camcpp/node", "/home/camcpp/previous_node_d");
            //currentHub.ExecuteCommand("g++ -o node -g /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include"); // debug

            currentHub.DeleteHubFile("/home/camcpp/previous_hub");
            currentHub.MoveHubFile("/home/camcpp/hub", "/home/camcpp/previous_hub");
            currentHub.DeleteHubFile("/home/camcpp/previous_node");
            currentHub.MoveHubFile("/home/camcpp/node", "/home/camcpp/previous_node");

            //currentHub.ExecuteHubCommand("g++ -O2 -o node  /home/camcpp/src/*.cpp $(pkg-config --cflags --libs opencv4) -lgpiod -lonnxruntime -I/usr/local/include");    // release
            logger.Log(LogLevel.INFO, "Uploader", "Recompiling...\n");
            currentHub.ExecuteHubCommand("make -C /home/camcpp/build");

            logger.Log(LogLevel.INFO, "Uploader", "Recompile completed.\n");
        }


        private void DistributeRuntimeFiles_Button_Click(object sender, EventArgs e)
        {
            DistributeRuntimeFiles();
        }
        private void DistributeRuntimeFiles()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Distributing Runtime Files");
            //  Copy Hub's settings file
            string hubSettingsStartingPath = Settings.All.SourceFilesDirectory + "hubSettings.json";
            string hubSettingsEndingPath = "/home/camcpp/src/hubSettings.json";
            logger.Log(LogLevel.INFO, "Uploader", $"Copying {hubSettingsStartingPath} to {hubSettingsEndingPath}");
            currentHub.CopyPCtoHub(hubSettingsStartingPath, hubSettingsEndingPath, false);

            //  Copy binary and settings file to each Node
            string nodeBinFile = "/home/camcpp/node";
            string nodeSettingsEndingPath = "/home/camcpp/src/nodeSettings.json";
            foreach (Device node in Settings.All.Nodes)
                if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID && node.isActive)
                {
                    string nodeSettingsStartingPath = Settings.All.SourceFilesDirectory + $"{node.Name}Settings.json";
                    logger.Log(LogLevel.INFO, "Uploader", $"Copying hub:{nodeBinFile} to node:{nodeBinFile}");
                    currentHub.CopyHubToNode(nodeBinFile, nodeBinFile, node.APAddress, node.Username);
                    logger.Log(LogLevel.INFO, "Uploader", $"Copying hub:{nodeSettingsStartingPath} to node:{nodeSettingsEndingPath}");
                    currentHub.CopyPCtoNode(nodeSettingsStartingPath, nodeSettingsEndingPath, node.APAddress, false);
                }

            logger.Log(LogLevel.INFO, "Uploader", "Distribution Complete");
            HighlightRuntimeFiles();
        }

        private void RunMain_Button_Click(object sender, EventArgs e)
        {
            startMain();
        }
        private void startMain()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Starting Services...\n");
            currentHub.ExecuteHubCommand("sudo systemctl start hub.service");
            logger.Log(LogLevel.INFO, "Uploader", "    Hub service starting....\n");

            //System.Threading.Thread.Sleep(30);

            logger.Log(LogLevel.DEBUG, "Uploader", "    Starting Node services on all devices...\n");

            foreach (Device node in Settings.All.Nodes)
                if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID && node.isActive)
                {
                    currentHub.ExecuteNodeCommand($"sudo systemctl start node.service", node.APAddress, node.Username);
                    logger.Log(LogLevel.INFO, "Uploader", $"    Node service starting on {node.Name}....\n");
                }

            //System.Threading.Thread.Sleep(5);

            logger.Log(LogLevel.INFO, "Uploader", "Service started on all devices.\n");
            checkServiceStatus(currentHub);
        }


        private void StopService_Button_Click(object sender, EventArgs e)
        {
            stopMain();

            DownloadFiles();
        }
        private void stopMain()
        {
            logger.Log(LogLevel.INFO, "Uploader", "Stopping Services...\n");
            currentHub.ExecuteHubCommand("sudo systemctl stop hub.service");
            logger.Log(LogLevel.INFO, "Uploader", "    Hub service stopping....\n");

            foreach (Device node in Settings.All.Nodes)
                if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID && node.isActive)
                    currentHub.ExecuteNodeCommand($"sudo systemctl stop node.service", node.APAddress, node.Username);

            logger.Log(LogLevel.INFO, "Uploader", "Service stopped on all devices.\n");
            checkServiceStatus(currentHub);
        }

        private void Reboot_Button_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Log(LogLevel.INFO, "Uploader", "Starting Reboot Process.\n");

                // Reboot each Node
                foreach (Device node in Settings.All.Nodes)
                    if ((node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID) && (node.Role != "Hub") && node.isActive)
                    {
                        //currentHub.ExecuteCommand($"ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown -r +2\"");
                        logger.Log(LogLevel.INFO, "Uploader", $"Rebooting {node.Username}@{node.APAddress}.\n");
                        currentHub.ExecuteHubCommand($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown -r +1\" > /dev/null 2>&1 &");
                    }
                logger.Log(LogLevel.INFO, "Uploader", $"Shutting down {currentHub._username}@{currentHub._host}.\n");
                currentHub.ExecuteHubCommand("sudo shutdown -r now");

                currentHub.Disconnect();

                setGUISSHStatus(false);


                logger.Log(LogLevel.INFO, "Uploader", "Reboot Complete.\n");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error: " + ex.Message + '\n');
            }
        }
        private void Shutdown_Button_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Log(LogLevel.INFO, "Uploader", "Starting Shutdown Process.\n");

                // Shutdown each Node
                foreach (Device node in Settings.All.Nodes)
                    if (node.ClusterID == Settings.All.Hubs[Clusters_Box.SelectedIndex].ClusterID && node.isActive)
                    {
                        logger.Log(LogLevel.INFO, "Uploader", $"Shutting down {node.Username}@{node.APAddress}.\n");
                        currentHub.ExecuteHubCommand($"nohup ssh -tt {node.Username}@{node.APAddress} \"sudo shutdown now\" > /dev/null 2>&1 &");
                        //currentHub.ExecuteCommand("sudo shutdown now", node.APAddress, node.Username, node.Password);
                    }
                //System.Threading.Thread.Sleep(1500);
                //currentHub.Connect();
                logger.Log(LogLevel.INFO, "Uploader", $"Shutting down {currentHub._username}@{currentHub._host}.\n");
                currentHub.ExecuteHubCommand("sudo shutdown now");


                currentHub.Disconnect();

                setGUISSHStatus(false);

                logger.Log(LogLevel.INFO, "Uploader", "Shutdown Complete.\n");
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.ERROR, "Uploader", "Error: " + ex.Message + '\n');
            }
        }

        private void Clusters_Box_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentHub = hubComs[Clusters_Box.Text];

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
