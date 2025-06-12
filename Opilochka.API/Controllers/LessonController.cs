using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Opilochka.Core.Auth;
using Opilochka.Core.ViewModels;
using Opilochka.Data.Lessons;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Policy = "AdminPolicy")]
    public class LessonController(OpilochkaDbContext context, ILogger<LessonController> logger) : Controller
    {
        private readonly ILogger<LessonController> _logger = logger;
        private readonly OpilochkaDbContext _context = context;

        //[Authorize]
        [HttpGet(Name = "GetLessons")]
        public async Task<List<Lesson>> Get()
        {
            _logger.LogInformation($"Вызван метод GetLessons без указания идентификатора");
            var lessons = await _context.Lessons.Where(b => !b.IsDeleted).ToListAsync();
            _logger.LogInformation($"Метод GetLessons возвращает уроки");
            return lessons;
        }

        //[Authorize]
        [HttpGet("{id}", Name = "GetLesson")]
        public async Task<IActionResult> Get(long id)
        {
            _logger.LogInformation("Вызван метод GetLesson с идентификатором: {id}", id);
            var lesson = await _context.Lessons.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (lesson == null)
            {
                _logger.LogWarning("Урок с идентификатором {id} не найден", id);
                return NotFound(
                    new
                    {
                        Error = $"Урок с идентификатором {id} не найден"
                    }
                );
            }
            _logger.LogInformation("Урок с идентификатором {id} успешно найден", id);
            return Ok(lesson);
        }

        //[Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpPost(Name = "CreateLesson")]
        public async Task<IActionResult> Post([FromBody] LessonRequest request)
        {
            _logger.LogInformation($"Вызван метод CreateLesson");

            try
            {
                var lesson = new Lesson()
                {
                    Title = request.Title,
                    UserId = request.UserId,
                    Icon = request.Icon,
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Урок успешно создан с идентификатором: {lesson.Id}", lesson.Id);
                return CreatedAtAction(nameof(Post), new { id = lesson.Id }, lesson);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Не удалось создать урок, возникло исключение: {ex.Message}",  ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка подключения" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка: {ex.Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка при создании урока" });
            }
        }

        //[Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpPut("{id}", Name = "UpdateLesson")]
        public async Task<IActionResult> Put(long id, [FromBody] Lesson lesson)
        {
            if (lesson == null)
            {
                _logger.LogWarning("Не получены корректные данные для обновления урока с идентификатором: {id}", id);
                return BadRequest(new { Error = "Некорректные данные для обновления урока" });
            }
            try
            {
                var updatedRows = await _context.Lessons
                    .Where(b => b.Id == id && b.Title != lesson.Title)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(b => b.Title, lesson.Title)
                        .SetProperty(b => b.Icon, lesson.Icon));

                if (updatedRows > 0)
                {
                    _logger.LogInformation("Урок с идентификатором {id} успешно обновлен", id);
                    return Ok(new { Message = "Урок успешно обновлен" });
                }

                _logger.LogWarning("Урок с идентификатором {id} не найден или не изменен", id);
                return NotFound(new { Error = $"Урок не найден или название не изменено" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("Ошибка при обновлении урока с идентификатором {id}: {ex.Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Ошибка при обновлении урока" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Неизвестная ошибка при обновлении урока с идентификатором {id}: {ex.Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Произошла ошибка" });
            }
        }

        //[Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpDelete("{id}", Name = "DeleteLesson")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Вызван метод DeleteLesson с идентификатором: {id}", id);

            var updatedRows = await _context.Lessons.Where(b => b.Id == id).ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.IsDeleted, b => true));

            if (updatedRows > 0)
            {
                _logger.LogInformation("Урок с идентификатором {id} успешно удален", id);
                return Ok(new { Message = $"Урок с идентификатором {id} успешно удален" });
            }

            _logger.LogWarning("Урок с идентификатором {id} не найден или уже удален", id);
            return NotFound(new { Error = $"Урок с идентификатором {id} не найден" });
        }
    }
}
