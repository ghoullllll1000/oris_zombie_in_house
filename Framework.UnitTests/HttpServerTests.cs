using Framework;
using Framework.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Sockets;

namespace Framework.UnitTests
{

    [TestClass]
    [DoNotParallelize]
    public class HttpServerTests
    {
        private HttpServer server = null!;
        private Task serverTask = null!;
        private HttpClient client = null!;

        private string originalDirectory = null!;
        private string testDirectory = null!;
        private int port;

        [TestInitialize]
        public void Setup()
        {
            originalDirectory = Directory.GetCurrentDirectory();

            testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Directory.CreateDirectory(Path.Combine(testDirectory, "static"));
            Directory.SetCurrentDirectory(testDirectory);

            File.WriteAllText(Path.Combine("static", "404.html"), "Not found");

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var settings = new Settings
            {
                Host = "127.0.0.1",
                Port = port.ToString()
            };

            server = new HttpServer(settings, new TestSessionService());
            serverTask = server.Start();

            client = new HttpClient
            {
                BaseAddress = new Uri($"http://127.0.0.1:{port}/")
            };
        }

        [TestCleanup]
        public async Task Cleanup()
        {
            server.Stop();
            await serverTask;

            client.Dispose();

            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(testDirectory, true);
        }

        private void CreateFile(string name, string content)
        {
            File.WriteAllText(Path.Combine("static", name), content);
        }

        [TestMethod]
        public async Task UnknownRoute_Returns404()
        {
            var response = await client.GetAsync("unknown");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task HtmlFile_ReturnsCorrectContent()
        {
            CreateFile("test.html", "<h1>Hello</h1>");

            var response = await client.GetAsync("test.html");
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("<h1>Hello</h1>", content);
        }

        [TestMethod]
        public async Task CssFile_ReturnsCorrectContent()
        {
            CreateFile("style.css", "body { color: red; }");

            var response = await client.GetAsync("style.css");
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("body { color: red; }", content);
        }

        [TestMethod]
        public async Task JsFile_ReturnsCorrectContent()
        {
            CreateFile("script.js", "console.log('Hello');");

            var response = await client.GetAsync("script.js");
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("console.log('Hello');", content);
        }

        [TestMethod]
        public async Task ImageFile_ReturnsCorrectContent()
        {
            byte[] image = { 1, 2, 3, 4 };

            File.WriteAllBytes(
                Path.Combine("static", "image.png"),
                image);

            var response = await client.GetAsync("image.png");
            byte[] content = await response.Content.ReadAsByteArrayAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            CollectionAssert.AreEqual(image, content);
        }

        [TestMethod]
        public async Task HtmlFile_ReturnsCorrectMimeType()
        {
            CreateFile("test.html", "Hello");

            var response = await client.GetAsync("test.html");

            Assert.AreEqual("text/html",
                response.Content.Headers.ContentType?.MediaType);
        }

        [TestMethod]
        public async Task CssFile_ReturnsCorrectMimeType()
        {
            CreateFile("style.css", "body {}");

            var response = await client.GetAsync("style.css");

            Assert.AreEqual("text/css",
                response.Content.Headers.ContentType?.MediaType);
        }

        [TestMethod]
        public async Task JsFile_ReturnsCorrectMimeType()
        {
            CreateFile("script.js", "console.log(1);");

            var response = await client.GetAsync("script.js");

            string? mimeType =
                response.Content.Headers.ContentType?.MediaType;

            Assert.IsTrue(mimeType?.Contains("javascript") == true);
        }

        [TestMethod]
        public async Task ImageFile_ReturnsCorrectMimeType()
        {
            File.WriteAllBytes(
                Path.Combine("static", "image.png"),
                new byte[] { 1, 2, 3 });

            var response = await client.GetAsync("image.png");

            Assert.AreEqual("image/png",
                response.Content.Headers.ContentType?.MediaType);
        }

        [TestMethod]
        public async Task MissingStaticFile_Returns404()
        {
            var response = await client.GetAsync("missing.html");

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task Server_HandlesMultipleRequests()
        {
            CreateFile("test.html", "Hello");

            var firstResponse = await client.GetAsync("test.html");
            var secondResponse = await client.GetAsync("test.html");

            Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);
            Assert.AreEqual(HttpStatusCode.OK, secondResponse.StatusCode);
        }

        [TestMethod]
        public async Task Server_StopsCorrectly()
        {
            server.Stop();

            await serverTask;

            Assert.IsTrue(serverTask.IsCompleted);
        }

        [TestMethod]
        public async Task Get_ReturnsControllerResponse()
        {
            var response = await client.GetAsync("test/hello");
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Hello!", content);
        }

        [TestMethod]
        public async Task Get_PassesQueryParameter()
        {
            var response = await client.GetAsync("test/query?name=Alice");
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Alice", content);
        }

        [TestMethod]
        public async Task Post_PassesFormParameter()
        {
            var data = new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["name"] = "Alice"
                });

            var response = await client.PostAsync("test/form", data);
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Alice", content);
        }

        [TestMethod]
        public async Task Get_ProtectedPageWithoutAuthorization_Returns401()
        {
            var response = await client.GetAsync("test/secret");

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task Get_ProtectedPageWithAuthorization_Returns200()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get, "test/secret");

            request.Headers.Add("Cookie", "sessionId=valid");

            var response = await client.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("Secret!", content);
        }

        private class TestSessionService : IUserSessionService
        {
            public string CreateSession(int userId) => "valid";

            public int? GetUserId(string? sessionId)
            {
                if (sessionId == "valid")
                    return 1;

                return null;
            }

            public void RemoveSession(string? sessionId) { }
        }
    }
}