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
    [Route("api/messages")] // ✅ Küçük harf ve çoğul
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
            try
            {
                if (string.IsNullOrEmpty(request.Text) || request.UserId == 0)
                {
                    _logger.LogWarning("❌ Geçersiz istek: Text veya UserId eksik");
                    return BadRequest(new { error = "Geçersiz mesaj veya kullanıcı." });
                }

                _logger.LogInformation($"📨 Yeni mesaj: UserId={request.UserId}, Text={request.Text}");

                var message = new Message
                {
                    UserId = request.UserId,
                    Text = request.Text,
                    Sentiment = "unknown"
                };

                // Hugging Face'e istek at
                try
                {
                    _logger.LogInformation("🤖 AI analizi başlatılıyor...");

                    var response = await _httpClient.PostAsJsonAsync(
                        "https://hatice10-chat-emotion-ai.hf.space/analyze",
                        new { text = message.Text },
                        new System.Threading.CancellationToken()
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"✅ AI cevabı: {json}");

                        using var doc = JsonDocument.Parse(json);

                        if (doc.RootElement.TryGetProperty("sentiment", out var sentimentProp))
                        {
                            message.Sentiment = sentimentProp.GetString() ?? "unknown";
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Mesaj oluşturulurken kritik hata");
                return StatusCode(500, new { error = "Mesaj gönderilemedi", details = ex.Message });
            }
        }

        // GET: api/messages
        [HttpGet]
        public IActionResult GetMessages()
        {
            try
            {
                var messages = _context.Messages
                    .OrderBy(m => m.Id)
                    .Select(m => new
                    {
                        m.Id,
                        m.UserId,
                        m.Text,
                        m.Sentiment,
                        // Frontend için timestamp ekleyelim
                        Timestamp = DateTime.UtcNow, // Gerçek timestamp için Message modeline eklenebilir
                        Confidence = 0.75 // Frontend bunu bekliyor
                    }).ToList();

                _logger.LogInformation($"✅ {messages.Count} mesaj getirildi");
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Mesajlar getirilirken hata");
                return StatusCode(500, new { error = "Mesajlar yüklenemedi" });
            }
        }

        // DELETE: api/messages/reset
        [HttpDelete("reset")]
        public async Task<IActionResult> ResetDatabase()
        {
            try
            {
                var messages = _context.Messages.ToList();
                var count = messages.Count;

                _context.Messages.RemoveRange(messages);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ {count} mesaj silindi");
                return Ok(new { message = $"Veritabanı temizlendi. {count} mesaj silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Veritabanı temizlenirken hata");
                return StatusCode(500, new { error = "Veritabanı temizlenemedi" });
            }
        }

        // DELETE: api/messages (Tüm mesajları sil - alternatif)
        [HttpDelete]
        public async Task<IActionResult> DeleteAllMessages()
        {
            try
            {
                var messages = _context.Messages.ToList();
                var count = messages.Count;

                _context.Messages.RemoveRange(messages);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🗑️ {count} mesaj silindi");
                return Ok(new { message = $"{count} mesaj silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Mesajlar silinirken hata");
                return StatusCode(500, new { error = "Mesajlar silinemedi" });
            }
        }
    }

    // Request DTO
    public class MessageRequest
    {
        public int UserId { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}