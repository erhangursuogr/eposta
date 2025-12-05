namespace DeuEposta.Models;

public class ResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ErrorDetail { get; set; }

    public static ResponseModel SuccessResult(string message = "İşlem başarılı")
    {
        return new ResponseModel
        {
            Success = true,
            Message = message,
            StatusCode = 200
        };
    }

    public static ResponseModel ErrorResult(string message, int statusCode = 400, string? errorDetail = null)
    {
        return new ResponseModel
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            ErrorDetail = errorDetail
        };
    }
}

public class ResponseDataModel<T> : ResponseModel
{
    public T? Data { get; set; }

    // Pagination properties
    public int TotalCount { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; }

    public static ResponseDataModel<T> SuccessResult(T data, string message = "İşlem başarılı")
    {
        return new ResponseDataModel<T>
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            Data = data
        };
    }

    public static ResponseDataModel<T> SuccessResultWithPagination(T data, int totalCount, int page, int pageSize, string message = "İşlem başarılı")
    {
        return new ResponseDataModel<T>
        {
            Success = true,
            Message = message,
            StatusCode = 200,
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    public new static ResponseDataModel<T> ErrorResult(string message, int statusCode = 400, string? errorDetail = null)
    {
        return new ResponseDataModel<T>
        {
            Success = false,
            Message = message,
            StatusCode = statusCode,
            ErrorDetail = errorDetail,
            Data = default(T)
        };
    }
}