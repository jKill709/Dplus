using jCommunicator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dplus_Desktop
{

    public sealed class ClusterStatus
    {
        public bool SSHConnected { get; init; }

        public int NodeCount { get; init; }

        public ServiceStatus HubServiceStatus { get; init; }
        public Dictionary<string, ServiceStatus> NodeServiceStatuses { get; init; }

        public ClusterStatus(bool SSHConnected, int NodeCount, ServiceStatus HubServiceStatus, Dictionary<string, ServiceStatus> NodeServiceStatuses)
        {
            this.SSHConnected = SSHConnected;
            this.NodeCount = NodeCount;
            this.HubServiceStatus = HubServiceStatus;
            this.NodeServiceStatuses = NodeServiceStatuses;
        }
        public ClusterStatus()
        {
            this.SSHConnected = false;
            this.NodeCount = 0;
            this.HubServiceStatus = ServiceStatus.Error;
            this.NodeServiceStatuses = new Dictionary<string, ServiceStatus>();
        }
    }
}
