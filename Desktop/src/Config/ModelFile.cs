namespace Dplus_Desktop.Config
{
    public class ModelFile
    {
        public string ModelName { get; set; } = string.Empty;
        public string ModelType { get; set; } = string.Empty;
        public DateTime? LastModifiedTime { get; set; }
        public DateTime? LastPushTime { get; set; } = null;
    }
}
