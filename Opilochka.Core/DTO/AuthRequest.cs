using System.ComponentModel.DataAnnotations;

namespace Opilochka.Core.ViewModels
{
    public class AuthRequest
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
