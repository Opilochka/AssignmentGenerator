using Microsoft.AspNetCore.Mvc;
using Opilochka.Core.Auth;
using Opilochka.Core.Passwords;
using Opilochka.Core.ViewModels;
using Opilochka.Data;
using Opilochka.Data.Users;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(OpilochkaDbContext context, JwtProvider jwtProvider, ILogger<AuthController> logger) : Controller
    {
        private readonly ILogger<AuthController> _logger = logger;
        private readonly OpilochkaDbContext _context = context;
        private readonly JwtProvider _jwtProvider = jwtProvider;

        [HttpPost("login")]
        public ActionResult<AuthResponse> Login(AuthRequest request)
        {
            _logger.LogInformation("Метод Post вызван с email: {Email}", request.Email);

            AuthResponse response = new();
            PasswordManager passwordManager = new();

            var user = _context.Users.FirstOrDefault(b => b.Email == request.Email);
            if (user == null)
            {
                _logger.LogWarning("Пользователь не найден для email: {Email}", request.Email);
                return BadRequest("Пользователь не найден!");
            }

            _logger.LogInformation("Пользователь найден для email: {Email}", request.Email);

            var verifyPassword = user.PasswordHash == passwordManager.HashPassword(request.Password);

            if (!verifyPassword)
            {
                _logger.LogWarning("Неправильная попытка ввода пароля для email: {Email}", request.Email);
                return BadRequest("Неправильный пароль");
            }

            _logger.LogInformation("Пароль подтвержден для email: {Email}", request.Email);

            var token = _jwtProvider.GenerateToken(user);
            response.AccessToken = token.AccessToken;
            response.RefreshToken = token.RefreshToken.Token;

            _logger.LogInformation("JWT токен сгенерирован для email: {Email}", request.Email);

            InsertRefreshToken(token.RefreshToken, request.Email);
            _logger.LogInformation("Refresh токен вставлен для email: {Email}", request.Email);

            DisableUserTokenByEmail(request.Email);
            _logger.LogInformation("Старые токены отключены для email: {Email}", request.Email);

            _logger.LogInformation("Метод Post успешно завершен для email: {Email}", request.Email);
            return Ok(response);
        }


        [HttpPost("refresh")]
        public ActionResult<AuthResponse> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshtoken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Отсутствует refresh token в cookies.");
                return BadRequest("Отсутствует refresh token.");
            }

            var isValid = IsRefreshTokenValid(refreshToken);
            if (!isValid)
            {
                _logger.LogWarning("Недействительный refresh token.");
                return BadRequest("Недействительный refresh token.");
            }

            var currentUser = FindUserByToken(refreshToken);
            if (currentUser == null)
            {
                _logger.LogWarning("Пользователь не найден по refresh token.");
                return BadRequest("Пользователь не найден.");
            }

            var token = _jwtProvider.GenerateToken(currentUser);
            var response = new AuthResponse
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken.Token
            };

            DisableUserToken(refreshToken);
            InsertRefreshToken(token.RefreshToken, currentUser.Email);

            _logger.LogInformation("Метод RefreshToken успешно завершен. Новый токен сгенерирован для пользователя: {Email}", currentUser.Email);
            return Ok(response);
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshtoken"];

                if (refreshToken != null)
                {
                    DisableUserToken(refreshToken);
                    _logger.LogInformation("Пользователь успешно вышел из системы, токен отключен");
                }
                else
                {
                    _logger.LogWarning("Попытка выхода без действующего refresh токена");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя: {Message}", ex.Message);
                return StatusCode(500, "Произошла ошибка при выходе из системы");
            }
        }

        private bool IsRefreshTokenValid(string token)
        {
            var result = _context.RefreshTokens
                    .Where(rt => rt.Token == token  && rt.Expires >= DateTime.UtcNow)
                    .Count();

            return result > 0;
        }
        private User? FindUserByToken(string token)
        {
            var user = (from rt in _context.RefreshTokens
                        join u in _context.Users on rt.Email equals u.Email
                        where rt.Token == token
                        select u).FirstOrDefault();

            return user;
        }
        private bool DisableUserTokenByEmail(string email)
        {
            var tokens = _context.RefreshTokens.Where(rt => rt.Email == email).ToList();

            if (tokens.Count == 0)
                return false;

            foreach (var token in tokens)
            {
                token.Enabled = false;
            }

            _context.SaveChanges();
            return true;
        }
        private bool DisableUserToken(string token)
        {
            var refreshToken = _context.RefreshTokens.FirstOrDefault(rt => rt.Token == token);

            if (refreshToken == null)
                return false;

            refreshToken.Enabled = false;
            _context.SaveChanges();
            return true;
        }
        private bool InsertRefreshToken(RefreshToken refreshToken, string email)
        {
            refreshToken.Email = email;

            if (refreshToken.CreatedDate.Kind != DateTimeKind.Utc)
            {
                refreshToken.CreatedDate = refreshToken.CreatedDate.ToUniversalTime();
            }

            if (refreshToken.Expires.Kind != DateTimeKind.Utc)
            {
                refreshToken.Expires = refreshToken.Expires.ToUniversalTime();
            }

            _context.RefreshTokens.Add(refreshToken);
            var result = _context.SaveChanges();

            return result > 0;
        }
    }
}
