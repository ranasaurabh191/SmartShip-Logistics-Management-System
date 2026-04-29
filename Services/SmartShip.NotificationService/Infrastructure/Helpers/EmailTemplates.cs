namespace SmartShip.NotificationService.Infrastructure.Helpers;

/// <summary>
/// Centralized, branded HTML email templates for all SmartShip notification events.
/// Clean white professional theme with SmartShip red accent.
/// </summary>
public static class EmailTemplates
{
    // ─── Brand tokens ─────────────────────────────────────────────────────────
    private const string BgPage    = "#f4f4f5";   // light grey page bg
    private const string BgCard    = "#ffffff";   // white card
    private const string BgHeader  = "#ffffff";   // header
    private const string BgFooter  = "#f9f9f9";   // footer strip
    private const string BgRow     = "#fafafa";   // alternating label cell
    private const string Accent    = "#e0001a";   // SmartShip red
    private const string BorderRed = "#f5c6cb";   // soft red border
    private const string BorderGry = "#e5e7eb";   // neutral border
    private const string TextMain  = "#1a1a1a";   // near-black body
    private const string TextMuted = "#6b7280";   // grey muted
    private const string TextWhite = "#ffffff";
    private const string Success   = "#059669";   // green
    private const string Warning   = "#d97706";   // amber

    // ─── Inline SVG logo (plane + SMARTSHIP wordmark) ────────────────────────
    private const string LogoSvg = """
<svg xmlns="http://www.w3.org/2000/svg" width="160" height="36" viewBox="0 0 160 36">
  <g transform="translate(0,6)">
    <polygon points="0,12 20,6 20,18" fill="#e0001a"/>
    <polygon points="20,6 26,0 26,24 20,18" fill="#b0001a"/>
  </g>
  <text x="34" y="26"
    font-family="Arial,sans-serif"
    font-size="17" font-weight="800"
    letter-spacing="1.2"
    fill="#1a1a1a">SMART<tspan fill="#e0001a">SHIP</tspan></text>
</svg>
""";

    // ─── Shared page wrapper ──────────────────────────────────────────────────
    private static string Wrap(string bannerBg, string bannerLabel, string bodyHtml, string footerNote)
    {
        return $"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8"/>
  <meta name="viewport" content="width=device-width,initial-scale=1"/>
  <title>SmartShip Notification</title>
</head>
<body style="margin:0;padding:0;background:{BgPage};font-family:Arial,Helvetica,sans-serif;">

<table width="100%" cellpadding="0" cellspacing="0" style="background:{BgPage};padding:36px 0;">
<tr><td align="center">

  <table width="580" cellpadding="0" cellspacing="0"
    style="max-width:580px;width:100%;background:{BgCard};
           border:1px solid {BorderGry};border-radius:6px;
           box-shadow:0 2px 12px rgba(0,0,0,0.07);overflow:hidden;">

    <!-- ══ HEADER ══ -->
    <tr>
      <td style="background:{BgHeader};padding:20px 32px;
                 border-bottom:3px solid {Accent};">
        <table width="100%" cellpadding="0" cellspacing="0">
          <tr>
            <td>{LogoSvg}</td>
            <td align="right"
              style="font-family:Arial,sans-serif;font-size:10px;
                     color:{TextMuted};letter-spacing:1.5px;text-transform:uppercase;">
              Logistics &bull; Tracking &bull; Delivery
            </td>
          </tr>
        </table>
      </td>
    </tr>

    <!-- ══ STATUS BANNER ══ -->
    <tr>
      <td style="background:{bannerBg};padding:14px 32px;">
        <span style="font-family:Arial,sans-serif;font-size:13px;font-weight:800;
                     color:{TextWhite};letter-spacing:1.5px;text-transform:uppercase;">
          {bannerLabel}
        </span>
      </td>
    </tr>

    <!-- ══ BODY ══ -->
    <tr>
      <td style="background:{BgCard};padding:28px 32px;">
        {bodyHtml}
      </td>
    </tr>

    <!-- ══ FOOTER NOTE ══ -->
    <tr>
      <td style="background:{BgRow};padding:14px 32px;
                 border-top:1px solid {BorderGry};">
        <p style="margin:0;font-size:12px;color:{TextMuted};line-height:1.7;">{footerNote}</p>
      </td>
    </tr>

    <!-- ══ FOOTER BAR ══ -->
    <tr>
      <td style="background:{BgFooter};padding:16px 32px;
                 border-top:1px solid {BorderGry};text-align:center;">
        <p style="margin:0;font-size:11px;color:{TextMuted};">
          &copy; {DateTime.Now.Year} SmartShip Logistics &bull;
          <a href="#" style="color:{Accent};text-decoration:none;">Track Shipment</a> &bull;
          <a href="#" style="color:{Accent};text-decoration:none;">Support</a>
        </p>
        <p style="margin:4px 0 0;font-size:10px;color:#aaa;letter-spacing:1px;">
          AUTOMATED NOTIFICATION &mdash; PLEASE DO NOT REPLY
        </p>
      </td>
    </tr>

  </table>
</td></tr>
</table>
</body>
</html>
""";
    }

    // ─── Data row (label | value) ─────────────────────────────────────────────
    private static string Row(string label, string value) => $"""
<tr>
  <td width="38%" style="padding:10px 14px;background:{BgRow};font-size:11px;font-weight:700;
      color:{TextMuted};letter-spacing:1px;text-transform:uppercase;
      border-bottom:1px solid {BorderGry};border-right:1px solid {BorderGry};">
    {label}
  </td>
  <td style="padding:10px 14px;background:{BgCard};font-size:13px;color:{TextMain};
      border-bottom:1px solid {BorderGry};">
    {value}
  </td>
</tr>
""";

    // ─── Status badge ─────────────────────────────────────────────────────────
    private static string Badge(string text, string bg, string color) =>
        $"<span style=\"display:inline-block;background:{bg};color:{color};" +
        $"border-radius:3px;padding:2px 10px;font-size:11px;font-weight:700;" +
        $"letter-spacing:1px;text-transform:uppercase;\">{text}</span>";

    // ─── Table wrapper ────────────────────────────────────────────────────────
    private static string Table(string rows) => $"""
<table width="100%" cellpadding="0" cellspacing="0"
  style="border:1px solid {BorderGry};border-radius:4px;
         border-collapse:collapse;margin-bottom:20px;">
  {rows}
</table>
""";

    // =========================================================================
    // PUBLIC TEMPLATES
    // =========================================================================

    /// <summary>Welcome email on new account creation.</summary>
    public static string WelcomeEmail(string name, string email, string role)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Hello <strong>{name}</strong>, your SmartShip account has been created successfully.
  You can now log in and start creating shipments.
</p>
{Table(
    Row("Name",         $"<strong>{name}</strong>") +
    Row("Email",        email) +
    Row("Account Type", Badge(role, Accent, TextWhite))
)}
""";
        return Wrap(Accent, "Welcome to SmartShip", body,
            "Log in to your SmartShip dashboard to create your first shipment and track deliveries in real time.");
    }

    /// <summary>Sent when a new shipment is created.</summary>
    public static string ShipmentCreated(string trackingNumber, string senderCity, string createdAt)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Your shipment has been registered in the SmartShip network.
  Please complete payment and schedule a pickup to get it moving.
</p>
{Table(
    Row("Tracking No.", $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
    Row("Origin",       senderCity) +
    Row("Created At",   createdAt) +
    Row("Status",       Badge("Pending Payment", Warning, TextWhite))
)}
""";
        return Wrap("#d97706", "Shipment Created", body,
            "Once payment is confirmed and pickup is scheduled, your package will be collected and dispatched.");
    }

    /// <summary>Sent on any status transition.</summary>
    public static string ShipmentStatusUpdated(
        string trackingNumber, string oldStatus, string newStatus,
        string location, string updatedAt)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Your shipment status has been updated. Here is the latest information from the SmartShip network.
</p>
{Table(
    Row("Tracking No.",     $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
    Row("Current Status",   Badge(newStatus,  Accent,    TextWhite)) +
    Row("Current Location", location) +
    Row("Updated At",       updatedAt)
)}
""";
        return Wrap(Accent, "Shipment Status Updated", body,
            "Log in to your SmartShip dashboard to view the live route map and full tracking timeline.");
    }

    /// <summary>Sent when delivered.</summary>
    public static string ShipmentDelivered(string trackingNumber, string deliveredAt)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Your shipment has been successfully delivered. Thank you for choosing SmartShip!
</p>
{Table(
    Row("Tracking No.",  $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
    Row("Delivered At",  deliveredAt) +
    Row("Status",        Badge("Delivered", Success, TextWhite))
)}
""";
        return Wrap(Success, "Shipment Delivered", body,
            "If you have any issue with this delivery, please contact our support team within 48 hours.");
    }

    /// <summary>Sent when cancelled.</summary>
    public static string ShipmentCancelled(string trackingNumber, string cancelledAt)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Your shipment has been cancelled. If this was not intentional, please contact our support team immediately.
</p>
{Table(
    Row("Tracking No.",  $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
    Row("Cancelled At",  cancelledAt) +
    Row("Status",        Badge("Cancelled", Accent, TextWhite))
)}
""";
        return Wrap(Accent, "Shipment Cancelled", body,
            "Any eligible refunds will be processed within 5 to 7 business days. Contact support for further assistance.");
    }

    /// <summary>Sent when payment is confirmed — includes a full invoice.</summary>
    public static string PaymentCompleted(
        string trackingNumber,
        int shipmentId,
        string paymentMethod,
        string paymentStatus,
        decimal amount,
        string? paidAt,
        string? razorpayPaymentId,
        string? razorpayOrderId)
    {
        var isCod      = paymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase);
        var invoiceNo  = $"INV-{shipmentId:D6}-{DateTime.Now:yyyyMM}";
        var issuedDate = DateTime.Now.ToString("dd-MMM-yyyy");
        var baseAmount = Math.Round(amount / 1.18m, 2);
        var gst        = Math.Round(amount - baseAmount, 2);

        var paymentRows =
            Row("Invoice No.",    invoiceNo) +
            Row("Tracking No.",   $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
            Row("Shipment ID",    $"#{shipmentId}") +
            Row("Issue Date",     issuedDate) +
            Row("Payment Method", Badge(paymentMethod, isCod ? Warning : "#1d4ed8", TextWhite)) +
            Row("Status",         Badge(paymentStatus, Success, TextWhite));

        if (!string.IsNullOrEmpty(paidAt))
            paymentRows += Row("Paid At", paidAt);
        if (!string.IsNullOrEmpty(razorpayOrderId))
            paymentRows += Row("Order ID", $"<span style=\"font-family:Courier New,monospace;font-size:12px;\">{razorpayOrderId}</span>");
        if (!string.IsNullOrEmpty(razorpayPaymentId))
            paymentRows += Row("Payment ID", $"<span style=\"font-family:Courier New,monospace;font-size:12px;\">{razorpayPaymentId}</span>");

        var invoiceTable = $"""
<table width="100%" cellpadding="0" cellspacing="0"
  style="border:1px solid {BorderGry};border-radius:4px;border-collapse:collapse;margin-bottom:20px;">
  <tr style="background:{Accent};">
    <td style="padding:10px 14px;font-size:11px;font-weight:800;
        color:{TextWhite};letter-spacing:1.5px;text-transform:uppercase;" width="60%">
      Description
    </td>
    <td align="right"
      style="padding:10px 14px;font-size:11px;font-weight:800;
             color:{TextWhite};letter-spacing:1.5px;text-transform:uppercase;">
      Amount (INR)
    </td>
  </tr>
  <tr>
    <td style="padding:12px 14px;font-size:13px;color:{TextMain};
        border-bottom:1px solid {BorderGry};">
      Shipment Freight Charges
      <br/><span style="font-size:11px;color:{TextMuted};">Tracking: {trackingNumber}</span>
    </td>
    <td align="right"
      style="padding:12px 14px;font-size:13px;color:{TextMain};
             border-bottom:1px solid {BorderGry};">
      Rs. {baseAmount:F2}
    </td>
  </tr>
  <tr>
    <td style="padding:10px 14px;font-size:12px;color:{TextMuted};
        background:{BgRow};border-bottom:1px solid {BorderGry};">
      GST @ 18%
    </td>
    <td align="right"
      style="padding:10px 14px;font-size:12px;color:{TextMuted};
             background:{BgRow};border-bottom:1px solid {BorderGry};">
      Rs. {gst:F2}
    </td>
  </tr>
  <tr style="background:#f0fdf4;">
    <td style="padding:14px 14px;font-size:13px;font-weight:800;color:{TextMain};text-transform:uppercase;letter-spacing:1px;">
      Total Paid
    </td>
    <td align="right"
      style="padding:14px 14px;font-size:18px;font-weight:800;color:{Success};">
      Rs. {amount:F2}
    </td>
  </tr>
</table>
""";

        var intro = isCod
            ? $"Your <strong>Cash on Delivery</strong> order has been confirmed and your shipment is now booked."
            : $"Your payment has been <strong>verified and confirmed</strong>. Your shipment is now booked.";

        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  {intro} You can now schedule a pickup from your SmartShip dashboard.
</p>

<p style="font-size:11px;font-weight:800;color:{Accent};letter-spacing:1.5px;
   text-transform:uppercase;margin:0 0 8px;">
  Payment Details
</p>
{Table(paymentRows)}

<p style="font-size:11px;font-weight:800;color:{Accent};letter-spacing:1.5px;
   text-transform:uppercase;margin:0 0 8px;">
  Invoice
</p>
{invoiceTable}
""";

        return Wrap(Success, "Payment Confirmed", body,
            "This is your official payment receipt. Please save this email for your records. " +
            "Schedule a pickup from your SmartShip dashboard to dispatch your shipment.");
    }

    /// <summary>Sent when payment fails.</summary>
    public static string PaymentFailed(string trackingNumber, string failedAt)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Your payment attempt was unsuccessful and your shipment order has been cancelled.
  Please try again by creating a new shipment.
</p>
{Table(
    Row("Tracking No.", $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
    Row("Failed At",    failedAt) +
    Row("Status",       Badge("Payment Failed", Accent, TextWhite))
)}
""";
        return Wrap(Accent, "Payment Failed", body,
            "If the amount was deducted from your account, it will be automatically reversed within 5 to 7 business days. Contact your bank or our support team for assistance.");
    }

    /// <summary>Sent when a refund is processed.</summary>
    public static string PaymentRefunded(string trackingNumber, string amount, string refundedAt)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 20px;line-height:1.7;">
  Your refund has been successfully processed for the cancelled shipment below.
</p>
{Table(
    Row("Tracking No.",  $"<strong style=\"font-family:Courier New,monospace;font-size:14px;letter-spacing:2px;\">{trackingNumber}</strong>") +
    Row("Refund Amount", $"<strong style=\"font-size:16px;color:{Success};\">Rs. {amount}</strong>") +
    Row("Refunded At",   refundedAt) +
    Row("Status",        Badge("Refund Processed", Success, TextWhite))
)}
""";
        return Wrap(Success, "Refund Processed", body,
            "The refund will reflect in your original payment account within 5 to 7 business days, depending on your bank or payment provider.");
    }

    /// <summary>OTP verification email.</summary>
    public static string OtpVerification(string otp)
    {
        var body = $"""
<p style="font-size:14px;color:{TextMain};margin:0 0 24px;text-align:center;line-height:1.7;">
  Use the verification code below to complete your request.<br/>
  This code expires in <strong>5 minutes</strong>.
</p>
<div style="text-align:center;margin:0 0 24px;">
  <div style="display:inline-block;background:{BgRow};border:2px solid {Accent};
              border-radius:6px;padding:20px 48px;">
    <span style="font-family:Courier New,monospace;font-size:44px;font-weight:900;
                 letter-spacing:14px;color:{Accent};">{otp}</span>
  </div>
</div>
<p style="font-size:12px;color:{TextMuted};text-align:center;margin:0;">
  Do not share this code with anyone, including SmartShip staff.
</p>
""";
        return Wrap(Accent, "OTP Verification", body,
            "If you did not request this verification code, please ignore this email and ensure your SmartShip account is secure.");
    }
}
