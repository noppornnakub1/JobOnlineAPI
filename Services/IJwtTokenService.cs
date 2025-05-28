using JobOnlineAPI.Controllers;

namespace JobOnlineAPI.Services
{
    public interface IJwtTokenService
    {
        string GenerateJwtToken(UserAdminModel user);
        string GenerateJwtToken(UserModel user);
    }
}