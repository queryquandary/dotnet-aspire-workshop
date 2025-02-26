using Aspire.Hosting.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Json;

namespace IntegrationTests
{
    [TestClass]
    public class IntegrationTests
    {
        private static AspireTestHost _host;
        private static HttpClient _client;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _host = new AspireTestHost();
            _client = _host.CreateClient();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _client.Dispose();
            _host.Dispose();
        }

        [TestMethod]
        public async Task TestApiGetZones()
        {
            var response = await _client.GetAsync("/zones");
            response.EnsureSuccessStatusCode();

            var zones = await response.Content.ReadFromJsonAsync<Zone[]>();
            Assert.IsNotNull(zones);
            Assert.IsTrue(zones.Length > 0);
        }

        [TestMethod]
        public async Task TestWebAppHomePage()
        {
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(content.Contains("MyWeatherHub"));
        }
    }

    public record Zone(string Key, string Name, string State);
}
