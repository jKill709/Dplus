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
        private ServiceStatusColorProvider _colorProvider = new ServiceStatusColorProvider();
        
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

            int margin = 35;
            int diameter = 5;

            int desktopX = margin;
            int hubX = Width / 2;
            int nodeX = Width - margin;

            int y = (Height - diameter) / 2;

            Graphics g = e.Graphics;
            g.Clear(SystemColors.Control);

            //DrawBorder(g);

            DrawDevice(g, "Desktop", desktopX, y, _colorProvider.GetColor(ServiceStatus.Active));

            DrawConnection(g, (desktopX + hubX) / 2, y, (int)((hubX - desktopX) * (0.8)), _status.SSHConnected ? Color.Green : Color.DarkGray);

            DrawDevice(g, "Hub", hubX, y, _status.SSHConnected ? _colorProvider.GetColor(_status.HubServiceStatus) : Color.DarkGray);

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
            int deviceWidth = 60;
            int radius = 5;

            Rectangle circle = new Rectangle(x - radius, y - (int)(Height * 0.2), radius * 2, radius * 2);

            using Brush brush = new SolidBrush(color);
            using Pen pen = new(Color.Black);

            g.FillEllipse(brush, circle);
            g.DrawEllipse(pen, circle);

            RectangleF textRect = new(x - (deviceWidth / 2), y + Height * 0.2f + radius - (Font.Height / 2), deviceWidth, Font.Height);
            using StringFormat sf = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            //g.DrawString(label, Font, Brushes.Blue, x - (deviceWidth / 2), y + ((int)(Height * 0.2) + radius), sf);
            g.DrawString(label, Font, Brushes.Black, textRect, sf);
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

            return _colorProvider.GetColor(_status.HubServiceStatus);
        }

        private ServiceStatus GetWorstNodeServiceStatus()
        {
            ServiceStatus returnValue = ServiceStatus.Active;
            foreach (var(name, status) in _status.NodeServiceStatuses)
            {
                if (returnValue < status)
                    returnValue = status;
            }

            return returnValue;
        }
        private ServiceStatus GetWorstServiceStatus()
        {
            ServiceStatus returnValue = GetWorstNodeServiceStatus();
            if (returnValue < _status.HubServiceStatus)
                returnValue = _status.HubServiceStatus;
        
            return returnValue; 
        }
        private ServiceStatus GetBestNodeServiceStatus()
        {

            ServiceStatus returnValue = ServiceStatus.Error;
            foreach (var (name, status) in _status.NodeServiceStatuses)
            {
                if (returnValue > status)
                    returnValue = status;
            }

            return returnValue;
        }
        private ServiceStatus GetBestServiceStatus()
        {
            ServiceStatus returnValue = GetBestNodeServiceStatus();
            if (returnValue > _status.HubServiceStatus)
                returnValue = _status.HubServiceStatus;

            return returnValue;
        }
    }
}