namespace Opilochka.Web.Services.Auth
{
    public class AccessTokenService
    {
        private readonly CookieService _cookieService;
        private readonly string _tokenKey = "access_token";

        public AccessTokenService(CookieService cookieService)
        {
            _cookieService = cookieService;
        }

        public async Task SetToken(string accessToken)
        {
            await _cookieService.Set(_tokenKey, accessToken, 1);
        }

        public async Task RemoveToken()
        {
            await _cookieService.Remove(_tokenKey);
        }

        public async Task<string?> GetToken()
        {
            return await _cookieService.Get(_tokenKey);
        }
    }
}
