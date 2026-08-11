using Dplus_Desktop.SettingsManager;
using mLogger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.LinkLabel;

namespace Dplus_Desktop
{
    public partial class Main : Form
    {
        private string logName = "CamManager";

        private Uploader? _uploader;   // These hold the single instances
        private Viewer? _viewer;       // of each form

        Dictionary<string, ClusterManager> clusters;

        Logger _logger = Logger.Instance;
        RichTextBoxSink _tbSink;
        TextFileSink _tfSink;

        public Main()  // Constructor for form 'Main'
        {
            InitializeComponent();

            _tbSink = new RichTextBoxSink(LiveLoggingBox);
            _tbSink.AddSource(logName, Color.Red, true);
            _logger.AddSink(_tbSink);
            _logger.Log(LogLevel.INFO, logName, "_tbSink Added");

            _tfSink = new TextFileSink(Path.Combine(Settings.All.LocalLogPath, logName), logName, ".log");
            _logger.AddSink(_tfSink);
            _logger.Log(LogLevel.INFO, logName, "_tfSink Added: " + Settings.All.LocalLogPath);

            _logger.AddSource("ClusterManager");

            clusters = new Dictionary<string, ClusterManager>();

            foreach (Device hub in Settings.All.Hubs)
            {
                ClusterManager cluster = new ClusterManager(hub, Settings.All.GetNodesByClusterID(hub.ClusterID));
                clusters.Add(hub.ClusterID, cluster);
                Clusters_Box.Items.Add(hub.ClusterID);

                cluster.Connected += UpdateClusterStatusControl;
                cluster.Disconnected += UpdateClusterStatusControl;
            }

            if (Clusters_Box.Items.Count == 0)
            {
                MessageBox.Show("No hubs configured. Please configure settings first.");
                throw new Exception("No clusters available. Please configure settings first.");
            }
            else
            {
                Clusters_Box.SelectedIndex = 0;
            }

            _logger.LogHeading(LogLevel.INFO, logName, "Main Initialized");
        }
        private async void Main_Load(object sender, EventArgs e)
        {
            foreach (ClusterManager cluster in clusters.Values)
            {
                await cluster.ConnectAsync();
            }
        }

        private void AddLogSource(string source, Color color = default, bool andModules = true)
        {
            _logger.AddSource(source, color, andModules);
            _logger.Log(LogLevel.INFO, logName, $"Added source '{source}' to _tbSink");
        }

        private async void UpdateClusterStatusControl(object? sender, EventArgs e)
        {
            CurrentCluster_StatusStrip.UpdateStatus(await clusters[Clusters_Box.Text].CheckSystem());
        }
        private void Uploader_Button_Click(object sender, EventArgs e)
        {
            if (_uploader == null || _uploader.IsDisposed)
            {
                AddLogSource("Uploader", Color.Green, true);
                _uploader = new Uploader(clusters);
                _uploader.FormClosed += (s, args) => _uploader = null; // cleanup
                _uploader.Show();
            }
            else
            {
                _uploader.BringToFront();
                _uploader.Focus();
            }
        }
        private void Viewer_Button_Click(object sender, EventArgs e)
        {
            if (_viewer == null || _viewer.IsDisposed)
            {
                AddLogSource("Viewer", Color.Blue, true);
                _viewer = new Viewer(clusters[Clusters_Box.Text]);
                _viewer.FormClosed += (s, args) => _viewer = null; // cleanup
                _viewer.Show(this);
            }
            else
            {
                _viewer.BringToFront();
                _viewer.Focus();
            }
        }
        private async void Clusters_Box_SelectedIndexChanged(object sender, EventArgs e)
        {
            CurrentCluster_StatusStrip.UpdateStatus(await clusters[Clusters_Box.Text].CheckSystem());
        }

    }
}
