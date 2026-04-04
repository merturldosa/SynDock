using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Storage;

/// <summary>
/// S3-compatible file storage service (Cloudflare R2, AWS S3, MinIO).
/// Uses AWS Signature V4 for authentication.
/// Falls back to LocalFileStorageService when S3 is not configured or on upload failure.
/// </summary>
public class CdnFileStorageService : IFileStorageService
{
    private readonly HttpClient _httpClient;
    private readonly LocalFileStorageService _localFallback;
    private readonly ILogger<CdnFileStorageService> _logger;
    private readonly string _endpoint;
    private readonly string _bucketName;
    private readonly string _accessKey;
    private readonly string _secretKey;
    private readonly string _publicUrl;
    private readonly string _region;
    private readonly bool _isConfigured;

    public CdnFileStorageService(
        HttpClient httpClient,
        IConfiguration config,
        LocalFileStorageService localFallback,
        ILogger<CdnFileStorageService> logger)
    {
        _httpClient = httpClient;
        _localFallback = localFallback;
        _logger = logger;

        _endpoint = config["Storage:S3:Endpoint"]?.TrimEnd('/') ?? "";
        _bucketName = config["Storage:S3:BucketName"] ?? "syndock-uploads";
        _accessKey = config["Storage:S3:AccessKey"] ?? "";
        _secretKey = config["Storage:S3:SecretKey"] ?? "";
        _publicUrl = config["Storage:S3:PublicUrl"]?.TrimEnd('/') ?? "";
        _region = config["Storage:S3:Region"] ?? "auto"; // "auto" for Cloudflare R2

        _isConfigured = !string.IsNullOrEmpty(_endpoint)
            && !string.IsNullOrEmpty(_accessKey)
            && !string.IsNullOrEmpty(_secretKey);
    }

    public bool IsConfigured => _isConfigured;

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
            return await _localFallback.UploadAsync(stream, fileName, folder, cancellationToken);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var key = $"{folder}/{safeName}";

        try
        {
            var contentType = GetContentType(ext);
            await PutObjectAsync(key, stream, contentType, cancellationToken);

            var publicUrl = string.IsNullOrEmpty(_publicUrl)
                ? $"{_endpoint}/{_bucketName}/{key}"
                : $"{_publicUrl}/{key}";

            _logger.LogInformation("File uploaded to CDN: {Key} -> {Url}", key, publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CDN upload failed for {Key}, falling back to local storage", key);

            if (stream.CanSeek)
                stream.Position = 0;

            return await _localFallback.UploadAsync(stream, fileName, folder, cancellationToken);
        }
    }

    public async Task DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (!_isConfigured)
        {
            await _localFallback.DeleteAsync(fileUrl, cancellationToken);
            return;
        }

        // Determine if this is a CDN URL or local URL
        var key = TryExtractKey(fileUrl);
        if (key == null)
        {
            // Not a CDN URL, delegate to local
            await _localFallback.DeleteAsync(fileUrl, cancellationToken);
            return;
        }

        try
        {
            await DeleteObjectAsync(key, cancellationToken);
            _logger.LogInformation("File deleted from CDN: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CDN delete failed for {Key}", key);
        }
    }

    /// <summary>
    /// Returns the public URL for a given object key.
    /// </summary>
    public string GetPublicUrl(string key)
    {
        if (!_isConfigured)
            return key;

        return string.IsNullOrEmpty(_publicUrl)
            ? $"{_endpoint}/{_bucketName}/{key}"
            : $"{_publicUrl}/{key}";
    }

    #region S3 REST API with AWS Signature V4

    private async Task PutObjectAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        var body = ms.ToArray();

        var url = $"{_endpoint}/{_bucketName}/{key}";
        var now = DateTime.UtcNow;
        var payloadHash = HashSha256(body);

        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        SignRequest(request, "PUT", $"/{_bucketName}/{key}", body, now, payloadHash);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task DeleteObjectAsync(string key, CancellationToken ct)
    {
        var url = $"{_endpoint}/{_bucketName}/{key}";
        var now = DateTime.UtcNow;
        var payloadHash = HashSha256(Array.Empty<byte>());

        var request = new HttpRequestMessage(HttpMethod.Delete, url);

        SignRequest(request, "DELETE", $"/{_bucketName}/{key}", Array.Empty<byte>(), now, payloadHash);

        var response = await _httpClient.SendAsync(request, ct);
        // 204 No Content is expected; 404 is acceptable (already deleted)
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Signs an HTTP request using AWS Signature Version 4.
    /// Compatible with S3, Cloudflare R2, and MinIO.
    /// </summary>
    private void SignRequest(HttpRequestMessage request, string method, string canonicalUri,
        byte[] body, DateTime now, string payloadHash)
    {
        var host = request.RequestUri!.Host;
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var amzDate = now.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);

        request.Headers.TryAddWithoutValidation("x-amz-date", amzDate);
        request.Headers.TryAddWithoutValidation("x-amz-content-sha256", payloadHash);
        request.Headers.Host = host;

        // Step 1: Canonical request
        var canonicalQueryString = "";
        var canonicalHeaders = $"host:{host}\nx-amz-content-sha256:{payloadHash}\nx-amz-date:{amzDate}\n";
        var signedHeaders = "host;x-amz-content-sha256;x-amz-date";

        var canonicalRequest = string.Join("\n",
            method,
            canonicalUri,
            canonicalQueryString,
            canonicalHeaders,
            signedHeaders,
            payloadHash);

        // Step 2: String to sign
        var service = "s3";
        var credentialScope = $"{dateStamp}/{_region}/{service}/aws4_request";
        var stringToSign = string.Join("\n",
            "AWS4-HMAC-SHA256",
            amzDate,
            credentialScope,
            HashSha256(Encoding.UTF8.GetBytes(canonicalRequest)));

        // Step 3: Signing key
        var signingKey = GetSignatureKey(_secretKey, dateStamp, _region, service);

        // Step 4: Signature
        var signature = ToHex(HmacSha256(signingKey, stringToSign));

        // Step 5: Authorization header
        var authHeader = $"AWS4-HMAC-SHA256 Credential={_accessKey}/{credentialScope}, SignedHeaders={signedHeaders}, Signature={signature}";
        request.Headers.TryAddWithoutValidation("Authorization", authHeader);
    }

    #endregion

    #region Helpers

    private string? TryExtractKey(string fileUrl)
    {
        if (!string.IsNullOrEmpty(_publicUrl) && fileUrl.StartsWith(_publicUrl))
            return fileUrl[(_publicUrl.Length + 1)..];

        var bucketPrefix = $"{_endpoint}/{_bucketName}/";
        if (fileUrl.StartsWith(bucketPrefix))
            return fileUrl[bucketPrefix.Length..];

        return null;
    }

    private static string GetContentType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".pdf" => "application/pdf",
        ".csv" => "text/csv",
        ".json" => "application/json",
        _ => "application/octet-stream"
    };

    private static string HashSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return ToHex(hash);
    }

    private static string ToHex(byte[] data)
    {
        return Convert.ToHexString(data).ToLowerInvariant();
    }

    private static byte[] HmacSha256(byte[] key, string data)
    {
        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] GetSignatureKey(string secretKey, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes($"AWS4{secretKey}"), dateStamp);
        var kRegion = HmacSha256(kDate, region);
        var kService = HmacSha256(kRegion, service);
        var kSigning = HmacSha256(kService, "aws4_request");
        return kSigning;
    }

    #endregion
}
