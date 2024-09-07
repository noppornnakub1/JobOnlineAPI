using JobOnlineAPI.Models;

namespace JobOnlineAPI.Services
{
    public interface IUserService
    {
        Task<AdminUser?> AuthenticateAsync(string username, string password);
    }
}
