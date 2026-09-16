using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.DataAccess
{
    [TestFixture, NonParallelizable]
    public class UploadFailureTest
    {
        [Test]
        public async Task MissingFileReturnsFailure()
        {
            Assert.IsFalse((await TableObjectsController.UploadTableObjectFile(Guid.NewGuid(), "image/png", null)).Success);
            Assert.IsFalse((await TableObjectsController.UploadTableObjectFile(Guid.NewGuid(), "image/png",
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))).Success);
        }

        [Test]
        public async Task UpdateUsesFilePathAndMimeTypeAndHandlesTransportFailure()
        {
            string previousToken = Dav.AccessToken;
            bool loggedIn = Dav.IsLoggedIn;
            string token = Guid.NewGuid().ToString();
            string path = Path.GetTempFileName();
            var handler = new FailingUploadHandler();
            var clients = (Dictionary<string, HttpClient>)typeof(ApiManager)
                .GetField("httpClients", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            using (var client = new HttpClient(handler))
            try
            {
                File.WriteAllText(path, "image-content");
                Dav.IsLoggedIn = true;
                Dav.AccessToken = token;
                clients.Add(token, client);
                var file = new TableObject(Guid.NewGuid(), 1) { IsFile = true, File = new FileInfo(path) };
                file.Properties.Add(new Property("ext", "png"));
                var method = typeof(SyncManager).GetMethod("UpdateTableObjectOnServer", BindingFlags.NonPublic | BindingFlags.Static);
                var result = await (Task<GraphQLApiResponse<TableObject>>)method.Invoke(null, new object[] { file });
                Assert.IsTrue(handler.Called, "upload reached HTTP with the actual file");
                Assert.AreEqual("image/png", handler.ContentType);
                Assert.AreEqual("image-content", handler.Body);
                Assert.IsFalse(result.Success, "transport error keeps upload pending without a null Error crash");
            }
            finally
            {
                clients.Remove(token);
                Dav.AccessToken = previousToken;
                Dav.IsLoggedIn = loggedIn;
                File.Delete(path);
            }
        }

        private sealed class FailingUploadHandler : HttpMessageHandler
        {
            public bool Called;
            public string ContentType;
            public string Body;
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                Called = true;
                ContentType = request.Content.Headers.ContentType.MediaType;
                Body = await request.Content.ReadAsStringAsync();
                throw new HttpRequestException("simulated offline transport");
            }
        }
    }
}
