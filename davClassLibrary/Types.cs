namespace davClassLibrary
{
    public enum Environment
    {
        Development,
        Test,
        Production
    }

    public enum Plan
    {
        Free,
        Plus,
        Pro
    }

    public class HttpResponse
    {
        public int Status { get; set; }
        public string Data { get; set; }

        public HttpResponse(int status, string data)
        {
            Status = status;
            Data = data;
        }
    }

    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public T Data { get; set; }
        public ApiError[] Errors { get; set; }
    }

    public class ApiError
    {
        public int Code { get; set; }
        public string Message { get; set; }
    }

    internal class HandleApiErrorResult
    {
        public bool Success { get; set; }
        public ApiError[] Errors { get; set; }
    }
}
