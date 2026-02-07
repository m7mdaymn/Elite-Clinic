namespace EliteClinic.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<object> Errors { get; set; }
    public ApiResponseMeta Meta { get; set; }

    public ApiResponse()
    {
        Errors = new List<object>();
        Meta = new ApiResponseMeta();
    }

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = new List<object>(),
            Meta = new ApiResponseMeta()
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Resource created successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Errors = new List<object>(),
            Meta = new ApiResponseMeta()
        };
    }

    public static ApiResponse<T> Error(string message, List<object>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<object>(),
            Meta = new ApiResponseMeta()
        };
    }

    public static ApiResponse<T> ValidationError(List<object> errors, string message = "Validation failed")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            Errors = errors,
            Meta = new ApiResponseMeta()
        };
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<object> Errors { get; set; }
    public ApiResponseMeta Meta { get; set; }

    public ApiResponse()
    {
        Errors = new List<object>();
        Meta = new ApiResponseMeta();
    }

    public static ApiResponse Ok(string message = "Operation completed successfully")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Errors = new List<object>(),
            Meta = new ApiResponseMeta()
        };
    }

    public static ApiResponse Error(string message, List<object>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<object>(),
            Meta = new ApiResponseMeta()
        };
    }
}

public class PaginatedResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<T> Data { get; set; }
    public PaginationMeta Pagination { get; set; }
    public ApiResponseMeta Meta { get; set; }

    public PaginatedResponse()
    {
        Data = new List<T>();
        Meta = new ApiResponseMeta();
    }

    public static PaginatedResponse<T> Ok(List<T> data, int page, int pageSize, int totalCount, string message = "Operation completed successfully")
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PaginatedResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, TotalCount = totalCount, TotalPages = totalPages },
            Meta = new ApiResponseMeta()
        };
    }
}

public class ApiResponseMeta
{
    public DateTime Timestamp { get; set; }
    public string RequestId { get; set; }

    public ApiResponseMeta()
    {
        Timestamp = DateTime.UtcNow;
        RequestId = Guid.NewGuid().ToString();
    }
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
