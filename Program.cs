using JobOnlineAPI.Repositories;
using JobOnlineAPI.Filters;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IApplicantRepository, ApplicantRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

builder.Services.AddSwaggerGen(options =>
{
    options.DocumentFilter<HideSchemasDocumentFilter>();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();