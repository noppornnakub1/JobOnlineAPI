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
            var adminUser = await _adminRepository.GetAdminUserByUsernameAsync(username);

            if (adminUser != null)
            {
                var isPasswordMatched = BCrypt.Net.BCrypt.Verify(password, adminUser.Password);
                if (isPasswordMatched)
                {
                    return adminUser;
                }
            }

            var user = await _adminRepository.GetUserByEmailAsync(username);

            if (user != null)
            {
                bool isPasswordMatched;
                if (user.PasswordHash.StartsWith("$2"))
                {
                    isPasswordMatched = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                else
                {
                    isPasswordMatched = _adminRepository.VerifySHA256Hash(password, user.PasswordHash);
                }

                if (isPasswordMatched)
                {
                    return new AdminUser
                    {
                        Username = user.Email,
                        Password = user.PasswordHash,
                        Role = "User"
                    };
                }
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