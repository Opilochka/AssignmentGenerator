using Microsoft.AspNetCore.Mvc;
using Opilochka.Core.Passwords;
using Opilochka.Core;
using Opilochka.Data.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Opilochka.Core.Auth;


namespace Opilochka.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(ILogger<UserController> logger, 
    OpilochkaDbContext context, 
    IPasswordManager passwordManager,
    IEmailService emailService) : ControllerBase
{
    private readonly ILogger<UserController> _logger = logger;
    private readonly OpilochkaDbContext _context = context;
    private readonly int LENGTH_PASSWORD = 20;
    private readonly IPasswordManager _passwordManager = passwordManager;
    private readonly IEmailService _emailService = emailService;

    [Authorize]
    [HttpGet(Name = "GetUsers")]
    public async Task<List<User>> Get()
    {
        _logger.LogInformation($"Вызван метод GetUsers без указания идентификатора");
        var users = await _context.Users.Where(b => !b.IsDeleted).ToListAsync();
        _logger.LogInformation($"Метод GetUsers возвращает пользователей");
        return users;
    }

    [Authorize]
    [HttpGet("{id}", Name = "GetUser")]
    public async Task<IActionResult> Get(long id)
    {
        _logger.LogInformation("Вызван метод GetUser с идентификатором: {id}", id);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
       
        if (user == null)
        {
            _logger.LogWarning("Пользователь с идентификатором {id} не найден", id);
            return NotFound(
                new
                {
                    Error = $"Пользователь с идентификатором {id} не найден"
                }
            );
        }

        _logger.LogInformation("Пользователь с идентификатором {id} успешно найден", id);
        return Ok(user);
    }

    [Authorize(Roles = RoleJWTData.AdminUserClaimName)]
    [HttpPost(Name = "CreateUser")]
    public async Task<IActionResult> Post([FromBody] UserRequest request)
    {
        _logger.LogInformation("Вызван метод CreateUser с электронной почтой: {Email}", request.Email);

        try
        {
            // 1. Проверка существующего пользователя
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(b => b.Email == request.Email, CancellationToken.None);

            if (existingUser != null)
            {
                _logger.LogWarning("Пользователь с email '{Email}' уже существует", request.Email);
                return BadRequest(new { Error = $"Пользователь с электронной почтой {request.Email} уже существует" });
            }

            _logger.LogInformation("Начинаем создание нового пользователя");

            // 2. Генерация пароля
            _logger.LogDebug("Генерируем временный пароль");
            string password = _passwordManager.GeneratePassword(LENGTH_PASSWORD);
            string passwordHash = _passwordManager.HashPassword(password);
            _logger.LogDebug("Пароль успешно сгенерирован и захэширован");

            // 3. Создаем объект пользователя
            var user = new User
            {
                GroupId = request.GroupId,
                Email = request.Email,
                SecondName = request.SecondName,
                FirstName = request.FirstName,
                Role = request.Role,
                PasswordHash = passwordHash
            };

            _logger.LogInformation("Пользователь подготовлен для сохранения: {Email}", user.Email);

            // 4. Отправка email
            _logger.LogDebug("Начинаем отправку email пользователю {Email}", user.Email);
            _emailService.SendEmailAsync(request.Email, password);
            _logger.LogDebug("Email успешно отправлен пользователю {Email}", user.Email);

            // 5. Сохранение в БД
            _logger.LogDebug("Добавляем пользователя в контекст БД");
            await _context.Users.AddAsync(user, CancellationToken.None);

            _logger.LogDebug("Сохраняем изменения в БД");
            await _context.SaveChangesAsync(CancellationToken.None);

            _logger.LogInformation("Пользователь с email '{Email}' успешно создан с идентификатором: {Id}", request.Email, user.Id);

            return CreatedAtAction(nameof(Post), new { id = user.Id }, user);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Не удалось создать пользователя из-за ошибки базы данных: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка подключения к базе данных" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Произошла неизвестная ошибка при создании пользователя: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка при создании пользователя" });
        }
    }

    [Authorize(Roles = RoleJWTData.AdminUserClaimName)]
    [HttpPut("{id}", Name = "UpdateUser")]
    public async Task<IActionResult> Put(long id, [FromBody] User user)
    {
        _logger.LogInformation("Вызван метод UpdateUser с идентификатором: {id}", id);

        if (user == null)
        {
            _logger.LogWarning("Не получены корректные данные для обновления пользователя с идентификатором: {id}", id);
            return BadRequest(new { Error = "Некорректные данные для обновления пользователя" });
        }

        try
        {
            var updatedRows = await _context.Users
            .Where(b => b.Id == id && !b.IsDeleted)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(b => b.SecondName, user.SecondName)
                 .SetProperty(b => b.FirstName, user.FirstName)
                 .SetProperty(b => b.GroupId, user.GroupId)
                 .SetProperty(b => b.Role, user.Role));

            if (updatedRows > 0)
            {
                _logger.LogInformation("Пользователь с идентификатором {id} успешно обновлен", id);
                return Ok(new { Message = "Пользователь успешно обновлен" });
            }

            _logger.LogWarning("Пользователь с идентификатором {id} не найден или не изменен", id);
            return NotFound(new { Error = $"Пользователь не найден или имя не изменен" });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("Ошибка при обновлении пользователя с идентификатором {id}: {ex.Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка при обновлении пользователя" });
        }
        catch (Exception ex)
        {
            _logger.LogError("Неизвестная ошибка при обновлении пользователя с идентификатором {id}: {Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка" });
        }
    }

    [AllowAnonymous]
    [HttpPost("CreateFirstAdmin")]
    public async Task<IActionResult> CreateFirstAdmin()
    {
        if (_context.Users.Any())
            return BadRequest("Пользователи уже существуют");

        PasswordManager passwordManager = new();
        string password = passwordManager.GeneratePassword(LENGTH_PASSWORD);
        string passwordHash = passwordManager.HashPassword(password);


        var adminUser = new User
        {
            FirstName = "Ольга",
            SecondName = "Валиева",
            Email = "olavalieva67637@gmail.com",
            Role = Core.Enums.Role.Admin,
            PasswordHash = passwordHash
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync();

        return Ok(new { Email = adminUser.Email, Password = password });
    }

    [Authorize(Roles = RoleJWTData.AdminUserClaimName)]
    [HttpDelete("{id}", Name = "DeleteUser")]
    public async Task<IActionResult> Delete(long id)
    {
        _logger.LogInformation("Вызван метод DeleteUser с идентификатором: {id}", id);

        var updatedRows = await _context.Users
            .Where(b => b.Id == id && !b.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, b => true));

        if (updatedRows > 0)
        {
            _logger.LogInformation("Пользователь с идентификатором {id} успешно удален", id);
            return Ok(new { Message = $"Пользователь с идентификатором {id} успешно удален" });
        }

        _logger.LogWarning("Пользователь с идентификатором {id} не найден или уже удален", id);
        return NotFound(new { Error = $"Пользователь с идентификатором {id} не найден" });
    }
}