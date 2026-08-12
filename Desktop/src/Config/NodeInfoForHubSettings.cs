namespace Dplus_Desktop.Config
{
    public class NodeInfoForHubSettings
    {
        public string name { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string IPAddress { get; set; } = string.Empty;
        public Intrinsics? intrinsics { get; set; } = new Intrinsics();
    }
}
