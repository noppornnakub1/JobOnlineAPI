using System.Data;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using JobOnlineAPI.Repositories;
using JobOnlineAPI.Services;
using JobOnlineAPI.Models;
using JobOnlineAPI.DAL;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<DapperContextHrms>();
builder.Services.AddSingleton<DapperContext>();

AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

builder.Services.AddScoped<IApplicantRepository, ApplicantRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IHRStaffRepository, HRStaffRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IDbConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSecretKey"))
    };
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "JobOnlineAPI", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.DefaultModelsExpandDepth(-1);
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "JobOnlineAPI v1");
});

app.UseHttpsRedirection();
app.UseCors("AllowAllOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();