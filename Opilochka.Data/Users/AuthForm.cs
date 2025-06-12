using System.ComponentModel.DataAnnotations;

namespace Opilochka.Data.Users
{
    public class AuthForm
    {
        [Required(ErrorMessage = "Электронная почта обязательна к заполнению.")]
        [EmailAddress(ErrorMessage = "Недействительный адрес электронной почты.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен к заполнению.")]
        public string Password { get; set; } = string.Empty;
    }
}
