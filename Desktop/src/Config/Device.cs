namespace Dplus_Desktop.Config
{
    public class Device
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool isActive { get; set; } = false;
        public string ClusterID { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string APAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int CameraIDnumber { get; set; } = 0;
    }
}
