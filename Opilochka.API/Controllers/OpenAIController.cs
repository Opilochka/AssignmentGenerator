using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Opilochka.Core.OpenAI;
using System.Net;
using System.Text;

namespace Opilochka.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OpenAIController(IOptions<OpenAIOptions> options, ILogger<TaskController> logger) : Controller
    {
        private readonly OpenAIOptions _options = options.Value;
        private readonly ILogger<TaskController> _logger = logger;

        //[Authorize(Policy = RoleJWTData.TeacherUserPolicyName)]
        [HttpPost]
        public async Task<IActionResult> SendMessage(string createTask)
        {
            if (string.IsNullOrWhiteSpace(createTask))
            {
                _logger.LogWarning("Попытка отправить пустой вопрос");
                return BadRequest("Вопрос не может быть пустым");
            }

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = CleanseString(createTask) }
                }
            };

            string jsonContent = JsonConvert.SerializeObject(requestBody);
            _logger.LogInformation("Отправка запроса к OpenAI: {JsonContent}", jsonContent);

            using var httpClient = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_options.URL),
                Headers =
                    {
                        { HttpRequestHeader.ContentType.ToString(), "application/json" },
                        { HttpRequestHeader.Authorization.ToString(), "Bearer " + _options.ApiKey },
                    },
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    string answer = await GetClearAnswerFromResponse(response);
                    _logger.LogInformation("Получен ответ от OpenAI: {Answer}", answer);
                    return Ok(answer);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Ошибка от OpenAI: {StatusCode}, Контент: {ErrorContent}", response.StatusCode, errorContent);
                    return StatusCode((int)response.StatusCode, $"Ошибка: {response.StatusCode}, Контент: {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Ошибка запроса: {Message}", ex.Message);
                return StatusCode(500, $"Ошибка запроса: {ex.Message}");
            }
        }
        private static async Task<string> GetClearAnswerFromResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var parsedResponse = JObject.Parse(responseContent);

            var choices = parsedResponse["choices"];

            if (choices == null || !choices.HasValues || !choices.Any())
            {
                return "*** Нет действительного ответа от API.";
            }

            var firstChoice = choices[0];
            var message = firstChoice?["message"];
            var content = message?["content"]?.ToString();

            if (string.IsNullOrEmpty(content))
            {
                return "*** Нет действительного ответа от API.";
            }

            return content;
        }
        private static string CleanseString(string input)
        {
            return JsonConvert.ToString(input); // Превращаем строку в корректный формат JSON
        }
    }
}
