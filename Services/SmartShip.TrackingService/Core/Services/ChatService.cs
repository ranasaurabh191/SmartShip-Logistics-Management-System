using SmartShip.TrackingService.Core.DTOs;
using SmartShip.TrackingService.Core.Interfaces.Repositories;
using SmartShip.TrackingService.Core.Interfaces.Services;
using SmartShip.TrackingService.Domain.Entities;
using System.Text.Json;
using System.Text.RegularExpressions;

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

    private static readonly Dictionary<string, (string Response, DateTime CachedAt)> _cache = new();

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

        var extracted = ExtractTrackingNumber(req.Message);
        if (extracted != null && activeShipmentId == null)
        {
            var all = await _shipmentClient.GetUserShipmentsAsync(userId, isAdmin);
            var match = all.FirstOrDefault(s =>
                s.TrackingNumber.Equals(extracted, StringComparison.OrdinalIgnoreCase));
            if (match != null) activeShipmentId = match.Id;
        }

        try
        {
            if (intent == "greeting") return StaticGreeting();
            if (intent == "reset_context") return StaticResetContext();

            if (intent == "admin_stats" && !isAdmin)
                return await AskOllama(req, userId, false,
                    "Dashboard stats are only available to admins. Tell the user politely.");

            if (intent == "list_documents" && !isAdmin)
                return await AskOllama(req, userId, false,
                    "Document access is admin-only. Customer can check delivery proof. Tell the user politely.");

            if (intent == "revenue_stats" && !isAdmin)
                return await AskOllama(req, userId, false,
                    "Revenue and earnings information is only available to admins. Tell the user politely.");

            if (intent == "revenue_stats")
                return await HandleRevenue(userId, req);

            if (intent is "track_shipment" or "delivery_eta"
                       or "shipment_status" or "delivery_proof" or "list_documents"
                       or "admin_stats")
            {
                if (intent == "admin_stats")
                    return await HandleAdminStats(userId, req);

                if (intent == "list_documents")
                {
                    if (activeShipmentId == null)
                        return await ShowShipmentPicker(userId, isAdmin,
                            "Which shipment's documents do you want to see?");
                    return await HandleDocuments(req, userId);
                }

                if (activeShipmentId == null)
                    return await ShowShipmentPicker(userId, isAdmin,
                        GetPickerPrompt(intent));

                return await HandleShipmentIntent(intent, req, activeShipmentId.Value, userId);
            }
            if (intent is "rate_calculate" or "rate_general" or "rate_compare")
                return await HandleRateWithOllama(req);

            return await AskOllama(req, userId, isAdmin, null, activeShipmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat processing failed for User {UserId}", userId);
            return new ChatResponseDto(
                "Something went wrong. Please try again.", "error", null);
        }
    }


    private static string DetectIntent(string message)
    {
        var msg = message.ToLower().Trim();

        if (msg.Length < 3) return "small_talk";

        if (Regex.IsMatch(msg, @"ss\d{10,}") ||
            Has(msg, "tell me about shipment", "about shipment", "info about",
                     "details of shipment", "shipment ss"))
            return "track_shipment";

        if (msg is "hi" or "hello" or "hey" or "good morning" or "hii"
               or "good evening" or "namaste" or "helo" or "heyy")
            return "greeting";

        if (msg is "check another" or "reset" or "another shipment" or "change shipment"
               or "different shipment" or "clear context" or "switch shipment"
               or "check another shipment" or "show another")
            return "reset_context";

        if (Has(msg, "rate", "price", "cost", "charge", "how much", "fee", "pricing"))
        {
            if (Has(msg, "cheapest", "compare", "which type", "best", "comparison"))
                return "rate_compare";
            return HasNumber(msg) ? "rate_calculate" : "rate_general";
        }
        if (Has(msg, "cheapest", "compare rates", "best rate")) return "rate_compare";

        if (Has(msg, "document", "invoice", "label", "file", "attachment"))
            return "list_documents";

        if (Has(msg, "proof", "delivery proof", "signature", "confirm delivery"))
            return "delivery_proof";

        if (Has(msg, "when will", "eta", "delivery time", "expected", "how many days"))
            return "delivery_eta";

        if (Has(msg, "track", "where is", "where are", "location", "transit", "in transit"))
            return "track_shipment";

        if (Has(msg, "status", "my shipments", "show shipments",
                "list shipments", "all shipments"))
            return "shipment_status";

        if (Has(msg, "revenue", "earnings", "income", "total revenue",
             "how much earned", "money collected", "amount collected",
             "billed", "total billed", "pending revenue"))
            return "revenue_stats";

        if (Has(msg, "total shipments", "pending count", "dashboard",
                "summary", "how many", "stats", "analytics"))
            return "admin_stats";

        return "unknown";
    }

    private static bool Has(string msg, params string[] kw)
        => kw.Any(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));

    private static bool HasNumber(string msg) => msg.Any(char.IsDigit);

    private static string? ExtractTrackingNumber(string message)
    {
        var m = Regex.Match(message, @"SS\d{10,}", RegexOptions.IgnoreCase);
        return m.Success ? m.Value.ToUpper() : null;
    }


    private async Task<ChatResponseDto> HandleShipmentIntent(
        string intent, ChatMessageRequest req, int shipmentId, int userId)
    {
        var shipment = await _shipmentClient.GetShipmentByIdAsync(shipmentId);
        if (shipment == null)
            return new ChatResponseDto(
                "I couldn't find that shipment. Please try selecting again.", "error", null);

        _logger.LogInformation("Shipment {Id} Status='{Status}' Payment='{Payment}'",
            shipmentId, shipment.Status, shipment.PaymentStatus);

        var events = await _trackingRepo.GetByTrackingNumberPagedAsync(
            shipment.TrackingNumber,
            new TrackingEventPagedRequest { Page = 1, PageSize = 20 });

        var allEvents = events.Data.ToList();
        var latestEvent = allEvents.LastOrDefault();

        DeliveryProof? proof = null;
        if (shipment.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
            proof = await _deliveryProofRepo.GetByShipmentIdAsync(shipmentId);

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

        var trackingHistory = allEvents.Any()
            ? string.Join("\n", allEvents.Select(e =>
                $"  - {e.EventTime:dd-MMM-yyyy HH:mm} | {e.Status} | {e.Location} | {e.Description}"))
            : "  No tracking events yet.";

        var proofSection = proof != null
            ? $"Delivered at: {proof.DeliveredAt:dd-MMM-yyyy HH:mm}\n" +
              $"Received by: {proof.ReceiverName}\n" +
              $"Delivered by: {proof.DeliveredBy}\n" +
              $"Notes: {proof.Notes ?? "None"}"
            : "No delivery proof on record.";

        var systemPrompt = $"""
            You are SmartShip AI, a helpful logistics assistant.
            Answer the user's question using ONLY the shipment data below.
            Be conversational, concise (under 120 words), and use clean markdown.
            Do NOT use excessive emojis — use at most 2 per response.
            Do NOT invent any data, dates, or locations not present below.

            === SHIPMENT DATA ===
            Tracking Number  : {shipment.TrackingNumber}
            Type             : {shipment.ShipmentType}
            Current Status   : {shipment.Status}
            Payment Status   : {shipment.PaymentStatus ?? "Unknown"}
            Weight           : {shipment.WeightKg} kg
            Route            : {shipment.OriginCity} to {shipment.DestinationCity}
            Created          : {createdAt:dd-MMM-yyyy HH:mm}
            Expected Delivery: {expectedDelivery:dd-MMM-yyyy} ({(daysRemaining > 0 ? $"in {daysRemaining} days" : isOverdue ? "overdue" : "today")})

            === TRACKING HISTORY ===
            {trackingHistory}

            === LATEST EVENT ===
            {(latestEvent == null ? "None" : $"{latestEvent.Status} at {latestEvent.Location} on {latestEvent.EventTime:dd-MMM-yyyy HH:mm} — {latestEvent.Description}")}

            === DELIVERY PROOF ===
            {proofSection}
            =====================

            RULES:
            - If Status = Delivered: confirm delivery using proof details above
            - If Status = Cancelled: say cancelled, give no delivery date
            - If payment status is not Paid: mention payment is pending
            - If no tracking events: say pickup has not been scheduled yet
            - For ETA: use Expected Delivery date above only
            - Do not say "in transit" if status says Delivered or Cancelled
            """;

        var reply = await CallOllama(req.Message, systemPrompt, req.History);
        return new ChatResponseDto(reply, intent, null);
    }

    private async Task<ChatResponseDto> HandleRevenue(
    int userId, ChatMessageRequest req)
    {
        var shipments = await _shipmentClient.GetUserShipmentsAsync(userId, true);

        var activeShipments = shipments.Where(s => !s.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();

        var totalRevenue = activeShipments.Sum(s => s.ShippingRate);
        var collectedRev = activeShipments
            .Where(s => string.Equals(s.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.ShippingRate);
        var pendingRev = totalRevenue - collectedRev;
        var deliveredRev = activeShipments
            .Where(s => s.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
            .Sum(s => s.ShippingRate);

        var systemPrompt = $"""
        You are SmartShip AI, an admin financial assistant.
        Answer the admin's revenue question using ONLY the data below.
        Be direct and professional. Use markdown. Under 100 words.

        === REVENUE SUMMARY (as of {DateTime.Now:dd-MMM-yyyy HH:mm}) ===
        Total Billed     : Rs {totalRevenue:N0}
        Collected (Paid) : Rs {collectedRev:N0}
        Pending          : Rs {pendingRev:N0}
        From Delivered   : Rs {deliveredRev:N0}
        Total Shipments  : {activeShipments.Count} (excluding cancelled)
        ================================================================

        Do not invent any numbers. Only use the values above.
        """;

        var reply = await CallOllama(req.Message, systemPrompt, req.History);
        return new ChatResponseDto(reply, "revenue_stats", null);
    }
    private async Task<ChatResponseDto> HandleAdminStats(
        int userId, ChatMessageRequest req)
    {
        var shipments = await _shipmentClient.GetUserShipmentsAsync(userId, true);
        _logger.LogInformation("Admin stats: fetched {Count} shipments", shipments.Count);

        var total = shipments.Count;
        var pending = shipments.Count(s => s.Status.Contains("Pending", StringComparison.OrdinalIgnoreCase));
        var booked = shipments.Count(s => s.Status.Contains("Booked", StringComparison.OrdinalIgnoreCase));
        var transit = shipments.Count(s => s.Status.Contains("Transit", StringComparison.OrdinalIgnoreCase));
        var delivered = shipments.Count(s => s.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase));
        var cancelled = shipments.Count(s => s.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));
        var draft = shipments.Count(s => s.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase));
        var paid = shipments.Count(s => string.Equals(s.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase));
        var unpaid = total - cancelled - paid;

        var systemPrompt = $"""
            You are SmartShip AI, an admin assistant.
            Answer the admin's question using ONLY the stats below.
            Be direct and professional. Use markdown table if listing stats.
            Do not use excessive emojis. Under 100 words.

            === LIVE SHIPMENT STATS (as of {DateTime.Now:dd-MMM-yyyy HH:mm}) ===
            Total Shipments : {total}
            Draft           : {draft}
            Pending Pickup  : {pending}
            Booked          : {booked}
            In Transit      : {transit}
            Delivered       : {delivered}
            Cancelled       : {cancelled}
            Paid            : {paid}
            Unpaid          : {unpaid}
            ===================================================================

            If the admin asks about hubs, users, or revenue — say those details
            are available in the Admin Panel, not here.
            """;

        var reply = await CallOllama(req.Message, systemPrompt, req.History);
        return new ChatResponseDto(reply, "admin_stats", null);
    }

    private async Task<ChatResponseDto> HandleRateWithOllama(ChatMessageRequest req)
    {
        double weightKg = 0;
        foreach (var w in req.Message.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var clean = w.Replace("kg", "", StringComparison.OrdinalIgnoreCase).Trim();
            if (double.TryParse(clean, out var p) && p > 0) { weightKg = p; break; }
        }

        decimal express = weightKg > 0 ? Math.Max((decimal)(weightKg * 150), 99) : 99;
        decimal international = weightKg > 0 ? Math.Max((decimal)(weightKg * 300), 99) : 99;
        decimal freight = weightKg > 0 ? Math.Max((decimal)(weightKg * 50), 99) : 99;
        decimal domestic = weightKg > 0 ? Math.Max((decimal)(weightKg * 80), 99) : 99;

        var systemPrompt = $"""
            You are SmartShip AI. Answer the user's shipping rate question.
            Use ONLY the pre-calculated rates below. Do not invent or modify any numbers.
            Be helpful and recommend the best option based on the user's need.
            Use clean markdown. No excessive emojis. Under 100 words.

            === SMARTSHIP RATE FORMULA ===
            Express       : Rs 150/kg, minimum Rs 99
            International : Rs 300/kg, minimum Rs 99
            Freight       : Rs 50/kg,  minimum Rs 99
            Domestic      : Rs 80/kg,  minimum Rs 99

            {(weightKg > 0 ? $"""
            === CALCULATED RATES FOR {weightKg} kg ===
            Express       : Rs {express:N0}
            International : Rs {international:N0}
            Freight       : Rs {freight:N0}
            Domestic      : Rs {domestic:N0}
            Cheapest      : Freight at Rs {freight:N0}
            """ : "No weight was specified — give general rate table.")}
            ==============================
            """;

        var reply = await CallOllama(req.Message, systemPrompt, req.History);
        return new ChatResponseDto(reply, "rate", null);
    }


    private async Task<ChatResponseDto> HandleDocuments(
        ChatMessageRequest req, int userId)
    {
        var shipmentId = req.SelectedShipmentId ?? req.ShipmentId!.Value;

        var docs = await _documentRepo.GetPagedByShipmentIdAsync(
            shipmentId, new DocumentPagedRequest { Page = 1, PageSize = 20 });

        if (!docs.Data.Any())
            return new ChatResponseDto(
                "No documents have been uploaded for this shipment yet.",
                "list_documents", null);

        var docList = string.Join("\n", docs.Data.Select((d, i) =>
            $"  {i + 1}. {d.FileName} — {d.DocumentType} — uploaded {d.UploadedAt:dd-MMM-yyyy}"));

        var systemPrompt = $"""
            You are SmartShip AI. The admin asked about shipment documents.
            List the documents below clearly. Be brief and professional.
            Mention they can download from the shipment details page.
            No excessive emojis.

            === DOCUMENTS ===
            {docList}
            Total: {docs.TotalCount} document(s)
            =================
            """;

        var reply = await CallOllama(req.Message, systemPrompt, null);
        return new ChatResponseDto(reply, "list_documents", docs.Data);
    }


    private async Task<ChatResponseDto> AskOllama(
    ChatMessageRequest req, int userId, bool isAdmin,
    string? extraContext, int? activeShipmentId = null)   
    {
        string shipmentContext = "";
        if (activeShipmentId.HasValue)
        {
            var shipment = await _shipmentClient.GetShipmentByIdAsync(activeShipmentId.Value);
            if (shipment != null)
            {
                var today = DateTime.Now;
                var createdAt = shipment.CreatedAt != default ? shipment.CreatedAt : today;
                var expectedDelivery = createdAt.AddDays(7);
                var daysRemaining = Math.Max(0, (expectedDelivery - today).Days);
                var isOverdue = today > expectedDelivery
                                && shipment.Status is not "Delivered" and not "Cancelled";

                shipmentContext = $"""

                === ACTIVE SHIPMENT CONTEXT ===
                Tracking Number  : {shipment.TrackingNumber}
                Type             : {shipment.ShipmentType}
                Status           : {shipment.Status}
                Payment Status   : {(string.IsNullOrEmpty(shipment.PaymentStatus) ? "Unknown" : shipment.PaymentStatus)}
                Shipping Rate    : Rs {shipment.ShippingRate:N0}
                Weight           : {shipment.WeightKg} kg
                Route            : {shipment.OriginCity} to {shipment.DestinationCity}
                Created          : {createdAt:dd-MMM-yyyy HH:mm}
                Expected Delivery: {expectedDelivery:dd-MMM-yyyy} ({(daysRemaining > 0 ? $"in {daysRemaining} days" : isOverdue ? "overdue" : "today")})
                ================================
                Use this data to answer follow-up questions about this shipment.
                Do NOT invent any values not present above. Also when user asks about any private information
                like shipment id or user id , you must not disclose it.
                """;
            }
        }

        var systemPrompt = $"""
        You are SmartShip AI, a logistics assistant for SmartShip courier service.
        Help users with shipping, tracking, rates, delivery, and logistics questions.

        User role : {(isAdmin ? "Admin" : "Customer")}

        {(extraContext != null ? $"Context: {extraContext}" : "")}
        {shipmentContext}

        SmartShip capabilities:
        - Track shipments by tracking number (format: SS + digits, e.g. SS2026041714261)
        - Shipping types: Domestic, Express, International, Freight
        - Rates: Domestic Rs80/kg, Express Rs150/kg, Freight Rs50/kg, International Rs300/kg
        - Minimum charge: Rs99 for all types
        - Customers can view delivery proof after delivery
        - Admins can access documents and full dashboard

        Rules:
        - Dont show user id to the user on answer.
        - Be concise (under 100 words), conversational, use markdown
        - Do not use excessive emojis — max 2 per reply
        - Only answer logistics and shipping related questions
        - If asked about unrelated topics politely redirect
        - Never invent shipment data, tracking numbers, or delivery dates
        - If the user asks about amount, cost, or rate — use Shipping Rate from context above
        - If unsure, suggest using the SmartShip dashboard
        """;

        if (activeShipmentId.HasValue)
        {
            var reply = await CallOllama(req.Message, systemPrompt, req.History);
            return new ChatResponseDto(reply, "ai", null);
        }

        var cacheKey = $"general:{req.Message.ToLower().Trim()}";
        if (_cache.TryGetValue(cacheKey, out var cached)
            && DateTime.Now - cached.CachedAt < TimeSpan.FromMinutes(30))
        {
            _logger.LogInformation("Cache hit for: {Message}", req.Message);
            return new ChatResponseDto(cached.Response, "ai_cached", null);
        }

        var generalReply = await CallOllama(req.Message, systemPrompt, req.History);
        _cache[cacheKey] = (generalReply, DateTime.Now);
        return new ChatResponseDto(generalReply, "ai", null);
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
                return "I'm having trouble connecting to the AI service. Please try again.";
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
            return "I'm unable to respond right now. Please try again shortly.";
        }
    }


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
            Label = $"{s.TrackingNumber} · {s.ShipmentType} · {s.OriginCity} → {s.DestinationCity}",
            Status = s.Status
        }).ToList();

        return new ChatResponseDto(prompt, "shipment_picker", null, chips);
    }

    private static string GetPickerPrompt(string intent) => intent switch
    {
        "track_shipment" => "Which shipment would you like to track?",
        "delivery_eta" => "Which shipment do you want the ETA for?",
        "shipment_status" => "Which shipment's status do you want to check?",
        "delivery_proof" => "Which shipment's delivery proof do you want?",
        _ => "Please select a shipment to continue:"
    };


    private static ChatResponseDto StaticGreeting() => new(
        "Hello! I'm your SmartShip AI assistant.\n\n" +
        "I can help with tracking, rates, delivery proof, and more.\n" +
        "Type **help** to see everything I can do.",
        "greeting", null);

    private static ChatResponseDto StaticResetContext() => new(
        "Context cleared. Which shipment would you like to check next?\n\n" +
        "Type **track**, **status**, or **delivery proof** to get started.",
        "reset", null);
}