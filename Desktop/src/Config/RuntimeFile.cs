namespace Dplus_Desktop.Config
{
    public class RuntimeFile
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsForNode { get; set; } = false;
        public string Path { get; set; } = string.Empty;
        public DateTime? LastSourceChangeTime { get; set; }
        public DateTime? LastCompliedTime { get; set; }
        public DateTime? LastPushedTime { get; set; }
    }
}
