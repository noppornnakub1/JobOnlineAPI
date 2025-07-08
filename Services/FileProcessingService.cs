namespace JobOnlineAPI.Services
{
    public class FileProcessingService(
        INetworkShareService networkShareService,
        ILogger<FileProcessingService> logger)
    {
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly ILogger<FileProcessingService> _logger = logger;

        public async Task<List<Dictionary<string, object>>> ProcessFilesAsync(IFormFileCollection files)
        {
            var fileMetadatas = new List<Dictionary<string, object>>();
            if (files == null || files.Count == 0)
                return fileMetadatas;

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg" };
            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    _logger.LogWarning("Skipping empty file: {FileName}", file.FileName);
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"Invalid file type for {file.FileName}. Only PNG, JPG, PDF, DOC, and DOCX are allowed.");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(_networkShareService.GetBasePath(), fileName);
                var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Invalid directory path for: {filePath}");

                try
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
                    throw;
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                fileMetadatas.Add(new Dictionary<string, object>
                {
                    { "FilePath", filePath.Replace('\\', '/') },
                    { "FileName", fileName },
                    { "FileSize", file.Length },
                    { "FileType", file.ContentType }
                });
            }

            return fileMetadatas;
        }

        public void MoveFilesToApplicantDirectory(int applicantId, List<Dictionary<string, object>> fileMetadatas)
        {
            if (fileMetadatas.Count == 0 || applicantId <= 0)
                return;

            var applicantPath = Path.Combine(_networkShareService.GetBasePath(), $"applicant_{applicantId}");
            if (!Directory.Exists(applicantPath))
            {
                try
                {
                    Directory.CreateDirectory(applicantPath);
                    _logger.LogInformation("Created applicant directory: {ApplicantPath}", applicantPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create applicant directory: {ApplicantPath}", applicantPath);
                    throw;
                }
            }
            else
            {
                foreach (var oldFile in Directory.GetFiles(applicantPath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFile);
                        _logger.LogInformation("Deleted old file: {OldFile}", oldFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old file: {OldFile}", oldFile);
                    }
                }
            }

            foreach (var metadata in fileMetadatas)
            {
                var oldFilePath = metadata.GetValueOrDefault("FilePath")?.ToString();
                var fileName = metadata.GetValueOrDefault("FileName")?.ToString();
                if (string.IsNullOrEmpty(oldFilePath) || string.IsNullOrEmpty(fileName))
                {
                    _logger.LogWarning("Skipping file with invalid metadata: {Metadata}", System.Text.Json.JsonSerializer.Serialize(metadata));
                    continue;
                }

                var newFilePath = Path.Combine(applicantPath, fileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Move(oldFilePath, newFilePath, overwrite: true);
                        _logger.LogInformation("Moved file from {OldFilePath} to {NewFilePath}", oldFilePath, newFilePath);
                        metadata["FilePath"] = newFilePath.Replace('\\', '/');
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to move file from {OldFilePath} to {NewFilePath}", oldFilePath, newFilePath);
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("File not found for moving: {OldFilePath}", oldFilePath);
                }
            }
        }
    }
}