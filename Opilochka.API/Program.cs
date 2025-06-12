using Opilochka.API;
using Opilochka.API.Extensions;
using Opilochka.Core.Auth;
using Opilochka.Core.Compiler;
using Opilochka.Core.OpenAI;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

//builder.WebHost.UseUrls("http://0.0.0.0:5001");

//var allowedOrigin = "https://app.opilochka.ru/";

//services.AddCors(options =>
//{
//    options.AddPolicy("AllowSpecificOrigin",
//        builder =>
//        {
//            builder.WithOrigins(allowedOrigin)
//                   .AllowAnyHeader()
//                   .AllowAnyMethod();
//        });
//});

services.Configure<JwtOptions>(configuration.GetSection(nameof(JwtOptions)));
services.Configure<CompilerOptions>(configuration.GetSection(nameof(CompilerOptions)));
services.Configure<OpenAIOptions>(configuration.GetSection(nameof(OpenAIOptions)));

builder.Services.AddControllers();
builder.Services.AddApiAuthentication(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiSwaggerGen();
builder.Services.AddDbContext<OpilochkaDbContext>();

services.AddScoped<JwtProvider>();


var app = builder.Build();

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