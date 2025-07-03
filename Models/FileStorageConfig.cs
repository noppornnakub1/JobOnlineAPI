namespace JobOnlineAPI.Models
{
    public class FileStorageConfig
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ProductionPath { get; set; } = string.Empty;
        public string BasePath { get; set; } = string.Empty;
        public string? NetworkPath { get; set; }
        public string? NetworkUsername { get; set; }
        public string? NetworkPassword { get; set; }
        public string? ApplicationFormUri { get; set; }
    }
}