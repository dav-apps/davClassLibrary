using System;

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

    public enum TableObjectUploadStatus
    {
        UpToDate = 0,
        New = 1,
        Updated = 2,
        Deleted = 3
    }

    internal enum SessionUploadStatus
    {
        UpToDate = 0,
        Deleted = 1
    }

    public enum TableObjectFileDownloadStatus
    {
        NoFileOrNotLoggedIn = 0,
        NotDownloaded = 1,
        Downloading = 2,
        Downloaded = 3
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
        public bool Success { get; set; }
        public int Status { get; set; }
        public T Data { get; set; }
        public ApiError[] Errors { get; set; }
    }

    public class ApiResponse
    {
        public bool Success { get; set; }
        public int Status { get; set; }
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

    internal class TableObjectDownload
    {
        public Guid uuid { get; set; }
        public string etag { get; set; }
    }
}
