using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Opilochka.Web.Services.Auth;

namespace Opilochka.Web.Components.Pages.Auth
{
    public class LoginBase : ComponentBase
    {
        [Inject] public NavigationManager? NavigationManager { get; set; }

        [Inject] public AuthService AuthService { get; set; } = default!;

        [Inject] public ILogger<LoginBase> Logger { get; set; } = default!;

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ErrorMessage {  get; set; } = string.Empty;

        public async Task OnLogin()
        {
            ErrorMessage = "";
            Logger.LogInformation("Попытка входа с электронной почтой: {Email}", Email);
            try
            {
                var status = await AuthService.Login(Email, Password);
                if (status)
                {
                    Logger.LogInformation("Вход выполнен успешно для пользователя: {Email}", Email);
                    NavigationManager?.NavigateTo("/");
                }
                else
                {
                    ErrorMessage = "Неверные учетные данные. Пожалуйста, попробуйте еще раз";
                }                
            }
            catch (JSDisconnectedException ex)
            {
                Logger.LogWarning("Не удалось выполнить JavaScript вызов: {Message}", ex.Message);
                ErrorMessage = "Не удалось выполнить вход. Пожалуйста, попробуйте позже";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Произошла ошибка при входе: {ex.Message}";
                Logger.LogError(ex, "Ошибка при входе для пользователя: {Email}", Email);
            }
        }
    }
}
