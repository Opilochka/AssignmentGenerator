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
        _logger.LogInformation($"������ ����� GetUsers ��� �������� ��������������");
        var users = await _context.Users.Where(b => !b.IsDeleted).ToListAsync();
        _logger.LogInformation($"����� GetUsers ���������� �������������");
        return users;
    }

    [Authorize]
    [HttpGet("{id}", Name = "GetUser")]
    public async Task<IActionResult> Get(long id)
    {
        _logger.LogInformation("������ ����� GetUser � ���������������: {id}", id);
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
       
        if (user == null)
        {
            _logger.LogWarning("������������ � ��������������� {id} �� ������", id);
            return NotFound(
                new
                {
                    Error = $"������������ � ��������������� {id} �� ������"
                }
            );
        }

        _logger.LogInformation("������������ � ��������������� {id} ������� ������", id);
        return Ok(user);
    }

    [Authorize(Roles = RoleJWTData.AdminUserClaimName)]
    [HttpPost(Name = "CreateUser")]
    public async Task<IActionResult> Post([FromBody] UserRequest request)
    {
        _logger.LogInformation("������ ����� CreateUser � ����������� ������: {request.Email}", request.Email);

        var existingUser = await _context.Users.FirstOrDefaultAsync(b => b.Email == request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { Error = $"������������ � ����������� ������ {request.Email} ��� ����������" });
        }

        try
        {
            string password = _passwordManager.GeneratePassword(LENGTH_PASSWORD);
            string passwordHash = _passwordManager.HashPassword(password);

            var user = new User
            {
                GroupId = request.GroupId,
                Email = request.Email,
                SecondName = request.SecondName,
                FirstName = request.FirstName,
                Role = request.Role,
                PasswordHash = passwordHash
            };

            _emailService.SendEmail(request.Email, password);

            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            _logger.LogInformation("������������ � email '{Email}' ������� ������ � ���������������: {Id}", request.Email, user.Id);
            return CreatedAtAction(nameof(Post), new { id = user.Id }, user);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("�� ������� ������� ������������, �������� ����������: {ex.Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "������ �����������" });
        }
        catch (Exception ex)
        {
            _logger.LogError("����������� ������: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "��������� ������ ��� �������� ������������" });
        }
    }

    [Authorize(Roles = RoleJWTData.AdminUserClaimName)]
    [HttpPut("{id}", Name = "UpdateUser")]
    public async Task<IActionResult> Put(long id, [FromBody] User user)
    {
        _logger.LogInformation("������ ����� UpdateUser � ���������������: {id}", id);

        if (user == null)
        {
            _logger.LogWarning("�� �������� ���������� ������ ��� ���������� ������������ � ���������������: {id}", id);
            return BadRequest(new { Error = "������������ ������ ��� ���������� ������������" });
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
                _logger.LogInformation("������������ � ��������������� {id} ������� ��������", id);
                return Ok(new { Message = "������������ ������� ��������" });
            }

            _logger.LogWarning("������������ � ��������������� {id} �� ������ ��� �� �������", id);
            return NotFound(new { Error = $"������������ �� ������ ��� ��� �� �������" });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("������ ��� ���������� ������������ � ��������������� {id}: {ex.Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "������ ��� ���������� ������������" });
        }
        catch (Exception ex)
        {
            _logger.LogError("����������� ������ ��� ���������� ������������ � ��������������� {id}: {Message}", id, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "��������� ������" });
        }
    }

    [AllowAnonymous]
    [HttpPost("CreateFirstAdmin")]
    public async Task<IActionResult> CreateFirstAdmin()
    {
        if (_context.Users.Any())
            return BadRequest("������������ ��� ����������");

        PasswordManager passwordManager = new();
        string password = passwordManager.GeneratePassword(LENGTH_PASSWORD);
        string passwordHash = passwordManager.HashPassword(password);


        var adminUser = new User
        {
            FirstName = "�����",
            SecondName = "�������",
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
        _logger.LogInformation("������ ����� DeleteUser � ���������������: {id}", id);

        var updatedRows = await _context.Users
            .Where(b => b.Id == id && !b.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, b => true));

        if (updatedRows > 0)
        {
            _logger.LogInformation("������������ � ��������������� {id} ������� ������", id);
            return Ok(new { Message = $"������������ � ��������������� {id} ������� ������" });
        }

        _logger.LogWarning("������������ � ��������������� {id} �� ������ ��� ��� ������", id);
        return NotFound(new { Error = $"������������ � ��������������� {id} �� ������" });
    }
}