using Microsoft.EntityFrameworkCore;
using Opilochka.API;
using Opilochka.API.Extensions;
using Opilochka.Core.Auth;
using Opilochka.Core.Compiler;
using Opilochka.Core.OpenAI;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Добавление конфигурации из appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Добавление конфигурации из переменных окружения
builder.Configuration.AddEnvironmentVariables();

// Настройка строки подключения
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (!string.IsNullOrEmpty(connectionString))
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
}

// Настройка параметров JWT
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
if (!string.IsNullOrEmpty(jwtSecretKey))
{
    builder.Configuration["JwtOptions:SecretKey"] = "opilochka_topsecret123!opilochka_topsecret123!";
}

var jwtExpiresHours = Environment.GetEnvironmentVariable("JWT_EXPIRES_HOURS");
if (!string.IsNullOrEmpty(jwtExpiresHours))
{
    builder.Configuration["JwtOptions:ExpiresHours"] = "12";
}

// Настройка параметров компилятора
var compilerClientId = Environment.GetEnvironmentVariable("COMPILER_CLIENT_ID");
if (!string.IsNullOrEmpty(compilerClientId))
{
    builder.Configuration["CompilerOptions:ClientId"] = compilerClientId;
}

var compilerClientSecret = Environment.GetEnvironmentVariable("COMPILER_CLIENT_SECRET");
if (!string.IsNullOrEmpty(compilerClientSecret))
{
    builder.Configuration["CompilerOptions:ClientSecret"] = compilerClientSecret;
}

var compilerUrl = Environment.GetEnvironmentVariable("COMPILER_URL");
if (!string.IsNullOrEmpty(compilerUrl))
{
    builder.Configuration["CompilerOptions:URL"] = compilerUrl;
}

// Настройка параметров OpenAI
var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
if (!string.IsNullOrEmpty(openAiApiKey))
{
    builder.Configuration["OpenAIOptions:ApiKey"] = openAiApiKey;
}

var openAiUrl = Environment.GetEnvironmentVariable("OPENAI_URL");
if (!string.IsNullOrEmpty(openAiUrl))
{
    builder.Configuration["OpenAIOptions:URL"] = openAiUrl;
}

services.Configure<JwtOptions>(builder.Configuration.GetSection(nameof(JwtOptions)));
services.Configure<CompilerOptions>(builder.Configuration.GetSection(nameof(CompilerOptions)));
services.Configure<OpenAIOptions>(builder.Configuration.GetSection(nameof(OpenAIOptions)));

builder.Services.AddControllers();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiSwaggerGen();
builder.Services.AddDbContext<OpilochkaDbContext>();

builder.Services.AddScoped<JwtProvider>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OpilochkaDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigin");


app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();