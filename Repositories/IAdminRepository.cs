using JobOnlineAPI.Models;

namespace JobOnlineAPI.Repositories
{
    public interface IAdminRepository
    {
        Task<int> AddAdminUserAsync(AdminUser admin);
        Task<bool> VerifyPasswordAsync(string username, string password);
        Task<AdminUser> GetAdminUserByUsernameAsync(string username);
    }
}