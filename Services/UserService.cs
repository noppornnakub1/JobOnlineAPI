using System.Threading.Tasks;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;

namespace JobOnlineAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IAdminRepository _adminRepository;

        public UserService(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public async Task<AdminUser?> AuthenticateAsync(string username, string password)
        {
            var user = await _adminRepository.GetAdminUserByUsernameAsync(username);

            var isPasswordMatched = BCrypt.Net.BCrypt.Verify(password, user.Password);
            Console.WriteLine($"Password Match: {isPasswordMatched}");

            if (user != null && isPasswordMatched)
            {
                return user;
            }
            else
            {
                Console.WriteLine("Password did not match!");
            }


            return null;
        }

        public async Task<string?> GetConfigValueAsync(string key)
        {
            return await _adminRepository.GetConfigValueAsync(key);
        }

        public async Task<string?> GetStyleValueAsync(string key)
        {
            return await _adminRepository.GetStyleValueAsync(key);
        }
    }
}