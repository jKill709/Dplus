using jColorProviders;
using jCommunicator;

namespace Dplus_Desktop.WinForms.Controls.ClusterStatusDisplay
{
    public class ServiceStatusColorProvider : IColorProvider<ServiceStatus>
    {
        List<Color> _colors = new List<Color> { Color.Green,
                                                Color.LightGreen,
                                                Color.LightGoldenrodYellow,
                                                Color.Yellow,
                                                Color.DarkGray,
                                                Color.DarkRed };
        public Color GetColor(ServiceStatus status)
        {
            return _colors[(int)status];
        }
    }
    public interface IClusterStatusDisplay
    {
        void UpdateStatus(ClusterStatus status);
    }
}