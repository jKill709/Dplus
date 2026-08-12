namespace Dplus_Desktop.Config
{
    public class SourceFile
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime? LastUploadTime { get; set; }
        public DateTime? LastModifiedTime { get; set; }
        public bool IsForHub { get; set; } = false;
        public bool IsForNode { get; set; } = false;
    }
}
