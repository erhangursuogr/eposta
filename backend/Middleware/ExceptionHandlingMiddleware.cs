using DeuEposta.Models;
using System.Net;
using System.Text.Json;

namespace DeuEposta.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the full exception details
        _logger.LogError(exception, "Unhandled exception occurred. Request: {Method} {Path} {QueryString} IP: {RemoteIP}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            context.Connection.RemoteIpAddress?.ToString());

        context.Response.ContentType = "application/json";

        var response = new ResponseDataModel<object?>();

        switch (exception)
        {
            case ArgumentException:
            case FormatException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Geçersiz parametre";
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Yetkisiz erişim";
                break;

            case FileNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = "Kaynak bulunamadı";
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                response.Message = "İşlem zaman aşımına uğradı";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // NEVER expose internal exception details in production
                response.Message = _environment.IsDevelopment()
                    ? $"Internal server error: {exception.Message}"
                    : "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.";
                break;
        }

        response.Success = false;

        // Development'ta detaylı hata bilgisi ekle
        if (_environment.IsDevelopment() && exception != null)
        {
            response.Data = new
            {
                exceptionType = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message
            };
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }
}