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
                _logger.LogWarning("❌ Geçersiz istek: Text veya UserId eksik");
                return BadRequest(new { error = "Geçersiz mesaj veya kullanıcı." });
            }

            var message = new Message
            {
                UserId = request.UserId,
                Text = request.Text,
                Sentiment = "unknown"
            };

            // Hugging Face Spaces endpoint
            string hfUrl = "https://hatice10-chat-emotion-ai.hf.space/run/predict";

            try
            {
                _logger.LogInformation("🤖 AI analizi başlatılıyor...");

                // Spaces için JSON: { "data": ["mesaj"] }
                var response = await _httpClient.PostAsJsonAsync(hfUrl, new { data = new string[] { message.Text } });

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"✅ AI cevabı: {json}");

                    using var doc = JsonDocument.Parse(json);

                    // Gradio genelde "data" array içinde cevap döner
                    if (doc.RootElement.TryGetProperty("data", out var dataProp) && dataProp.GetArrayLength() > 0)
                    {
                        message.Sentiment = dataProp[0].GetString() ?? "unknown";
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ AI servisi hata döndü: {response.StatusCode}");
                }
            }
            catch (Exception aiEx)
            {
                _logger.LogError(aiEx, "⚠️ AI analizi hatası - mesaj 'unknown' sentiment ile kaydedilecek");
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"💾 Mesaj kaydedildi: Id={message.Id}, Sentiment={message.Sentiment}");

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
                    Timestamp = DateTime.UtcNow,
                    Confidence = 0.75
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
