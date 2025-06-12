using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opilochka.Core.Auth;
using Opilochka.Data.StudyGroups;
using Opilochka.Data.Users;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class GroupController(ILogger<GroupController> logger, OpilochkaDbContext context) : Controller
    {
        private readonly ILogger<GroupController> _logger = logger;
        private readonly OpilochkaDbContext _context = context;

        [Authorize]
        [HttpGet(Name = "GetGroups")]
        public async Task<List<StudyGroup>> Get()
        {
            _logger.LogInformation("Вызван метод GetGroups без указания идентификатора");
            var groups = await _context.StudyGroups.Where(b => !b.IsDeleted).ToListAsync();
            _logger.LogInformation($"Метод GetGroups возвращает группы");
            return groups;
        }

        [Authorize]
        [HttpGet("{id}", Name = "GetGroup")]
        public async Task<IActionResult> Get(long id)
        {
            _logger.LogInformation("Вызван метод GetGroup с идентификатором: {id}", id);
            var group = await _context.StudyGroups.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (group == null)
            {
                _logger.LogWarning("Группа с идентификатором {id} не найдена", id);
                return NotFound(
                    new
                    {
                        Error = $"Группа с идентификатором {id} не найдена"
                    }
                );
            }

            _logger.LogInformation("Группа с идентификатором {id} успешно найдена", id);
            return Ok(group);
        }

        [Authorize]
        [HttpGet("{id}/Users", Name = "GetUsersInGroup")]
        public async Task<List<User>> GetUsersInGroup(long id)
        {
            _logger.LogInformation("Вызван метод GetUsersInGroup с идентификатором группы {id}", id);

            var group = await _context.StudyGroups
                .Include(g => g.Users)
                .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

            if (group == null)
            {
                _logger.LogWarning("Группа с идентификатором {id} не найдена или удалена", id);
                return [];
            }

            _logger.LogInformation("Метод GetUsersInGroup возвращает пользователей для группы с идентификатором {id}", id);
            return group.Users.ToList();
        }

        [Authorize(Policy = RoleJWTData.AdminUserPolicyName)]
        [HttpPost(Name = "CreateGroup")]
        public async Task<IActionResult> Post([FromBody] GroupRequest request)
        {
            _logger.LogInformation("Вызван метод CreateGroup для создания группы с именем: {request.Name}", request.Name);

            var existingGroup = await _context.StudyGroups.FirstOrDefaultAsync(b => b.Name == request.Name);
            if (existingGroup != null)
            {
                _logger.LogWarning("Группа с именем '{request.Name}' уже существует", request.Name);
                return Conflict(new { Error = $"Группа с именем '{request.Name}' уже существует" });
            }

            try
            {
                var group = new StudyGroup
                {
                    Name = request.Name,
                };

                _context.StudyGroups.Add(group);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Группа с именем '{request.Name}' успешно создана с идентификатором: {group.Id}", request.Name, group.Id);
                return CreatedAtAction(nameof(Post), new { id = group.Id }, group);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Ошибка при создании группы: {ex.Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка при создании группы" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка: {ex.Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка" });
            }
        }

        [Authorize(Policy = RoleJWTData.AdminUserPolicyName)]
        [HttpPut("{id}", Name = "UpdateGroup")]
        public async Task<IActionResult> Put(long id, [FromBody] StudyGroup group)
        {
            _logger.LogInformation("Вызван метод Update с идентификатором: {id}", id);

            if (group == null || string.IsNullOrWhiteSpace(group.Name))
            {
                _logger.LogWarning("Не получены корректные данные для обновления группы с идентификатором: {id}", id);
                return BadRequest(new { Error = "Некорректные данные для обновления группы" });
            }

            try
            {
                var updatedRows = await _context.StudyGroups
                    .Where(b => b.Id == id && !b.IsDeleted && b.Name != group.Name)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.Name, group.Name));

                if (updatedRows > 0)
                {
                    _logger.LogInformation("Группа с идентификатором {id} успешно обновлена на новое имя: {group.Name}", id, group.Name);
                    return Ok(new { Message = "Группа успешно обновлена" });
                }

                _logger.LogWarning("Группа с идентификатором {id} не найдена или имя не изменилось", id);
                return NotFound(new { Error = $"Группа не найдена или имя не изменилось" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Ошибка при обновлении группы с идентификатором {id}: {ex.Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка при обновлении группы" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка при обновлении группы с идентификатором {id}: {ex.Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка" });
            }
        }

        [Authorize(Policy = RoleJWTData.AdminUserPolicyName)]
        [HttpDelete("{id}", Name = "DeleteGroup")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Вызван метод DeleteGroup с идентификатором: {id}", id);

            var updatedRows = await _context.StudyGroups.Where(b => b.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, b => true));

            if (updatedRows > 0)
            {
                _logger.LogInformation("Группа с идентификатором {id} успешно удалена", id);
                return Ok(new { Message = $"Группа с идентификатором {id} успешно удалена" });
            }

            _logger.LogWarning("Группа с идентификатором {id} не найдена для удаления", id);
            return NotFound(new { Error = $"Группа с идентификатором {id} не найден" });
        }
    }
}
