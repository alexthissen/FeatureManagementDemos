using LeaderboardWebApi.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeaderboardWebApi.IntegrationTests
{
    [TestClass]
    public class ServiceContractIntegrationTest
    {
        //[TestInitialize]
        public async Task Initialize()
        {
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices((context, services) =>
                    {
                        var root = new InMemoryDatabaseRoot();
                        services.AddScoped(provider =>
                        {
                            // Replace SQL Server with in-memory provider
                            return new DbContextOptionsBuilder<LeaderboardContext>()
                                .UseInMemoryDatabase("HighScores", root)
                                .UseApplicationServiceProvider(provider)
                                .Options;
                        });
                    });
                });

            // Create direct in-memory HTTP client
            var httpClient = factory.CreateClient(new WebApplicationFactoryClientOptions() { });
        }

        [TestMethod]
        public async Task GetReturns200OK()
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices((context, services) =>
                    {
                        var root = new InMemoryDatabaseRoot();
                        services.AddScoped(provider =>
                        {
                            // Replace SQL Server with in-memory provider
                            return new DbContextOptionsBuilder<LeaderboardContext>()
                                .UseInMemoryDatabase("HighScores", root)
                                .UseApplicationServiceProvider(provider)
                                .Options;
                        });
                    });
                });

            // Create direct in-memory HTTP client
            HttpClient httpClient = factory.CreateClient(); // new WebApplicationFactoryClientOptions() { BaseAddress = new System.Uri("https://localhost") });

            // Act
            var response = await httpClient.GetAsync("Leaderboard");

            // Assert 
            response.EnsureSuccessStatusCode();
            string responseHtml = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(responseHtml.Contains("1337"));
        }

        [TestMethod]
        public async Task TestMethod1()
        {
            await using var application = new WebApplicationFactory<Program>();

            // Create direct in-memory HTTP client
            HttpClient client = application.CreateClient(new WebApplicationFactoryClientOptions() { });
            var response = await client.GetAsync("/weatherforecast");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.IsNotNull(response);
        }
    }
}
