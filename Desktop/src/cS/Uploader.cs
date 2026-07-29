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
        //ClusterManager currentCluster;

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

                checkServiceStatus(clusters[Clusters_Box.Text]);

                UpdateManagedFiles_Boxes();
            }
        }

        private void AddLogSource(string source, Color color = default, bool andModules = true)
        {
            logger.AddSource(source, color, andModules);
            logger.Log(mLogger.LogLevel.INFO, "Uploader", $"Added source '{source}' to _tbSink");
        }
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
        private void UpdateManagedFiles_Boxes()
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
                        if (file.LastModifiedTime == DateTime.MinValue)
                            color = Color.Red;          // Local copy missing
                        if (file.LastUploadTime == DateTime.MinValue)
                            color = Color.Orange;       // Hub copy missing
                        else if (file.LastModifiedTime > file.LastUploadTime)
                            color = Color.Yellow;       // Needs upload to update
                        else
                            color = Color.Green;        // Good to go
                    }
                    else
                    {
                        color = Color.DarkRed;          // No last modified (file likley missing)
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

                if ((file.LastSourceChangeTime.HasValue && file.LastSourceChangeTime > DateTime.MinValue) && (file.LastCompliedTime.HasValue && file.LastCompliedTime > DateTime.MinValue) && (file.LastPushedTime.HasValue && file.LastPushedTime > DateTime.MinValue))
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
                item.BackColor = color;
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
                    if (file.LastModifiedTime == DateTime.MinValue)
                        color = Color.DarkRed;
                    else if (file.LastPushTime == DateTime.MinValue)
                        color = Color.Red;          // No last modified and/or pushed time (file likeley missing)
                    else if (file.LastModifiedTime > file.LastPushTime)
                        color = Color.Yellow;       // Needs to be compiled
                    else
                        color = Color.Green;        // Good to go
                }
                else
                {
                    color = Color.DarkRed;              // No last modified and/or pushed time (file likeley missing)
                }

                // Create a new ListViewItem with file name, type, last modified
                var item = new ListViewItem(file.ModelName);
                item.SubItems.Add(file.ModelType);
                item.SubItems.Add(lastPushTime);
                item.SubItems.Add(lastModifiedTime);
                item.BackColor = color;
                item.UseItemStyleForSubItems = true;

                ModelFiles_Box.Items.Add(item);
            }
        }

        private void HighlightNodes()
        {
            foreach (ListViewItem nodeItem in Nodes_Box.Items)
            {
                Device? nodeDevice = Settings.All.Nodes.Find(d => d.Name == nodeItem.SubItems[0].Text);
                Color color = Color.Black;

                if (nodeDevice != null)
                {
                    ServiceStatus status = clusters[Clusters_Box.Text].CheckDeviceServiceStatus(nodeDevice);
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

                nodeItem.BackColor = color;
            }
        }
        private void checkServiceStatus(ClusterManager com)
        {
            CurrentCluster_StatusStrip.UpdateStatus(com.CheckSystem());
        }
        private void checkServiceStatus()
        {
            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());
        }

        #region WinformEventHandlers
        private void Uploader_FormClosing(object sender, FormClosingEventArgs e)
        {
            //SaveManagedFiles();

            logger.LogHeading(LogLevel.INFO, "Uploader", "Uploader Exiting");
        }
        private void Reselect_Button_Click(object sender, EventArgs e)
        {
            UpdateManagedFiles_Boxes();
        }
        private void CheckServiceStatus_Button_Click(object sender, EventArgs e)
        {
            checkServiceStatus(clusters[Clusters_Box.Text]);
        }
        private void Connect_Click(object sender, EventArgs e)
        {
            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());
            UpdateManagedFiles_Boxes();
        }
        private void Upload_Button_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Log(LogLevel.INFO, "Uploader", "Starting Upload Process.\n");

                // Ensure connection
                clusters[Clusters_Box.Text].CheckSSH(true);

                // Upload files
                clusters[Clusters_Box.Text].UploadFiles();

                // Show new status in GUI
                UpdateManagedFiles_Boxes();

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
            clusters[Clusters_Box.Text].DownloadFiles();
        }
        private void ManualRecompile_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].ManualRecompile(BackupFirst_Box.Checked);
        }
        private void AutoRecompile_Button1_Click(object sender, EventArgs e)
        {

            clusters[Clusters_Box.Text].AutoRecompile(BackupFirst_Box.Checked);
            clusters[Clusters_Box.Text].DistributeRuntimeFiles();

            UpdateManagedFiles_Boxes();
        }
        private void DistributeRuntimeFiles_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].DistributeRuntimeFiles();
        }
        private void RunMain_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].startMain();
            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());
        }
        private void StopService_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].stopMain();

            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());

            clusters[Clusters_Box.Text].DownloadFiles();
        }
        private void Reboot_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].RebootCluster();
            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());
        }
        private void Shutdown_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].ShutdownCluster();
            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());
        }
        private void Clusters_Box_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentCluster_StatusStrip.UpdateStatus(clusters[Clusters_Box.Text].CheckSystem());
            UpdateManagedFiles_Boxes();
            LoadNodes();
        }
        private void CreateSettingsFiles_Button_Click(object sender, EventArgs e)
        {
            clusters[Clusters_Box.Text].CreateSettingsFiles();
        }

        #endregion
    }
}
