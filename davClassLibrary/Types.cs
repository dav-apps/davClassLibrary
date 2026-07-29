using System;
using System.Collections.Generic;

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

    public enum SessionUploadStatus
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

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public int Status { get; set; }
        public T Data { get; set; }
        public ApiResponseError Error { get; set; }
    }

    public class ApiResponseError
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class GraphQLApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }
    }

    public class GraphQLApiResponse
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
    }

    public class ApiErrorRaw
    {
        public string code { get; set; }
        public string message { get; set; }
    }

    internal class HandleApiErrorResult
    {
        public bool Success { get; set; }
        public List<string> Errors { get; set; }
    }

    internal class TableObjectDownload
    {
        public Guid uuid { get; set; }
        public string etag { get; set; }
    }
}
