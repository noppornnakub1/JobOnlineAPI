namespace JobOnlineAPI.Services
{
    public interface INetworkShareService
    {
        Task<bool> ConnectAsync();
        void Disconnect();
        string GetBasePath();
    }
}