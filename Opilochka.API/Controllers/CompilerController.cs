using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Opilochka.Core.Compiler;
using Opilochka.Data.Answers;
using System.Text.Json;


namespace Opilochka.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompilerController(IOptions<CompilerOptions> options, ILogger<CompilerController> logger) : Controller
    {
        private readonly CompilerOptions _options = options.Value;
        private readonly ILogger<CompilerController> _logger = logger;

        [HttpPost]
        public async Task<IActionResult> CompileCode([FromBody] CompileRequest compileRequest)
        {
            if (compileRequest == null || string.IsNullOrEmpty(compileRequest.Code) || string.IsNullOrEmpty(compileRequest.Language))
            {
                _logger.LogWarning("Некорректный запрос компиляции");
                return BadRequest(new { Output = null as string, Error = "Некорректный запрос компиляции" });
            }

            using var httpClient = new HttpClient();
            var requestBody = new
            {
                script = compileRequest.Code,
                language = compileRequest.Language,
                versionIndex = "0",
                clientId = _options.ClientId,
                clientSecret = _options.ClientSecret
            };

            try
            {
                _logger.LogInformation("Отправка запроса на компиляцию к {URL}", _options.URL);
                var response = await httpClient.PostAsJsonAsync(_options.URL, requestBody);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<CompilerResponse>(result);

                    _logger.LogInformation("Компиляция прошла успешно. Ответ: {Response}", result);

                    return Ok(responseData);
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Компиляция не удалась. Код статуса: {StatusCode}, Сообщение об ошибке: {ErrorMessage}", response.StatusCode, errorMessage);

                    return StatusCode((int)response.StatusCode, new CompilerResponse
                    {
                        output = null,
                        error = errorMessage
                    });
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Ошибка при запросе к службе компиляции");
                return StatusCode(500, new CompilerResponse{ output = null, error = "Произошла ошибка при обработке вашего запроса" });
            }
        }
    }
}
