using Opilochka.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Opilochka.Data.Users
{
    public class UserRequest
    {
        [Required]
        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("secondName")]
        public string SecondName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("groupId")]
        public long? GroupId { get; set; }

        [Required]
        public Role Role { get; set; }
    }
}
