using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Shop.API.Middleware;

public class PiiMaskingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PiiMaskingMiddleware> _logger;

    // Patterns for PII detection
    private static readonly Regex EmailPattern = new(
        @"""([a-zA-Z0-9._%+\-]+)@([a-zA-Z0-9.\-]+\.[a-zA-Z]{2,})""",
        RegexOptions.Compiled);

    private static readonly Regex PhonePattern = new(
        @"""(\d{2,4})-(\d{3,4})-(\d{4})""",
        RegexOptions.Compiled);

    public PiiMaskingMiddleware(RequestDelegate next, ILogger<PiiMaskingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only intercept JSON API responses
        if (!IsApiRequest(context))
        {
            await _next(context);
            return;
        }

        // Capture the original response body
        var originalBodyStream = context.Response.Body;
        using var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        await _next(context);

        memoryStream.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

        // Only mask JSON responses with success status codes
        if (context.Response.ContentType?.Contains("application/json") == true
            && !string.IsNullOrEmpty(responseBody))
        {
            var currentUserId = GetCurrentUserId(context);
            var currentUserEmail = GetCurrentUserEmail(context);
            responseBody = MaskPiiInResponse(responseBody, currentUserId, currentUserEmail);
        }

        // Write the (potentially masked) response back
        context.Response.Body = originalBodyStream;
        var bytes = Encoding.UTF8.GetBytes(responseBody);
        context.Response.ContentLength = bytes.Length;
        await context.Response.Body.WriteAsync(bytes);
    }

    private static bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        // Skip auth endpoints — they always return self-data
        if (path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
            return false;
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    private static int? GetCurrentUserId(HttpContext context)
    {
        var idClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return idClaim != null && int.TryParse(idClaim, out var id) ? id : null;
    }

    private static string? GetCurrentUserEmail(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
    }

    private string MaskPiiInResponse(string json, int? currentUserId, string? currentUserEmail)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
                return json;

            MaskNode(node, currentUserId, currentUserEmail);
            return node.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException)
        {
            // Fallback: regex-based masking for malformed or non-standard JSON
            return MaskPiiWithRegex(json, currentUserEmail);
        }
    }

    private void MaskNode(JsonNode node, int? currentUserId, string? currentUserEmail)
    {
        if (node is JsonObject obj)
        {
            // Check if this object represents the current user
            var isOwnData = IsOwnUserData(obj, currentUserId, currentUserEmail);

            if (!isOwnData)
            {
                MaskObjectFields(obj);
            }

            // Recurse into child nodes
            foreach (var kvp in obj.ToList())
            {
                if (kvp.Value is JsonObject or JsonArray)
                {
                    MaskNode(kvp.Value!, currentUserId, currentUserEmail);
                }
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not null)
                    MaskNode(item, currentUserId, currentUserEmail);
            }
        }
    }

    private static bool IsOwnUserData(JsonObject obj, int? currentUserId, string? currentUserEmail)
    {
        // Check by userId field
        if (currentUserId.HasValue)
        {
            if (obj.TryGetPropertyValue("userId", out var userIdNode)
                && userIdNode is JsonValue userIdVal
                && userIdVal.TryGetValue<int>(out var uid)
                && uid == currentUserId.Value)
            {
                return true;
            }

            // Also check 'id' at top level (for user profile responses)
            if (obj.TryGetPropertyValue("id", out var idNode)
                && idNode is JsonValue idVal
                && idVal.TryGetValue<int>(out var id)
                && id == currentUserId.Value
                && obj.ContainsKey("email"))
            {
                return true;
            }
        }

        // Check by email
        if (currentUserEmail is not null
            && obj.TryGetPropertyValue("email", out var emailNode)
            && emailNode is JsonValue emailVal
            && emailVal.TryGetValue<string>(out var email)
            && string.Equals(email, currentUserEmail, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static void MaskObjectFields(JsonObject obj)
    {
        // Mask email field
        if (obj.TryGetPropertyValue("email", out var emailNode)
            && emailNode is JsonValue emailVal
            && emailVal.TryGetValue<string>(out var email)
            && !string.IsNullOrEmpty(email))
        {
            obj["email"] = MaskEmail(email);
        }

        // Mask phone field
        if (obj.TryGetPropertyValue("phone", out var phoneNode)
            && phoneNode is JsonValue phoneVal
            && phoneVal.TryGetValue<string>(out var phone)
            && !string.IsNullOrEmpty(phone))
        {
            obj["phone"] = MaskPhone(phone);
        }
    }

    internal static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return email;

        var localPart = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        return $"{localPart[0]}***@{domain}";
    }

    internal static string MaskPhone(string phone)
    {
        // Handle formats like 010-1234-5678, 02-123-4567, etc.
        var match = Regex.Match(phone, @"^(\d{2,4})-?\d{3,4}-?(\d{4})$");
        if (match.Success)
        {
            return $"{match.Groups[1].Value}-****-{match.Groups[2].Value}";
        }

        // For other formats, mask middle portion
        if (phone.Length >= 7)
        {
            var prefix = phone[..3];
            var suffix = phone[^4..];
            return $"{prefix}****{suffix}";
        }

        return phone;
    }

    private static string MaskPiiWithRegex(string text, string? currentUserEmail)
    {
        // Mask emails (but not the current user's)
        text = EmailPattern.Replace(text, match =>
        {
            var fullEmail = $"{match.Groups[1].Value}@{match.Groups[2].Value}";
            if (currentUserEmail != null
                && string.Equals(fullEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                return match.Value;
            }
            return $"\"{match.Groups[1].Value[0]}***@{match.Groups[2].Value}\"";
        });

        // Mask phone numbers
        text = PhonePattern.Replace(text, match =>
            $"\"{match.Groups[1].Value}-****-{match.Groups[3].Value}\"");

        return text;
    }
}
