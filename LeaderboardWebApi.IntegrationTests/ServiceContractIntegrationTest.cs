using LeaderboardWebApi.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeaderboardWebApi.IntegrationTests
{
    [TestClass]
    public class ServiceContractIntegrationTest
    {
        ////[TestInitialize]
        //public async Task Initialize()
        //{
        //    var factory = new WebApplicationFactory<Program>()
        //        .WithWebHostBuilder(builder =>
        //        {
        //            builder.ConfigureServices((context, services) =>
        //            {
        //                var root = new InMemoryDatabaseRoot();
        //                services.AddScoped(provider =>
        //                {
        //                    // Replace SQL Server with in-memory provider
        //                    return new DbContextOptionsBuilder<LeaderboardContext>()
        //                        .UseInMemoryDatabase("HighScores", root)
        //                        .UseApplicationServiceProvider(provider)
        //                        .Options;
        //                });
        //            });
        //        });

        //    // Create direct in-memory HTTP client
        //    var httpClient = factory.CreateClient(new WebApplicationFactoryClientOptions() { });
        //}

        [DataTestMethod]
        [DataRow("Ring0", "api/v1.0/Leaderboard?limit=1", 1)]
        [DataRow("Ring1", "api/v1.0/Leaderboard", 2)]
        public async Task GetReturns200OK(string environment, string url, int count)
        {
            await using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseContentRoot(Directory.GetCurrentDirectory());
                    builder.UseEnvironment(environment);
                    builder.ConfigureTestServices(services =>
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
                        var provider = services.BuildServiceProvider();
                        using (var scope = provider.CreateScope())
                        {
                            scope.ServiceProvider.GetRequiredService<LeaderboardContext>().Database.EnsureCreated();
                        }
                    });
                });

            // Create direct in-memory HTTP client
            HttpClient httpClient = factory.CreateClient(); // new WebApplicationFactoryClientOptions() { BaseAddress = new System.Uri("https://localhost") });

            // Act
            var response = await httpClient.GetAsync(url);

            // Assert 
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<IEnumerable<dynamic>>(body);
            Assert.AreEqual(count, result.Count());
        }
    }
}
