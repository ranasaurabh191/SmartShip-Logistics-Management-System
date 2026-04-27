using System.Text.Json;
using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Core.Interfaces.Services;

namespace SmartShip.TrackingService.Core.Services;

public class ChatService : IChatService
{
    private readonly IConfiguration _config;
    private readonly ITrackingEventRepository _trackingRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IDeliveryProofRepository _deliveryProofRepo;
    private readonly IShipmentClient _shipmentClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;

    private static readonly Dictionary<string, (string Response, DateTime CachedAt)>
        _cache = new();

    public ChatService(
        IConfiguration config,
        ITrackingEventRepository trackingRepo,
        IDocumentRepository documentRepo,
        IDeliveryProofRepository deliveryProofRepo,
        IShipmentClient shipmentClient,
        IHttpClientFactory httpClientFactory,
        ILogger<ChatService> logger)
    {
        _config = config;
        _trackingRepo = trackingRepo;
        _documentRepo = documentRepo;
        _deliveryProofRepo = deliveryProofRepo;
        _shipmentClient = shipmentClient;
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _logger = logger;
    }


    public async Task<ChatResponseDto> ProcessAsync(
        ChatMessageRequest req, int userId, bool isAdmin)
    {
        _logger.LogInformation("Chat from User {UserId}: {Message}", userId, req.Message);

        var activeShipmentId = req.SelectedShipmentId ?? req.ShipmentId;

        var intent = DetectIntent(req.Message);
        _logger.LogInformation("Intent: {Intent}, ActiveShipment: {Id}", intent, activeShipmentId);

        try
        {
            if (intent == "greeting") return HandleGreeting();
            if (intent == "help") return HandleHelp();
            if (intent == "small_talk") return HandleSmallTalk();

            if (intent == "rate_calculate") return HandleRateCalculation(req.Message);
            if (intent == "rate_general") return HandleRateGeneral();
            if (intent == "rate_compare") return await HandleRateComparison(req.Message);

            if (intent is "track_shipment" or "delivery_eta" or "shipment_status"
                       or "list_documents" or "delivery_proof")
            {
                if (activeShipmentId == null)
                    return await ShowShipmentPicker(userId, isAdmin,
                        GetPickerPrompt(intent));

                return await HandleShipmentIntent(
                    intent, req, activeShipmentId.Value, userId);
            }

            if (intent == "admin_stats" && isAdmin)
                return await HandleAdminStats(userId);

            if (intent == "reset_context")
                return new ChatResponseDto(
                    "✅ Context cleared. Which shipment would you like to check next?",
                    "reset", null);

            return await AskOllama(req, userId, isAdmin, activeShipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat processing failed for User {UserId}", userId);
            return new ChatResponseDto(
                "Sorry, something went wrong. Please try again.",
                "error", null);
        }
    }


    private static string DetectIntent(string message)
    {
        var msg = message.ToLower().Trim();

        if (msg.Length < 3) return "small_talk";

        if (msg is "hi" or "hello" or "hey" or "good morning"
               or "good evening" or "namaste" or "hii" or "helo")
            return "greeting";

        if (Has(msg, "help", "what can you", "commands", "options", "what do you do"))
            return "help";

        if (IsSmallTalk(msg)) return "small_talk";

        if (msg is "check another" or "reset" or "another shipment" or "change shipment"
               or "different shipment" or "clear context")
            return "reset_context";

        if (Has(msg, "rate", "price", "cost", "charge", "how much", "fee")
            && HasNumber(msg))
            return "rate_calculate";

        if (Has(msg, "cheapest", "compare", "which type", "best rate", "rate comparison"))
            return "rate_compare";

        if (Has(msg, "rate", "price", "cost", "charge", "how much", "fee", "pricing"))
            return "rate_general";

        if (Has(msg, "when will", "how long", "eta", "arrive", "days", "delivery time",
                "expected", "estimate"))
            return "delivery_eta";

        if (Has(msg, "track", "where is", "location", "transit", "shipment status",
                "where", "status of my"))
            return "track_shipment";

        if (Has(msg, "document", "invoice", "label", "file", "upload", "attachment"))
            return "list_documents";

        if (Has(msg, "delivered", "delivery proof", "proof", "signature", "received",
                "confirm delivery"))
            return "delivery_proof";

        if (Has(msg, "status", "what status", "all shipments", "my shipments",
                "show shipments", "list shipments"))
            return "shipment_status";

        if (Has(msg, "total shipments", "pending count", "dashboard", "summary",
                "how many", "stats", "analytics"))
            return "admin_stats";

        return "unknown";
    }

    private static bool Has(string msg, params string[] kw)
        => kw.Any(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static bool HasNumber(string msg)
        => msg.Any(char.IsDigit);

    private static bool IsSmallTalk(string msg) =>
        new[] { "how are you", "thanks", "thank you", "ok", "okay", "cool",
                "nice", "good", "bye", "goodbye", "ok got it", "got it",
                "sure", "alright", "great", "awesome" }
        .Any(s => msg.Contains(s, StringComparison.OrdinalIgnoreCase));


    private async Task<ChatResponseDto> ShowShipmentPicker(
        int userId, bool isAdmin, string prompt)
    {
        var shipments = await _shipmentClient.GetUserShipmentsAsync(userId, isAdmin);

        if (!shipments.Any())
            return new ChatResponseDto(
                "You don't have any shipments yet. Create one to get started!",
                "no_shipments", null);

        var chips = shipments.Select(s => new ShipmentChip
        {
            ShipmentId = s.Id,
            TrackingNumber = s.TrackingNumber,
            Label = $"{s.TrackingNumber} · {s.ShipmentType} · " +
                             $"{s.OriginCity}→{s.DestinationCity}",
            Status = s.Status
        }).ToList();

        return new ChatResponseDto(prompt, "shipment_picker", null, chips);
    }

    private static string GetPickerPrompt(string intent) => intent switch
    {
        "track_shipment" => "📦 Which shipment would you like to track? Select one below:",
        "delivery_eta" => "🕐 Which shipment do you want the ETA for? Select one below:",
        "shipment_status" => "📋 Which shipment's status do you want to check? Select one below:",
        "list_documents" => "📎 Which shipment's documents do you want to see? Select one below:",
        "delivery_proof" => "✅ Which shipment's delivery proof do you want? Select one below:",
        _ => "📦 Please select a shipment to continue:"
    };


    private async Task<ChatResponseDto> HandleShipmentIntent(
        string intent, ChatMessageRequest req,
        int shipmentId, int userId)
    {
        var shipment = await _shipmentClient.GetShipmentByIdAsync(shipmentId);
        if (shipment == null)
            return new ChatResponseDto(
                "I couldn't find that shipment. Please try selecting again.",
                "error", null);

        var events = await _trackingRepo.GetByTrackingNumberPagedAsync(
            shipment.TrackingNumber,
            new TrackingEventPagedRequest { Page = 1, PageSize = 20 });

        var latestEvent = events.Data.LastOrDefault();

        var proof = shipment.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase)
            ? await _deliveryProofRepo.GetByShipmentIdAsync(shipmentId)
            : null;

        var daysSinceCreated = (DateTime.Now - shipment.CreatedAt).Days;
        var expectedDelivery = shipment.CreatedAt.AddDays(7);
        var daysRemaining = Math.Max(0, (expectedDelivery - DateTime.Now).Days);

        var shipmentContext = new
        {
            TrackingNumber = shipment.TrackingNumber,
            Type = shipment.ShipmentType,
            Status = shipment.Status,
            PaymentStatus = shipment.PaymentStatus,
            WeightKg = shipment.WeightKg,
            Route = $"{shipment.OriginCity} → {shipment.DestinationCity}",
            CreatedAt = shipment.CreatedAt.ToString("dd-MMM-yyyy"),
            DaysSinceCreated = daysSinceCreated,
            ExpectedDelivery = expectedDelivery.ToString("dd-MMM-yyyy"),
            DaysRemaining = daysRemaining,
            LatestEvent = latestEvent == null ? null : new
            {
                Status = latestEvent.Status,
                Location = latestEvent.Location,
                Time = latestEvent.EventTime.ToString("dd-MMM-yyyy hh:mm tt"),
                Description = latestEvent.Description
            },
            AllEvents = events.Data.Select(e => new
            {
                e.Status,
                e.Location,
                Time = e.EventTime.ToString("dd-MMM-yyyy hh:mm tt"),
                e.Description
            }),
            DeliveryProof = proof == null ? null : new
            {
                proof.ReceiverName,
                proof.DeliveredBy,
                DeliveredAt = proof.DeliveredAt.ToString("dd-MMM-yyyy hh:mm tt"),
                proof.Notes
            }
        };

        var systemPrompt = $"""
            You are SmartShip's AI logistics assistant.
            Answer the user's question using ONLY the real shipment data provided.
            Do NOT make up any data. Be concise, friendly, and use markdown formatting.
            
            Rules:
            - If PaymentStatus is not "Paid", mention payment is pending
            - If Status is "PendingPickup" or no tracking events exist, say pickup is not scheduled yet
            - For ETA questions, use CreatedAt + 7-day average and mention DaysRemaining
            - If delivered, show DeliveryProof details
            - Keep response under 150 words unless showing a list
            
            Real shipment data:
            {JsonSerializer.Serialize(shipmentContext, new JsonSerializerOptions { WriteIndented = true })}
            """;

        var reply = await CallOllama(req.Message, systemPrompt, req.History);
        return new ChatResponseDto(reply, intent, shipmentContext);
    }

    private static ChatResponseDto HandleRateCalculation(string message)
    {
        var words = message.Split(' ');
        double weightKg = 0;
        foreach (var w in words)
        {
            var clean = w.Replace("kg", "").Replace("KG", "").Trim();
            if (double.TryParse(clean, out var parsed))
            {
                weightKg = parsed;
                break;
            }
        }

        if (weightKg <= 0)
            return HandleRateGeneral(); 

        decimal expressRate = Math.Max((decimal)(weightKg * 150), 99);
        decimal internationalRate = Math.Max((decimal)(weightKg * 300), 99);
        decimal freightRate = Math.Max((decimal)(weightKg * 50), 99);
        decimal domesticRate = Math.Max((decimal)(weightKg * 80), 99);

        return new ChatResponseDto(
            $"💰 **Estimated Rates for {weightKg} kg:**\n\n" +
            $"| Type          | Rate          |\n" +
            $"|---------------|---------------|\n" +
            $"| 🚀 Express       | ₹{expressRate:N0}       |\n" +
            $"| 🌍 International | ₹{internationalRate:N0} |\n" +
            $"| 🚚 Freight       | ₹{freightRate:N0}       |\n" +
            $"| 📦 Domestic      | ₹{domesticRate:N0}      |\n\n" +
            $"*Minimum charge ₹99 applies. Final rate shown at checkout.*",
            "rate_calculate", null);
    }

    private static ChatResponseDto HandleRateGeneral() => new(
        "💰 **SmartShip Rates are calculated as:**\n\n" +
        "| Type          | Rate per kg | Minimum |\n" +
        "|---------------|-------------|----------|\n" +
        "| 🚀 Express       | ₹150/kg     | ₹99      |\n" +
        "| 🌍 International | ₹300/kg     | ₹99      |\n" +
        "| 🚚 Freight       | ₹50/kg      | ₹99      |\n" +
        "| 📦 Domestic      | ₹80/kg      | ₹99      |\n\n" +
        "💡 *Tell me a weight (e.g. \"rate for 3kg\") for an exact quote!*",
        "rate_general", null);

    private async Task<ChatResponseDto> HandleRateComparison(string message)
    {
        double weightKg = 1;
        foreach (var w in message.Split(' '))
        {
            var clean = w.Replace("kg", "").Trim();
            if (double.TryParse(clean, out var parsed) && parsed > 0)
            {
                weightKg = parsed;
                break;
            }
        }

        var prompt = $"""
            User asked: "{message}"
            
            SmartShip rate formula:
            - Express: weightKg × ₹150, min ₹99
            - International: weightKg × ₹300, min ₹99
            - Freight: weightKg × ₹50, min ₹99
            - Domestic: weightKg × ₹80, min ₹99
            
            Weight being compared: {weightKg}kg
            
            Express rate:       ₹{Math.Max((decimal)(weightKg * 150), 99):N0}
            International rate: ₹{Math.Max((decimal)(weightKg * 300), 99):N0}
            Freight rate:       ₹{Math.Max((decimal)(weightKg * 50), 99):N0}
            Domestic rate:      ₹{Math.Max((decimal)(weightKg * 80), 99):N0}
            
            Answer the user's question naturally. Recommend the cheapest option clearly.
            Use markdown. Keep it under 100 words.
            """;

        var reply = await CallOllama(message, prompt, null);
        return new ChatResponseDto(reply, "rate_compare", null);
    }


    private async Task<ChatResponseDto> HandleAdminStats(int userId)
    {
        var shipments = await _shipmentClient.GetUserShipmentsAsync(userId, true);

        var total = shipments.Count;
        var pending = shipments.Count(s => s.Status == "PendingPickup");
        var transit = shipments.Count(s => s.Status == "InTransit");
        var delivered = shipments.Count(s => s.Status == "Delivered");
        var unpaid = shipments.Count(s => s.PaymentStatus != "Paid");

        return new ChatResponseDto(
            $"📊 **SmartShip Dashboard Summary:**\n\n" +
            $"| Metric           | Count |\n" +
            $"|------------------|-------|\n" +
            $"| 📦 Total          | {total}     |\n" +
            $"| 🕐 Pending Pickup | {pending}   |\n" +
            $"| 🚚 In Transit     | {transit}   |\n" +
            $"| ✅ Delivered      | {delivered} |\n" +
            $"| 💳 Unpaid         | {unpaid}    |\n\n" +
            $"*Live data as of {DateTime.Now:hh:mm tt, dd-MMM}*",
            "admin_stats", null);
    }


    private async Task<ChatResponseDto> AskOllama(
        ChatMessageRequest req, int userId, bool isAdmin, int? activeShipmentId)
    {
        if (req.Message.Length < 5 || IsSmallTalk(req.Message.ToLower()))
            return FallbackResponse();

        var systemPrompt = $"""
            You are SmartShip's AI logistics assistant.
            Help users with shipment tracking, delivery, documents, and logistics questions.
            
            Current user: {(isAdmin ? "Admin" : "Customer")} (ID: {userId})
            {(activeShipmentId.HasValue ? $"Active shipment context: ID {activeShipmentId}" : "No shipment selected")}
            
            Rules:
            - Be concise and friendly
            - Use markdown formatting
            - Only answer logistics/shipping related questions
            - If asked about unrelated topics, politely redirect
            - Do not make up shipment data
            """;

        var cacheKey = req.Message.ToLower().Trim();
        if (_cache.TryGetValue(cacheKey, out var cached)
            && DateTime.Now - cached.CachedAt < TimeSpan.FromMinutes(30))
        {
            _logger.LogInformation("Cache hit for: {Message}", req.Message);
            return new ChatResponseDto(cached.Response, "ai_cached", null);
        }

        var reply = await CallOllama(req.Message, systemPrompt, req.History);

        _cache[cacheKey] = (reply, DateTime.Now);
        return new ChatResponseDto(reply, "ai", null);
    }


    private async Task<string> CallOllama(
        string userMessage, string systemPrompt,
        List<ChatHistoryItem>? history)
    {
        var baseUrl = _config["AI:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model = _config["AI:Ollama:Model"] ?? "gemma2:2b";

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        if (history?.Any() == true)
        {
            foreach (var h in history.TakeLast(6))
            {
                messages.Add(new
                {
                    role = h.Role == "bot" ? "assistant" : "user",
                    content = h.Text
                });
            }
        }

        messages.Add(new { role = "user", content = userMessage });

        var payload = new { model, messages, stream = false };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{baseUrl}/api/chat", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama error: {Status}", response.StatusCode);
                return FallbackResponse().Reply;
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("message")
                       .GetProperty("content")
                       .GetString()
                   ?? "Sorry, I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama call failed");
            return FallbackResponse().Reply;
        }
    }


    private static ChatResponseDto HandleGreeting() => new(
        "👋 Hello! I'm your SmartShip AI assistant.\n\n" +
        "Type **help** to see everything I can do, or just ask me anything!",
        "greeting", null);

    private static ChatResponseDto HandleSmallTalk() => new(
        "😊 I'm here to help with your shipments! " +
        "Ask me about tracking, rates, documents, or delivery status.\n\n" +
        "Type **help** for the full list.",
        "small_talk", null);

    private static ChatResponseDto HandleHelp() => new(
        """
        🤖 **SmartShip AI — What I can do:**

        📦 **Track shipment** — "where is my package?"
        💰 **Rate calculator** — "rate for 5kg" or "how much does express cost?"
        🕐 **Delivery ETA** — "when will my order arrive?"
        📋 **Shipment status** — "show my shipments"
        📎 **Documents** — "show my invoice"
        ✅ **Delivery proof** — "was my order delivered?"
        📊 **Admin stats** — "show dashboard summary" *(admin only)*

        💡 *Select a shipment when prompted to get live data!*
        """, "help", null);

    private static ChatResponseDto FallbackResponse() => new(
        "I'm not sure about that. Here's what I can help with:\n\n" +
        "📦 Track | 💰 Rates | 🕐 ETA | 📎 Documents | ✅ Delivery Proof\n\n" +
        "Type **help** for the full list.",
        "fallback", null);
}