namespace Opilochka.Core.Auth
{
    public class JwtOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = "Opilochka.API";
        public string Audience { get; set; } = "Opilochka.Web";
    }
}