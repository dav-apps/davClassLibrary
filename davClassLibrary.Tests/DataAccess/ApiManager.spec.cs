using davClassLibrary.DataAccess;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.DataAccess
{
    [TestFixture, NonParallelizable]
    public class ApiManagerTest
    {
        [Test]
        public async Task PublicDownloadsOmitTokenWithoutChangingAuthenticatedDownloads()
        {
            string previousToken = Dav.AccessToken;
            string path = Path.Combine(Path.GetTempPath(), "dav-download-" + Guid.NewGuid());
            try
            {
                Dav.AccessToken = "test-session-one";
                Assert.IsNull(await DownloadAndReadAuthorization(path, true));
                Assert.AreEqual("Bearer test-session-one", await DownloadAndReadAuthorization(path, false));
                Dav.AccessToken = "test-session-two";
                Assert.IsNull(await DownloadAndReadAuthorization(path, true));
                Assert.AreEqual("Bearer test-session-two", await DownloadAndReadAuthorization(path, false));
            }
            finally
            {
                Dav.AccessToken = previousToken;
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static async Task<string> DownloadAndReadAuthorization(string path, bool isPublic)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                string url = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/image";
                var request = ReadRequest(listener);
                var download = isPublic ? ApiManager.DownloadPublicFile(url, path) : ApiManager.DownloadFile(url, path);
                var completed = Task.WhenAll(request, download);
                Assert.AreSame(completed, await Task.WhenAny(completed, Task.Delay(10000)), "Download timed out");
                await completed;
                Assert.IsTrue(await download);
                Assert.AreEqual("image", File.ReadAllText(path));
                return await request;
            }
            finally { listener.Stop(); }
        }

        private static async Task<string> ReadRequest(TcpListener listener)
        {
            using (var client = await listener.AcceptTcpClientAsync())
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true))
            {
                string authorization = null;
                string line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                    if (line.StartsWith("Authorization:", StringComparison.OrdinalIgnoreCase))
                        authorization = line.Substring("Authorization:".Length).Trim();
                byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 5\r\nConnection: close\r\n\r\nimage");
                await stream.WriteAsync(response, 0, response.Length);
                return authorization;
            }
        }
    }
}
