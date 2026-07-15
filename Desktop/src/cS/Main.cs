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
using static System.Windows.Forms.LinkLabel;

namespace Dplus_Desktop
{
    public partial class Main : Form
    {
        private Uploader? _uploader;   // These hold the single instances
        private Viewer? _viewer;       // of each form

        Logger logger = Logger.Instance;
        RichTextBoxSink _tbSink;
        TextFileSink _tfSink;

        public Main()  // Constructor for form 'Main'
        {
            InitializeComponent();

            _tbSink = new RichTextBoxSink(LiveLoggingBox);
            logger.AddSink(_tbSink);
            logger.Log(LogLevel.INFO, "CamManager", "_tbSink Added");

            _tfSink = new TextFileSink(Path.Combine(Settings.All.LocalLogPath, "CamManager"), "CamManager", ".log");
            logger.AddSink(_tfSink);
            logger.Log(LogLevel.INFO, "CamManager", "_tfSink Added: " + Settings.All.LocalLogPath);

            logger.LogHeading(LogLevel.INFO, "CamManager", "Main Initialized");
        }

        private void Uploader_Button_Click(object sender, EventArgs e)
        {
            if (_uploader == null || _uploader.IsDisposed)
            {
                _uploader = new Uploader();
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
                _viewer = new Viewer();
                _viewer.FormClosed += (s, args) => _viewer = null; // cleanup
                _viewer.Show();
            }
            else
            {
                _viewer.BringToFront();
                _viewer.Focus();
            }
        }
    }
}
