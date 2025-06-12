using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Opilochka.Data
{
    [Table("RefreshTokens")]
    public class RefreshToken : DataItem
    {
        public required string Token { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime Expires {  get; set; }
        public bool Enabled { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}
