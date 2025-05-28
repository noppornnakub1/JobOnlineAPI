using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace JobOnlineAPI.Filters
{
    [AttributeUsage(AttributeTargets.All)]
    public class JwtAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtAuthorizeAttribute>>();

            string? authHeader = context.HttpContext.Request.Headers.Authorization;
            if (string.IsNullOrEmpty(authHeader))
            {
                logger.LogWarning("AccessToken cannot be empty");
                context.Result = new BadRequestObjectResult(new { message = "AccessToken cannot be empty" });
                return;
            }

            string token = authHeader.StartsWith("Bearer ") ? authHeader[7..].Trim() : authHeader;
            if (token.Count(c => c == '.') != 2)
            {
                context.Result = new BadRequestObjectResult(new { message = "Invalid token format. Expected JWS format (header.payload.signature)" });
                return;
            }

            try
            {
                var jwtSettings = configuration.GetSection("JwtSettings");
                var jwtSecret = jwtSettings["AccessSecret"] ?? throw new InvalidOperationException("JwtSettings:AccessSecret is missing.");
                var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is missing.");
                var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is missing.");
                var sqlServerConnectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection connection string is missing.");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(subClaim))
                {
                    logger.LogWarning("No 'sub' claim found in the token");
                    context.Result = new BadRequestObjectResult(new { message = "Invalid token: No 'sub' claim found" });
                    return;
                }

                var parts = subClaim.Split('|');
                if (parts.Length != 2)
                {
                    logger.LogWarning("Invalid 'sub' claim format: {Sub}", subClaim);
                    context.Result = new BadRequestObjectResult(new { message = "Invalid token: 'sub' claim format is incorrect" });
                    return;
                }

                string cn = parts[0];
                string samAccountName = parts[1];

            }
            catch (SecurityTokenMalformedException ex)
            {
                logger.LogWarning("Invalid token format: {Message}", ex.Message);
                context.Result = new BadRequestObjectResult(new { message = "Invalid token format: Token must be a valid JWT with correct segments" });
                return;
            }
            catch (SecurityTokenException ex)
            {
                logger.LogWarning("Token validation failed: {Message}", ex.Message);
                context.Result = new UnauthorizedObjectResult(new { message = "Invalid or expired token", error = ex.Message });
                return;
            }

            await next();
        }
    }
}