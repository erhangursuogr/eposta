using DeuEposta.Models;

namespace DeuEposta.Utils;

public static class ResponseHelper
{
    public static object Success(string message, object? data = null)
    {
        return new
        {
            success = true,
            message,
            data
        };
    }

    public static object Error(string message, int statusCode = 400, string? errorDetail = null)
    {
        return new
        {
            success = false,
            message,
            statusCode,
            errorDetail
        };
    }
}
