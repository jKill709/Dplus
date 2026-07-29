using jCommunicator;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Dplus_Desktop.WinForms.Controls.ClusterStatusDisplay
{
    public class DeviceStatusStrip : UserControl, IClusterStatusDisplay
    {
        private ClusterStatus _status = new ClusterStatus(false, 0, ServiceStatus.Failed, []);
        
        public DeviceStatusStrip()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;

            Font = new Font("Segoe UI", 8);
            Height = 25;
            MinimumSize = new Size(150, 25);
        }

        public void UpdateStatus(ClusterStatus status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateStatus(status));
                return;
            }

            _status = status ?? throw new ArgumentNullException(nameof(status));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            int margin = 25;
            int diameter = 5;

            int desktopX = margin;
            int hubX = Width / 2;
            int nodeX = Width - margin;

            int y = Height / 2;

            Graphics g = e.Graphics;
            g.Clear(SystemColors.Control);

            DrawBorder(g);

            DrawDevice(g, "Desktop", desktopX, y, Color.Green);

            DrawConnection(g, (desktopX + hubX) / 2, y, (int)((hubX - desktopX) * (0.8)), _status.SSHConnected ? Color.Green : Color.DarkGray);

            DrawDevice(g, "Hub", hubX, y, _status.SSHConnected ? GetStatusColor(_status.HubServiceStatus) : Color.DarkGray);

            DrawConnection(g, (hubX + nodeX) / 2, y, (int)((nodeX - hubX) * (0.8)), _status.SSHConnected ? Color.Green : Color.DarkGray);

            DrawDevice(g, $"Nodes ({_status.NodeCount})", nodeX, y, GetNodeColor());
        }

        private void DrawBorder(Graphics g)
        {
            using Pen pen = new(Color.LightGray);
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        private void DrawDevice(Graphics g, string label, int x, int y, Color color)
        {
            int deviceWidth = 40;
            int radius = 5;

            Rectangle circle = new Rectangle(x - radius, y - (int)(Height * 0.2), radius * 2, radius * 2);

            using Brush brush = new SolidBrush(color);
            using Pen pen = new(Color.Black);

            g.FillEllipse(brush, circle);
            g.DrawEllipse(pen, circle);

            using StringFormat sf = new()
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };

            g.DrawString(label, Font, Brushes.Black, x - (deviceWidth / 2), y + ((int)(Height * 0.2) + radius), sf);
        }

        private void DrawConnection(Graphics g, int x, int y, int connectionLength, Color color)
        {
            using Pen pen = new(color, 3);

            g.DrawLine(pen, x - (connectionLength / 2), y, x + (connectionLength / 2), y);
        }

        private string GetStatusMessage()
        {
            if (!_status.SSHConnected)
                return "Hub Offline";

            return _status.HubServiceStatus switch
            {
                ServiceStatus.Active => "Cluster Running",
                ServiceStatus.Activating => "Starting",
                ServiceStatus.Deactivating => "Stopping",
                ServiceStatus.Inactive => "Software Stopped",
                ServiceStatus.Failed => "Service Failed",
                ServiceStatus.Error => "Node Communication Error",
                _ => "Unknown"
            };
        }

        private Color GetNodeColor()
        {
            if (!_status.SSHConnected)
                return Color.DarkGray;

            return _status.HubServiceStatus switch
            {
                ServiceStatus.Active => Color.ForestGreen,
                ServiceStatus.Activating => Color.Goldenrod,
                ServiceStatus.Deactivating => Color.Orange,
                ServiceStatus.Inactive => Color.SteelBlue,
                ServiceStatus.Failed => Color.Firebrick,
                ServiceStatus.Error => Color.Firebrick,
                _ => Color.DarkGray
            };
        }

        private Color GetStatusColor(ServiceStatus status)
        {
            return status switch
            {
                ServiceStatus.Active => Color.ForestGreen,
                ServiceStatus.Activating => Color.Goldenrod,
                ServiceStatus.Deactivating => Color.Orange,
                ServiceStatus.Inactive => Color.SteelBlue,
                ServiceStatus.Failed => Color.Firebrick,
                ServiceStatus.Error => Color.Firebrick,
                _ => Color.DarkGray
            };
        }
    }
}