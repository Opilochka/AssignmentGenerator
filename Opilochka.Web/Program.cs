using Microsoft.AspNetCore.Components.Authorization;
using Opilochka.Web.Components;
using Opilochka.Web.Security;
using Opilochka.Web.Services;
using Opilochka.Web.Services.Admin;
using Opilochka.Web.Services.Auth;
using Opilochka.Web.Services.Student;
using Opilochka.Web.Services.Teacher;


var builder = WebApplication.CreateBuilder(args);

// Добавление необходимых сервисов
builder.Services.AddHttpClient();
builder.Services.AddScoped<CookieService>();
builder.Services.AddScoped<AccessTokenService>();
builder.Services.AddScoped<RefreshTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<APIService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<LessonService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<CompilerService>();
builder.Services.AddScoped<AnswerService>();
builder.Services.AddScoped<ResultCheckService>();
builder.Services.AddScoped<HttpRequestHandler>();

// Конфигурация HttpClient для API
builder.Services.AddHttpClient("ApiClient", options =>
{
    options.BaseAddress = new Uri("http://localhost:60001/api/");
});


// Регистрация Razor компонентов
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Настройка аутентификации и авторизации
builder.Services.AddAuthorization();
builder.Services.AddAuthentication()
    .AddScheme<CustomOption, JWTAuthenticationHandler>(
        "JWTAuth", options => { }
    );

builder.Services.AddScoped<JWTAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, JWTAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", true);
    app.UseHsts(); 
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

