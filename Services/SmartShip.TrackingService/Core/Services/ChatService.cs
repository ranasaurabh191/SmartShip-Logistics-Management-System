using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Core.Interfaces.Services;
using SmartShip.TrackingService.Domain.Entities;
using System.Text.Json;

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
    private static string? ExtractTrackingNumber(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            message, @"SS\d{10,}", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpper() : null;
    }

    public async Task<ChatResponseDto> ProcessAsync(
        ChatMessageRequest req, int userId, bool isAdmin)
    {
        _logger.LogInformation("Chat from User {UserId}: {Message}", userId, req.Message);
        _logger.LogInformation("ChatService VERSION 2 loaded");
        var activeShipmentId = req.SelectedShipmentId ?? req.ShipmentId;
        var intent = DetectIntent(req.Message);
        _logger.LogInformation("Intent: {Intent}, ActiveShipment: {Id}", intent, activeShipmentId);

        var extractedTrackingNumber = ExtractTrackingNumber(req.Message);
        if (extractedTrackingNumber != null && activeShipmentId == null)
        {
            var allShipments = await _shipmentClient.GetUserShipmentsAsync(userId, isAdmin);
            var matched = allShipments.FirstOrDefault(s =>
                s.TrackingNumber.Equals(extractedTrackingNumber,
                    StringComparison.OrdinalIgnoreCase));
            if (matched != null)
                activeShipmentId = matched.Id;
        }
        try
        {
            if (intent == "greeting") return HandleGreeting();
            if (intent == "help") return HandleHelp();
            if (intent == "small_talk") return HandleSmallTalk();
            if (intent == "reset_context") return HandleResetContext();

            if (intent == "rate_calculate") return HandleRateCalculation(req.Message);
            if (intent == "rate_general") return HandleRateGeneral();
            if (intent == "rate_compare") return await HandleRateComparison(req.Message);

            if (intent == "admin_stats")
            {
                if (!isAdmin)
                    return new ChatResponseDto(
                        "📊 Dashboard stats are available to **admins only**.",
                        "unauthorized", null);
                return await HandleAdminStats(userId);
            }

            if (intent == "list_documents")
            {
                if (!isAdmin)
                    return new ChatResponseDto(
                        "📎 Document access is available to **admins only**.\n\n" +
                        "As a customer you can view your **delivery proof** once delivered.\n" +
                        "Type **delivery proof** to check it.",
                        "unauthorized", null);

                if (activeShipmentId == null)
                    return await ShowShipmentPicker(userId, isAdmin,
                        "📎 Which shipment's documents do you want to see? Select one below:");

                return await HandleDocuments(req, userId, isAdmin);
            }

            if (intent is "track_shipment" or "delivery_eta"
                       or "shipment_status" or "delivery_proof")
            {
                if (activeShipmentId == null)
                    return await ShowShipmentPicker(userId, isAdmin,
                        GetPickerPrompt(intent));

                return await HandleShipmentIntent(intent, req,
                    activeShipmentId.Value, userId);
            }

            return await AskOllama(req, userId, isAdmin, activeShipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat processing failed for User {UserId}", userId);
            return new ChatResponseDto(
                "⚠️ Something went wrong. Please try again.",
                "error", null);
        }
    }
    private static string DetectIntent(string message)
    {
        var msg = message.ToLower().Trim();

        if (msg.Contains("tell me about shipment") ||
            msg.Contains("about shipment") ||
            msg.Contains("info about") ||
            msg.Contains("details of shipment") ||
            msg.Contains("shipment ss") ||
            System.Text.RegularExpressions.Regex.IsMatch(msg, @"ss\d{10,}"))
            return "track_shipment";

        if (Has(msg, "how many hubs", "active hubs", "how many users", "total users",
        "user count", "hub count"))
            return "unknown";

        if (msg.Length < 3) return "small_talk";

        if (msg is "hi" or "hello" or "hey" or "good morning" or "hii"
               or "good evening" or "namaste" or "helo" or "heyy")
            return "greeting";

        if (Has(msg, "help", "what can you do", "commands", "options", "menu"))
            return "help";

        if (IsSmallTalk(msg)) return "small_talk";

        if (msg is "check another" or "reset" or "another shipment" or "change shipment"
               or "different shipment" or "clear context" or "switch shipment"
               or "list my shipments" or "check another shipment" or "show another")
            return "reset_context";

        if (Has(msg, "rate", "price", "cost", "charge", "how much", "fee", "pricing"))
        {
            if (Has(msg, "cheapest", "compare", "which type", "best", "comparison"))
                return "rate_compare";
            return HasNumber(msg) ? "rate_calculate" : "rate_general";
        }

        if (Has(msg, "cheapest", "compare rates", "which type", "best rate"))
            return "rate_compare";

        if (Has(msg, "document", "invoice", "label", "file", "attachment", "packing slip"))
            return "list_documents";

        if (Has(msg, "proof", "delivery proof", "signature", "confirm delivery", "received by"))
            return "delivery_proof";

        if (Has(msg, "when will", "how long", "eta", "delivery time",
                "expected", "estimate", "how many days"))
            return "delivery_eta";

        if (Has(msg, "track", "where is", "where are", "location", "transit",
                "where", "current status", "in transit"))
            return "track_shipment";

        if (Has(msg, "status", "my shipments", "show shipments",
                "list shipments", "all shipments", "shipment status"))
            return "shipment_status";

        if (Has(msg, "total shipments", "pending count", "dashboard",
                "summary", "how many", "stats", "analytics"))
            return "admin_stats";

        return "unknown";
    }

    private static bool Has(string msg, params string[] kw)
        => kw.Any(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static bool HasNumber(string msg) => msg.Any(char.IsDigit);

    private static bool IsSmallTalk(string msg) =>
        new[] { "how are you", "thanks", "thank you", "ok", "okay", "cool",
                "nice", "good", "bye", "goodbye", "ok got it", "got it",
                "sure", "alright", "great", "awesome", "perfect", "noted" }
        .Any(s => msg.Contains(s, StringComparison.OrdinalIgnoreCase));

    private async Task<ChatResponseDto> ShowShipmentPicker(
        int userId, bool isAdmin, string prompt)
    {
        var shipments = await _shipmentClient.GetUserShipmentsAsync(userId, isAdmin);

        if (!shipments.Any())
            return new ChatResponseDto(
                "📭 You don't have any shipments yet. Create one to get started!",
                "no_shipments", null);

        var chips = shipments.Select(s => new ShipmentChip
        {
            ShipmentId = s.Id,
            TrackingNumber = s.TrackingNumber,
            Label = $"{s.TrackingNumber} · {s.ShipmentType} · {s.OriginCity}→{s.DestinationCity}",
            Status = s.Status
        }).ToList();

        return new ChatResponseDto(prompt, "shipment_picker", null, chips);
    }

    private static string GetPickerPrompt(string intent) => intent switch
    {
        "track_shipment" => "📦 Which shipment would you like to track? Select one below:",
        "delivery_eta" => "📅 Which shipment do you want the ETA for? Select one below:",
        "shipment_status" => "📋 Which shipment's status do you want? Select one below:",
        "list_documents" => "📎 Which shipment's documents? Select one below:",
        "delivery_proof" => "✅ Which shipment's delivery proof? Select one below:",
        _ => "📦 Please select a shipment to continue:"
    };

    
    private async Task<ChatResponseDto> HandleShipmentIntent(
        string intent, ChatMessageRequest req, int shipmentId, int userId)
    {
        var shipment = await _shipmentClient.GetShipmentByIdAsync(shipmentId);
        if (shipment == null)
            return new ChatResponseDto(
                "❌ I couldn't find that shipment. Please try selecting again.",
                "error", null);

        var events = await _trackingRepo.GetByTrackingNumberPagedAsync(
            shipment.TrackingNumber,
            new TrackingEventPagedRequest { Page = 1, PageSize = 20 });
        var latestEvent = events.Data.LastOrDefault();

        var proof = shipment.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase)
            ? await _deliveryProofRepo.GetByShipmentIdAsync(shipmentId)
            : null;

        var today = DateTime.Now;
        if (!DateTime.TryParseExact(
                shipment.CreatedAt.ToString(),
                new[] { "dd-MMM-yyyy hh:mm tt", "dd-MMM-yyyy HH:mm", "yyyy-MM-ddTHH:mm:ss" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var createdAt))
            createdAt = today;

        var expectedDelivery = createdAt.AddDays(7);
        var daysRemaining = Math.Max(0, (expectedDelivery - today).Days);
        var isOverdue = today > expectedDelivery
                               && shipment.Status is not "Delivered" and not "Cancelled";

        var status = shipment.Status?.Trim().ToLower() ?? "";

        var reply = status switch
        {
            "delivered" =>
                BuildDeliveredReply(shipment, proof),

            "cancelled" =>
                BuildCancelledReply(shipment),

            "pendingpickup" or "pending_pickup" or "pending pickup" or "pending" =>
                BuildPendingPickupReply(shipment, expectedDelivery, daysRemaining),

            "intransit" or "in_transit" or "in transit" =>
                BuildInTransitReply(shipment, latestEvent, expectedDelivery, daysRemaining),

            _ => BuildGenericReply(shipment, latestEvent, expectedDelivery, daysRemaining, isOverdue)
        };
        _logger.LogInformation("Shipment {Id} Status='{Status}' Payment='{Payment}'",
            shipmentId, shipment.Status, shipment.PaymentStatus);
        return new ChatResponseDto(reply, intent, null);
    }


    private static string BuildDeliveredReply(ShipmentSummary s, DeliveryProof? proof)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"✅ **Shipment {s.TrackingNumber} — Delivered**\n");
        sb.AppendLine($"📦 Type : {s.ShipmentType}");
        sb.AppendLine($"🛣️ Route : {s.OriginCity} → {s.DestinationCity}");
        sb.AppendLine($"💳 Payment : {s.PaymentStatus}");

        if (proof != null)
        {
            sb.AppendLine($"\n**Delivery Confirmation:**");
            sb.AppendLine($"🕒 Delivered on : {proof.DeliveredAt:dd-MMM-yyyy hh:mm tt}");
            sb.AppendLine($"👤 Received by : {proof.ReceiverName}");
            sb.AppendLine($"🚚 Delivered by : {proof.DeliveredBy}");
            if (!string.IsNullOrWhiteSpace(proof.Notes))
                sb.AppendLine($"📝 Notes         : {proof.Notes}");
        }
        else
        {
            sb.AppendLine("\n📋 Delivery proof not yet recorded.");
        }

        if (s.PaymentStatus is not "Paid")
            sb.AppendLine("\n⚠️ Payment is still pending for this shipment.");

        return sb.ToString().TrimEnd();
    }

    private static string BuildCancelledReply(ShipmentSummary s)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"❌ **Shipment {s.TrackingNumber} — Cancelled**\n");
        sb.AppendLine($"📦 Type : {s.ShipmentType}");
        sb.AppendLine($"🛣️ Route : {s.OriginCity} → {s.DestinationCity}");
        sb.AppendLine($"💳 Payment : {s.PaymentStatus}");
        sb.AppendLine("\nThis shipment has been cancelled. No delivery will be made.");

        if (s.PaymentStatus == "Paid")
            sb.AppendLine("💡 Since payment was made, please contact support for a refund.");

        return sb.ToString().TrimEnd();
    }

    private static string BuildPendingPickupReply(
        ShipmentSummary s, DateTime expectedDelivery, int daysRemaining)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🕐 **Shipment {s.TrackingNumber} — Awaiting Pickup**\n");
        sb.AppendLine($"📦 Type : {s.ShipmentType}");
        sb.AppendLine($"🛣️ Route : {s.OriginCity} → {s.DestinationCity}");
        sb.AppendLine($"💳 Payment : {s.PaymentStatus}");
        sb.AppendLine($"📅 Expected Delivery : {expectedDelivery:dd-MMM-yyyy}" +
                      $" ({(daysRemaining > 0 ? $"in {daysRemaining} days" : "today")})");
        sb.AppendLine("\n⏳ Pickup has not been scheduled yet.");

        if (s.PaymentStatus is not "Paid")
            sb.AppendLine("⚠️ Payment is pending — please complete payment to proceed.");

        return sb.ToString().TrimEnd();
    }

    private static string BuildInTransitReply(
        ShipmentSummary s, TrackingEvent? latestEvent,
        DateTime expectedDelivery, int daysRemaining)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🚚 **Shipment {s.TrackingNumber} — In Transit**\n");
        sb.AppendLine($"📦 Type : {s.ShipmentType}");
        sb.AppendLine($"🛣️ Route : {s.OriginCity} → {s.DestinationCity}");
        sb.AppendLine($"💳 Payment : {s.PaymentStatus}");
        sb.AppendLine($"📅 Expected Delivery : {expectedDelivery:dd-MMM-yyyy}" +
                      $" ({(daysRemaining > 0 ? $"in {daysRemaining} days" : "today")})");

        if (latestEvent != null)
        {
            sb.AppendLine("\n**Latest Update:**");
            sb.AppendLine($"📍 Location : {latestEvent.Location}");
            sb.AppendLine($"📋 Status : {latestEvent.Status}");
            sb.AppendLine($"🕒 Time : {latestEvent.EventTime:dd-MMM-yyyy hh:mm tt}");
            if (!string.IsNullOrWhiteSpace(latestEvent.Description))
                sb.AppendLine($"📝 Notes : {latestEvent.Description}");
        }
        else
        {
            sb.AppendLine("\n📍 No location updates yet.");
        }

        if (s.PaymentStatus is not "Paid")
            sb.AppendLine("\n⚠️ Payment is pending for this shipment.");

        return sb.ToString().TrimEnd();
    }

    private static string BuildGenericReply(
        ShipmentSummary s, TrackingEvent? latestEvent,
        DateTime expectedDelivery, int daysRemaining, bool isOverdue)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"📦 **Shipment {s.TrackingNumber}**\n");
        sb.AppendLine($"📋 Status : {s.Status}");
        sb.AppendLine($"📦 Type : {s.ShipmentType}");
        sb.AppendLine($"🛣️ Route : {s.OriginCity} → {s.DestinationCity}");
        sb.AppendLine($"💳 Payment : {s.PaymentStatus}");
        sb.AppendLine($"📅 Expected Delivery : {expectedDelivery:dd-MMM-yyyy}" +
                      $" ({(isOverdue ? "⚠️ overdue" : daysRemaining > 0 ? $"in {daysRemaining} days" : "today")})");

        if (latestEvent != null)
        {
            sb.AppendLine("\n**Latest Update:**");
            sb.AppendLine($"📍 {latestEvent.Status} at {latestEvent.Location}" +
                          $" — {latestEvent.EventTime:dd-MMM-yyyy hh:mm tt}");
        }

        if (s.PaymentStatus is not "Paid")
            sb.AppendLine("\n⚠️ Payment is pending for this shipment.");

        return sb.ToString().TrimEnd();
    }

    private static ChatResponseDto HandleRateCalculation(string message)
    {
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        double weightKg = 0;
        foreach (var w in words)
        {
            var clean = w.Replace("kg", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (double.TryParse(clean, out var parsed) && parsed > 0)
            { weightKg = parsed; break; }
        }

        if (weightKg <= 0) return HandleRateGeneral();

        decimal express = Math.Max((decimal)(weightKg * 150), 99);
        decimal international = Math.Max((decimal)(weightKg * 300), 99);
        decimal freight = Math.Max((decimal)(weightKg * 50), 99);
        decimal domestic = Math.Max((decimal)(weightKg * 80), 99);

        return new ChatResponseDto(
            $"💰 **Rates for {weightKg} kg:**\n\n" +
            $"🚀 Express →  ₹{express:N0}\n" +
            $"🌍 International →  ₹{international:N0}\n" +
            $"🚚 Freight →  ₹{freight:N0}\n" +
            $"📦 Domestic →  ₹{domestic:N0}\n\n" +
            $"*Minimum charge ₹99. Final rate confirmed at checkout.*",
            "rate_calculate", null);
    }

    private static ChatResponseDto HandleRateGeneral() => new(
        "💰 **SmartShip Shipping Rates:**\n\n" +
        "🚀 Express →  ₹150/kg  (min ₹99)\n" +
        "🌍 International →  ₹300/kg  (min ₹99)\n" +
        "🚚 Freight →  ₹50/kg   (min ₹99)\n" +
        "📦 Domestic →  ₹80/kg   (min ₹99)\n\n" +
        "💡 Say **\"rate for 5kg\"** for an exact quote!",
        "rate_general", null);

    private async Task<ChatResponseDto> HandleRateComparison(string message)
    {
        double weightKg = 1;
        foreach (var w in message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var clean = w.Replace("kg", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (double.TryParse(clean, out var p) && p > 0) { weightKg = p; break; }
        }

        decimal express = Math.Max((decimal)(weightKg * 150), 99);
        decimal international = Math.Max((decimal)(weightKg * 300), 99);
        decimal freight = Math.Max((decimal)(weightKg * 50), 99);
        decimal domestic = Math.Max((decimal)(weightKg * 80), 99);
        var cheapest = new[] { ("Express", express), ("International", international),
                                        ("Freight", freight), ("Domestic", domestic) }
                                .OrderBy(x => x.Item2).First();

        var prompt = $"""
            User asked: "{message}"
            Weight: {weightKg}kg

            Calculated rates:
            Express ₹{express:N0}, International ₹{international:N0},
            Freight ₹{freight:N0}, Domestic ₹{domestic:N0}
            Cheapest: {cheapest.Item1} at ₹{cheapest.Item2:N0}

            Answer the user's question. Recommend {cheapest.Item1} clearly.
            Use markdown. Under 80 words. No made-up data.
            """;

        var reply = await CallOllama(message, prompt, null);
        return new ChatResponseDto(reply, "rate_compare", null);
    }


    private async Task<ChatResponseDto> HandleAdminStats(int userId)
    {
        var shipments = await _shipmentClient.GetUserShipmentsAsync(userId, true);
        _logger.LogInformation("Admin stats: fetched {Count} shipments", shipments.Count);

        var total = shipments.Count;
        var pending = shipments.Count(s => s.Status
            .Contains("Pending", StringComparison.OrdinalIgnoreCase));
        var transit = shipments.Count(s => s.Status
            .Contains("Transit", StringComparison.OrdinalIgnoreCase));
        var delivered = shipments.Count(s => s.Status
            .Equals("Delivered", StringComparison.OrdinalIgnoreCase));
        var cancelled = shipments.Count(s => s.Status
            .Equals("Cancelled", StringComparison.OrdinalIgnoreCase));
        var unpaid = shipments.Count(s => s.PaymentStatus is not "Paid");

        return new ChatResponseDto(
            $"📊 **SmartShip Dashboard Summary:**\n\n" +
            $"📦 Total Shipments →  {total}\n" +
            $"🕐 Pending Pickup →  {pending}\n" +
            $"🚚 In Transit →  {transit}\n" +
            $"✅ Delivered →  {delivered}\n" +
            $"❌ Cancelled →  {cancelled}\n" +
            $"💳 Unpaid →  {unpaid}\n\n" +
            $"🕒 *Live data as of {DateTime.Now:hh:mm tt, dd-MMM}*",
            "admin_stats", null);
    }

    private async Task<ChatResponseDto> HandleDocuments(
        ChatMessageRequest req, int userId, bool isAdmin)
    {
        var shipmentId = req.SelectedShipmentId ?? req.ShipmentId!.Value;

        var docs = await _documentRepo.GetPagedByShipmentIdAsync(
            shipmentId, new DocumentPagedRequest { Page = 1, PageSize = 20 });

        if (!docs.Data.Any())
            return new ChatResponseDto(
                "📂 No documents uploaded for this shipment yet.",
                "list_documents", null);

        var list = string.Join("\n", docs.Data.Select((d, i) =>
            $"{i + 1}. **{d.FileName}** — {d.DocumentType} — {d.UploadedAt:dd-MMM-yyyy}"));

        return new ChatResponseDto(
            $"📎 **{docs.TotalCount} document(s)** for this shipment:\n\n{list}\n\n" +
            "_To download, open the shipment details page._",
            "list_documents", docs.Data);
    }

    private async Task<ChatResponseDto> AskOllama(
        ChatMessageRequest req, int userId, bool isAdmin, int? activeShipmentId)
    {
        if (req.Message.Length < 5 || IsSmallTalk(req.Message.ToLower()))
            return FallbackResponse();

        var msg = req.Message.ToLower();
        if (Has(msg, "where is", "track", "delivered", "eta", "delivery date",
                "status", "location", "in transit", "tell me about",
                "about shipment", "when will", "shipment ss", "proof",
                "picked up", "out for delivery", "dispatched"))
        {
            if (activeShipmentId.HasValue)
                return await HandleShipmentIntent(
                    "track_shipment", req, activeShipmentId.Value, userId);

            return await ShowShipmentPicker(userId, isAdmin,
                "📦 Which shipment would you like to check? Select one below:");
        }

        var systemPrompt =
            $"You are SmartShip's AI logistics assistant. Help with shipping questions.\n" +
            $"Current user: {(isAdmin ? "Admin" : "Customer")} (ID: {userId})\n" +
            $"Rules: Be concise. Use markdown. Only answer logistics/shipping questions.\n" +
            $"If asked about weather, news, or anything unrelated to shipping, say:\n" +
            $"\"I can only help with SmartShip logistics. Type *help for options.\"";

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
            foreach (var h in history.TakeLast(6))
                messages.Add(new
                {
                    role = h.Role == "bot" ? "assistant" : "user",
                    content = h.Text
                });

        messages.Add(new { role = "user", content = userMessage });

        var payload = new { model, messages, stream = false };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"{baseUrl}/api/chat", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned {Status}", response.StatusCode);
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
        "👋 Hello! I'm your **SmartShip AI** assistant.\n\n" +
        "I can help you with tracking, rates, delivery proof, and more.\n" +
        "Type **help** to see everything I can do!",
        "greeting", null);

    private static ChatResponseDto HandleHelp() => new(
        "📋 **SmartShip AI — What I Can Do:**\n\n" +
        "1️⃣  **Track shipment** — \"where is my package?\"\n" +
        "2️⃣  **Shipment status** — \"show my shipments\"\n" +
        "3️⃣  **Delivery ETA** — \"when will my order be delivered?\"\n" +
        "4️⃣  **Delivery proof** — \"was my order delivered?\"\n" +
        "5️⃣  **Rate calculator** — \"rate for 5kg\"\n" +
        "6️⃣  **Rate comparison** — \"which type is cheapest for 3kg?\"\n" +
        "7️⃣  **Documents** — admin only\n" +
        "8️⃣  **Dashboard stats** — \"show summary\" (admin only)\n\n" +
        "Select a shipment when prompted to get live data! 📦",
        "help", null);

    private static ChatResponseDto HandleSmallTalk() => new(
        "😊 I'm here to help with your SmartShip shipments!\n\n" +
        "Ask me about **tracking**, **rates**, **delivery status**, or **proof**.\n" +
        "Type **help** for the full list.",
        "small_talk", null);

    private static ChatResponseDto HandleResetContext() => new(
        "🔄 Context cleared!\n\n" +
        "Which shipment would you like to check next?\n" +
        "Type **track**, **status**, or **delivery proof** to select one.",
        "reset", null);

    private static ChatResponseDto FallbackResponse() => new(
        "🤔 I'm not sure about that.\n\n" +
        "I can help with: 📦 Track · 💰 Rates · 📅 ETA · ✅ Delivery Proof\n\n" +
        "Type **help** for the full list.",
        "fallback", null);
}