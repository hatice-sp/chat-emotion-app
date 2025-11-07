using ChatEmotionBackend.Data;
using ChatEmotionBackend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace ChatEmotionBackend.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MessageController> _logger;

        public MessageController(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<MessageController> logger)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] MessageRequest request)
        {
            if (string.IsNullOrEmpty(request.Text) || request.UserId == 0)
            {
                return BadRequest(new { error = "Geçersiz mesaj veya kullanıcı." });
            }

            var message = new Message
            {
                UserId = request.UserId,
                Text = request.Text,
                Sentiment = "unknown",
            };

            string flaskApiUrl = "https://chat-emotion-app-1-aahh.onrender.com/analyze";

            try
            {
                var payload = new { text = message.Text };
                var response = await _httpClient.PostAsJsonAsync(flaskApiUrl, payload);
                var json = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"AI API response: {json}");

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("sentiment", out var sentiment))
                        message.Sentiment = sentiment.GetString() ?? "unknown";

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analizi hatası - mesaj 'unknown' sentiment ile kaydedilecek");
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }

        // GET: api/messages
        [HttpGet]
        public IActionResult GetMessages()
        {
            var messages = _context.Messages
                .OrderBy(m => m.Id)
                .Select(m => new
                {
                    m.Id,
                    m.UserId,
                    m.Text,
                    m.Sentiment,
                    Timestamp = DateTime.UtcNow
                }).ToList();

            return Ok(messages);
        }

        // DELETE: api/messages/reset
        [HttpDelete("reset")]
        public async Task<IActionResult> ResetDatabase()
        {
            var messages = _context.Messages.ToList();
            var count = messages.Count;

            _context.Messages.RemoveRange(messages);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Veritabanı temizlendi. {count} mesaj silindi." });
        }
    }

    public class MessageRequest
    {
        public int UserId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
