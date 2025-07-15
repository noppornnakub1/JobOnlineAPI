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
using Microsoft.Extensions.Options;
using Rotativa.AspNetCore;
using Microsoft.Extensions.FileProviders;
using JobOnlineAPI.Filters;
using OfficeOpenXml;

var options = new WebApplicationOptions
{
    WebRootPath = "public",
    ContentRootPath = Directory.GetCurrentDirectory()
};
var builder = WebApplication.CreateBuilder(options);

builder.Services.AddScoped<ITRequestExampleOperationFilter, ITRequestExampleOperationFilter>();

using var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});
var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("Configuration sources:");
foreach (var source in builder.Configuration.Sources)
{
    logger.LogInformation(" - {Source}", source.GetType().Name);
}

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    logger.LogError("DefaultConnection string is missing or empty.");
    throw new InvalidOperationException("DefaultConnection string is missing or empty.");
}
logger.LogInformation("DefaultConnection: {ConnectionString}", connectionString);

var fileStorageConfig = builder.Configuration.GetSection("FileStorage").Get<FileStorageConfig>();
if (fileStorageConfig == null || string.IsNullOrEmpty(fileStorageConfig.BasePath))
{
    logger.LogError("FileStorage configuration is missing or BasePath is not set.");
    throw new InvalidOperationException("FileStorage configuration is missing or BasePath is not set.");
}
logger.LogInformation("FileStorage BasePath: {BasePath}", fileStorageConfig.BasePath);

var fullPath = Path.Combine(builder.Environment.ContentRootPath, fileStorageConfig.BasePath);
logger.LogInformation("Resolved FileStorage FullPath: {FullPath}", fullPath);
if (!Directory.Exists(fullPath))
{
    Directory.CreateDirectory(fullPath);
    logger.LogInformation("Created FileStorage directory: {Path}", fullPath);
}

var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
if (emailSettings == null || string.IsNullOrEmpty(emailSettings.SmtpServer))
{
    logger.LogError("EmailSettings configuration is missing or SmtpServer is not set.");
    throw new InvalidOperationException("EmailSettings configuration is missing or SmtpServer is not set.");
}
logger.LogInformation("EmailSettings SmtpServer: {SmtpServer}", emailSettings.SmtpServer);

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<DapperContextHrms>();
builder.Services.AddSingleton<DapperContext>();

AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<IHRStaffRepository, HRStaffRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<ILdapService, LdapService>();
builder.Services.AddScoped<IConsentService, ConsentService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<INetworkShareService, NetworkShareService>();
builder.Services.AddScoped<FileProcessingService>();
builder.Services.Configure<FileStorageConfig>(
    builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddSingleton(resolver =>
    resolver.GetRequiredService<IOptions<FileStorageConfig>>().Value);

builder.Services.AddScoped<IDbConnection>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        sp.GetRequiredService<ILogger<Program>>().LogError("Database connection string is not configured.");
        throw new InvalidOperationException("Database connection string is not configured.");
    }
    return new SqlConnection(connectionString);
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtKey = builder.Configuration["JwtSettings:AccessSecret"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        logger.LogError("JWT AccessSecret is not configured.");
        throw new InvalidOperationException("JWT AccessSecret is not configured.");
    }
    var issuer = builder.Configuration["JwtSettings:Issuer"];
    var audience = builder.Configuration["JwtSettings:Audience"];
    if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
    {
        logger.LogError("JWT Issuer or Audience is not configured.");
        throw new InvalidOperationException("JWT Issuer or Audience is not configured.");
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "JobOnlineAPI", Version = "v1" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "file",
        Format = "binary"
    });
    c.OperationFilter<ITRequestExampleOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    c.UseAllOfToExtendReferenceSchemas();
});

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "public")),
    RequestPath = ""
});

try
{
    RotativaConfiguration.Setup(app.Environment.ContentRootPath, "Rotativa");
    logger.LogInformation("Rotativa configured successfully.");
}
catch (Exception ex)
{
    logger.LogError("Rotativa configuration failed: {Message}", ex.Message);
    throw;
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            var error = new { Error = "An unexpected error occurred. Please try again later." };
            await context.Response.WriteAsJsonAsync(error);
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
            logger.LogError(exception, "An error occurred: {Error}, Path: {Path}, StackTrace: {StackTrace}",
                exception?.Message, exception?.TargetSite, exception?.StackTrace);
        });
    });
}

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

await app.RunAsync();