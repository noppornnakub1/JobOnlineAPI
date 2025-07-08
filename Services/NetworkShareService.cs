using System.Runtime.InteropServices;
using JobOnlineAPI.Models;
using Microsoft.Extensions.Options;

namespace JobOnlineAPI.Services
{
    public class NetworkShareService : INetworkShareService
    {
        private readonly FileStorageConfig _fileStorageConfig;
        private readonly StorageConfig _currentStorageConfig;
        private readonly ILogger<NetworkShareService> _logger;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NetResource
        {
            public int dwScope;
            public int dwType;
            public int dwDisplayType;
            public int dwUsage;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpLocalName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpRemoteName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpComment;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpProvider;
        }

        [DllImport("mpr.dll", EntryPoint = "WNetAddConnection2W", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NetResource netResource, string? password, string? username, int flags);

        [DllImport("mpr.dll", EntryPoint = "WNetCancelConnection2W", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string lpName, int dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fForce);

        public NetworkShareService(IOptions<FileStorageConfig> config, ILogger<NetworkShareService> logger)
        {
            _fileStorageConfig = config.Value ?? throw new ArgumentNullException(nameof(config));
            _logger = logger;

            bool isProduction = _fileStorageConfig.EnvironmentName?.Equals("Production", StringComparison.OrdinalIgnoreCase) ?? false;
            string basePath = isProduction
                ? _fileStorageConfig.ProductionPath ?? throw new InvalidOperationException("ProductionPath is not configured.")
                : _fileStorageConfig.NetworkPath ?? throw new InvalidOperationException("NetworkPath is not configured.");

            _currentStorageConfig = new StorageConfig
            {
                BasePath = basePath,
                UseNetworkShare = !isProduction && !string.IsNullOrEmpty(_fileStorageConfig.NetworkPath) &&
                                  !string.IsNullOrEmpty(_fileStorageConfig.NetworkUsername) &&
                                  !string.IsNullOrEmpty(_fileStorageConfig.NetworkPassword),
                Username = isProduction ? null : _fileStorageConfig.NetworkUsername,
                Password = isProduction ? null : _fileStorageConfig.NetworkPassword
            };

            if (!_currentStorageConfig.UseNetworkShare && !Directory.Exists(_currentStorageConfig.BasePath))
            {
                Directory.CreateDirectory(_currentStorageConfig.BasePath);
                _logger.LogInformation("Created local directory: {BasePath}", _currentStorageConfig.BasePath);
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (!_currentStorageConfig.UseNetworkShare)
                return CheckLocalStorage();

            const int maxRetries = 3;
            const int retryDelayMs = 2000;
            string serverName = $"\\\\{new Uri(_currentStorageConfig.BasePath).Host}";

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Attempt {Attempt}/{MaxRetries}: Connecting to {BasePath}", attempt, maxRetries, _currentStorageConfig.BasePath);
                    DisconnectExistingConnections(serverName);

                    bool connected = AttemptNetworkConnection();
                    if (!connected)
                    {
                        _logger.LogWarning("Connection attempt failed, retrying...");
                        continue;
                    }

                    if (!Directory.Exists(_currentStorageConfig.BasePath))
                    {
                        Directory.CreateDirectory(_currentStorageConfig.BasePath);
                        _logger.LogInformation("Created directory: {BasePath}", _currentStorageConfig.BasePath);
                    }

                    ValidateNetworkShare();
                    _logger.LogInformation("Successfully connected to network share: {BasePath}", _currentStorageConfig.BasePath);
                    return true;
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "Failed to connect after {MaxRetries} attempts. Falling back to local path.", maxRetries);
                        FallbackToLocalPath();
                        return false;
                    }
                    _logger.LogWarning(ex, "Retrying after delay for {BasePath}", _currentStorageConfig.BasePath);
                    await Task.Delay(retryDelayMs);
                }
            }

            return false;
        }

        public void Disconnect()
        {
            if (!_currentStorageConfig.UseNetworkShare)
                return;

            try
            {
                string serverName = $"\\\\{new Uri(_currentStorageConfig.BasePath).Host}";
                DisconnectPath(_currentStorageConfig.BasePath);
                DisconnectPath(serverName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from network share: {Message}", ex.Message);
            }
        }

        public string GetBasePath() => _currentStorageConfig.BasePath;

        private bool CheckLocalStorage()
        {
            if (Directory.Exists(_currentStorageConfig.BasePath))
            {
                _logger.LogInformation("Using local storage at {BasePath}", _currentStorageConfig.BasePath);
                return true;
            }
            _logger.LogError("Local path {BasePath} does not exist.", _currentStorageConfig.BasePath);
            throw new DirectoryNotFoundException($"Local path {_currentStorageConfig.BasePath} is not accessible.");
        }

        private void DisconnectExistingConnections(string serverName)
        {
            DisconnectPath(_currentStorageConfig.BasePath);
            DisconnectPath(serverName);
        }

        private void DisconnectPath(string path)
        {
            int result = WNetCancelConnection2(path, 0, true);
            if (result != 0 && result != 1219)
            {
                _logger.LogWarning("Failed to disconnect {Path}: {ErrorMessage} (Error Code: {Result})", path, new System.ComponentModel.Win32Exception(result).Message, result);
            }
        }

        private bool AttemptNetworkConnection()
        {
            NetResource netResource = new()
            {
                dwType = 1,
                lpRemoteName = _currentStorageConfig.BasePath,
                lpLocalName = null,
                lpProvider = null
            };

            int result = WNetAddConnection2(ref netResource, _currentStorageConfig.Password, _currentStorageConfig.Username, 0);
            if (result == 0)
            {
                _logger.LogInformation("Successfully connected to {BasePath}", _currentStorageConfig.BasePath);
                return true;
            }

            _logger.LogError("Failed to connect to {BasePath}: {ErrorMessage} (Error Code: {Result})", _currentStorageConfig.BasePath, new System.ComponentModel.Win32Exception(result).Message, result);
            return false;
        }

        private void ValidateNetworkShare()
        {
            if (!Directory.Exists(_currentStorageConfig.BasePath))
            {
                _logger.LogError("Network share {BasePath} does not exist.", _currentStorageConfig.BasePath);
                throw new DirectoryNotFoundException($"Network share {_currentStorageConfig.BasePath} is not accessible.");
            }
        }

        private void FallbackToLocalPath()
        {
            _currentStorageConfig.BasePath = _fileStorageConfig.ProductionPath;
            _currentStorageConfig.UseNetworkShare = false;
            _currentStorageConfig.Username = null;
            _currentStorageConfig.Password = null;

            if (!Directory.Exists(_currentStorageConfig.BasePath))
            {
                Directory.CreateDirectory(_currentStorageConfig.BasePath);
                _logger.LogInformation("Created fallback directory: {BasePath}", _currentStorageConfig.BasePath);
            }
        }
    }

    internal class StorageConfig
    {
        public required string BasePath { get; set; }
        public bool UseNetworkShare { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}