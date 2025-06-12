using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Opilochka.Web.Services.Auth
{
    public class RefreshTokenService
    {
        private readonly ProtectedLocalStorage _storage;
        private readonly string key = "refresh_token";

        public RefreshTokenService(ProtectedLocalStorage storage)
        {
            _storage = storage;
        }

        public async Task Set(string value)
        {
            await _storage.SetAsync(key, value);
        }

        public async Task<string?> Get()
        {
            var result = await _storage.GetAsync<string>(key);
            if (result.Success) return result.Value;

            return string.Empty;
        }

        internal async Task Remove()
        {
            await _storage.DeleteAsync(key);
        }
    }
}
