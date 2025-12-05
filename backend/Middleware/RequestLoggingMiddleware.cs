using DeuEposta.Utils;
using Serilog.Context;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

namespace DeuEposta.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip logging for health checks and static files
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Contains("/health") || path.Contains("/hangfire") || path.Contains("/_framework"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N").Substring(0, 8);

        // Get user info
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";
        var userName = context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Anonymous";
        var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "None";

        // Get IP Address
        var ipAddress = HttpContextHelper.GetClientIPAddress(context);

        // Read request body
        string requestBody = await ReadRequestBodyAsync(context);
        var queryString = context.Request.QueryString.Value;

        // Capture response body
        var originalResponseBody = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Add context properties for all logs in this request
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("UserName", userName))
        using (LogContext.PushProperty("UserRole", userRole))
        using (LogContext.PushProperty("IpAddress", ipAddress))
        {
            try
            {
                await _next(context);

                stopwatch.Stop();

                // Read response body
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseBodyText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                var statusCode = context.Response.StatusCode;
                var logLevel = statusCode >= 500 ? LogLevel.Error :
                              statusCode >= 400 ? LogLevel.Warning :
                              LogLevel.Information;

                var emoji = statusCode >= 500 ? "❌" :
                           statusCode >= 400 ? "⚠️" :
                           statusCode >= 300 ? "↩️" :
                           "✅";

                // Sanitize sensitive data
                if (path.Contains("/login", StringComparison.OrdinalIgnoreCase))
                {
                    requestBody = SanitizePassword(requestBody);
                }

                // Sanitize base64 image data
                requestBody = SanitizeBase64Images(requestBody);

                // Sanitize response body (icerik alanını filtrele)
                responseBodyText = SanitizeIcerik(responseBodyText);

                // Log with detailed information
                _logger.Log(logLevel,
                    "{Emoji} {Method} {Path} → {StatusCode} | {ElapsedMs}ms | User: {UserName} ({Role}) | IP: {IpAddress} | Request: {RequestBody} | Query: {QueryString} | Response: {ResponseBody}",
                    emoji,
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    userName,
                    userRole,
                    ipAddress,
                    string.IsNullOrEmpty(requestBody) ? "-" : requestBody,
                    string.IsNullOrEmpty(queryString) ? "-" : queryString,
                    string.IsNullOrEmpty(responseBodyText) ? "-" : TruncateIfNeeded(responseBodyText, 1000));

                // Copy response to original stream
                await responseBody.CopyToAsync(originalResponseBody);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "💥 {Method} {Path} FAILED | {ElapsedMs}ms | User: {UserName} | IP: {IpAddress} | Request: {RequestBody}",
                    context.Request.Method,
                    context.Request.Path,
                    stopwatch.ElapsedMilliseconds,
                    userName,
                    ipAddress,
                    string.IsNullOrEmpty(requestBody) ? "-" : requestBody);
                throw;
            }
            finally
            {
                context.Response.Body = originalResponseBody;
            }
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        // Skip file uploads
        if (context.Request.Path.Value?.Contains("/upload", StringComparison.OrdinalIgnoreCase) == true ||
            context.Request.ContentType?.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true)
        {
            return "[FILE UPLOAD]";
        }

        context.Request.EnableBuffering();

        try
        {
            // Limit to 1MB to prevent memory issues
            const int maxBufferSize = 1048576;
            var contentLength = Math.Min(Convert.ToInt32(context.Request.ContentLength ?? 0), maxBufferSize);

            if (contentLength == 0)
                return "-";

            var buffer = new byte[contentLength];
            var bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length));
            var bodyAsText = bytesRead > 0 ? Encoding.UTF8.GetString(buffer, 0, bytesRead) : string.Empty;
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            // Warn if truncated
            if (context.Request.ContentLength > maxBufferSize)
            {
                return bodyAsText + "... [TRUNCATED - Request too large]";
            }

            return string.IsNullOrEmpty(bodyAsText) ? "-" : bodyAsText;
        }
        catch
        {
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            return "-";
        }
    }

    private string SanitizePassword(string requestBody)
    {
        try
        {
            // Simple password masking for JSON
            if (requestBody.Contains("\"password\"", StringComparison.OrdinalIgnoreCase) ||
                requestBody.Contains("\"sifre\"", StringComparison.OrdinalIgnoreCase))
            {
                return "[PASSWORD MASKED]";
            }
            return requestBody;
        }
        catch
        {
            return requestBody;
        }
    }

    private string TruncateIfNeeded(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "... [truncated]";
    }

    private string SanitizeBase64Images(string requestBody)
    {
        try
        {
            // Check if contains base64 image data
            if (requestBody.Contains("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                // Use regex to replace base64 image data with placeholder
                var pattern = @"data:image/[^;]+;base64,[A-Za-z0-9+/=]+";
                var replacement = "[BASE64_IMAGE_DATA_REMOVED]";
                return System.Text.RegularExpressions.Regex.Replace(requestBody, pattern, replacement);
            }
            return requestBody;
        }
        catch
        {
            return requestBody;
        }
    }

    private string SanitizeIcerik(string responseBody)
    {
        try
        {
            // JSON içinde "icerik" veya "içerik" alanlarını filtrele
            if (!responseBody.Contains("\"icerik\"", StringComparison.OrdinalIgnoreCase) &&
                !responseBody.Contains("\"içerik\"", StringComparison.OrdinalIgnoreCase))
            {
                return responseBody;
            }

            using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            // data.icerik alanını filtrele
            if (root.TryGetProperty("data", out var dataElement))
            {
                var filteredData = FilterIcerikInObject(dataElement);

                var result = new Dictionary<string, object?>();
                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name == "data")
                    {
                        result[prop.Name] = filteredData;
                    }
                    else
                    {
                        result[prop.Name] = GetJsonValue(prop.Value);
                    }
                }

                return System.Text.Json.JsonSerializer.Serialize(result);
            }

            return responseBody;
        }
        catch
        {
            // JSON parse hatası - olduğu gibi döndür
            return responseBody;
        }
    }

    private object? FilterIcerikInObject(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.Equals("icerik", StringComparison.OrdinalIgnoreCase) ||
                    prop.Name.Equals("içerik", StringComparison.OrdinalIgnoreCase))
                {
                    var content = prop.Value.GetString() ?? "";
                    var preview = content.Length > 50 ? content.Substring(0, 50) + "..." : content;
                    dict[prop.Name] = $"[FILTERED - {content.Length} chars] {preview}";
                }
                else
                {
                    dict[prop.Name] = GetJsonValue(prop.Value);
                }
            }
            return dict;
        }
        return GetJsonValue(element);
    }

    private object? GetJsonValue(System.Text.Json.JsonElement element)
    {
        return element.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => element.GetString(),
            System.Text.Json.JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDecimal(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.Null => null,
            System.Text.Json.JsonValueKind.Object => FilterIcerikInObject(element),
            System.Text.Json.JsonValueKind.Array => element.EnumerateArray().Select(GetJsonValue).ToList(),
            _ => element.ToString()
        };
    }
}