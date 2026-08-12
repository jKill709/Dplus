using jColorProviders;
using jCommunicator;
using Dplus_Desktop.Network;

namespace Dplus_Desktop.UI.Controls
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