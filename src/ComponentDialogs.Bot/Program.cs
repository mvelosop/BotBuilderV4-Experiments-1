// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace ComponentDialogs.Bot
{
    public class Program
    {
        private static readonly string ApplicationName = typeof(Program).Namespace;

        public static int Main(string[] args)
        {
            var configuration = GetConfiguration();

            ConfigureLogging(configuration);

            try
            {
                Log.Information("----- Starting web host");
                BuildWebHost(args, configuration)
                    .Run();

                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "----- Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IWebHost BuildWebHost(string[] args, IConfiguration configuration) => WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseConfiguration(configuration)
            .UseSerilog()
            .Build();

        private static void ConfigureLogging(IConfiguration configuration)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.WithProperty("Application", ApplicationName)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    $@"D:\home\LogFiles\{ApplicationName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 15,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1));

            if (IsDevelopment())
            {
                loggerConfiguration = loggerConfiguration
                    .MinimumLevel.Verbose()
                    .WriteTo.Seq("http://localhost:5341");
            }
            else
            {
                loggerConfiguration = loggerConfiguration
                    .MinimumLevel.Debug()
                    .WriteTo.ApplicationInsightsTraces(configuration["APPINSIGHTS_INSTRUMENTATIONKEY"], LogEventLevel.Debug);
            }

            Log.Logger = loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        private static IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{GetEnvironmentName()}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        private static string GetEnvironmentName() => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        private static bool IsDevelopment() => GetEnvironmentName() != "Production";
    }

    public class OriginalProgram
    {
        public static void OriginalMain(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // Add Azure Logging
                    logging.AddAzureWebAppDiagnostics();

                    // Logging Options.
                    // There are other logging options available:
                    // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1
                    // logging.AddDebug();
                    // logging.AddConsole();
                })

                // Logging Options.
                // Consider using Application Insights for your logging and metrics needs.
                // https://azure.microsoft.com/en-us/services/application-insights/
                // .UseApplicationInsights()
                .UseStartup<Startup>()
                .UseSerilog()
                .Build();
    }
}
