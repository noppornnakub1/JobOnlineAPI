namespace JobOnlineAPI.Models
{
    public class FileStorageConfig
    {
        public string? ProductionPath { get; set; }
        public string? BasePath { get; set; }
        public string? NetworkPath { get; set; }
        public string? NetworkUsername { get; set; }
        public string? NetworkPassword { get; set; }
        public string? EnvironmentName { get; set; }
        public string? ApplicationFormUri { get; set; }
    }
}