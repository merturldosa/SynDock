using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace Shop.Infrastructure.Logging;

public class PiiDestructurePolicy : IDestructuringPolicy
{
    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex PhoneRegex = new(
        @"\d{2,4}-\d{3,4}-\d{4}",
        RegexOptions.Compiled);

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue? result)
    {
        if (value is string str)
        {
            var masked = MaskPii(str);
            if (masked != str)
            {
                result = new ScalarValue(masked);
                return true;
            }
        }

        result = null;
        return false;
    }

    public static string MaskPii(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Mask email: first char + "***@" + domain
        var result = EmailRegex.Replace(input, match =>
        {
            var email = match.Value;
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0) return email;
            var domain = email[(atIndex + 1)..];
            return $"{email[0]}***@{domain}";
        });

        // Mask phone: first 3 digits + "****" + last 4 digits
        result = PhoneRegex.Replace(result, match =>
        {
            var phone = match.Value;
            var digits = phone.Replace("-", "");
            if (digits.Length < 7) return phone;
            return $"{digits[..3]}****{digits[^4..]}";
        });

        return result;
    }
}

public class PiiEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Process all string properties in the log event
        var properties = logEvent.Properties.ToList();
        foreach (var prop in properties)
        {
            if (prop.Value is ScalarValue scalar && scalar.Value is string str)
            {
                var masked = PiiDestructurePolicy.MaskPii(str);
                if (masked != str)
                {
                    logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(prop.Key, masked));
                }
            }
        }
    }
}
