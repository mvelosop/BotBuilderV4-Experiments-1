using ComponentDialogBot.Dialogs.Greeting;
using ComponentDialogs.Bot.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Registration.Application.Contracts;
using Registration.Application.Services;
using Registration.Infrastructure;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ComponentDialogs.Bot.ScenarioTests
{
    public class TestingHost : IDisposable
    {
        private static readonly string ApplicationName = typeof(TestingHost).Namespace;

        public TestingHost()
        {
            // Configuration setup
            Configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Logging setup
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithProperty("Application", ApplicationName)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File(
                    $@"D:\home\LogFiles\{ApplicationName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 15,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                .CreateLogger();

            // Dependency injection setup
            var services = new ServiceCollection();

            // General infrastructure services configuration
            services.AddSingleton<IConfiguration>(sp => Configuration);
            services.AddSingleton(new LoggerFactory().AddSerilog());
            services.AddLogging();

            // Bot infrastructure services configuration
            services.AddScoped<IStorage, MemoryStorage>();
            services.AddScoped<ConversationState>();
            services.AddScoped<ComponentDialogsBotAccessors>(sp =>
            {
                var conversationState = sp.GetRequiredService<ConversationState>();

                // Create the custom state accessor.
                // State accessors enable other components to read and write individual properties of state.
                return new ComponentDialogsBotAccessors(conversationState)
                {
                    CounterState = conversationState.CreateProperty<CounterState>(ComponentDialogsBotAccessors.CounterStateName),
                    DialogState = conversationState.CreateProperty<DialogState>(ComponentDialogsBotAccessors.DialogStateName),
                    GreetingState = conversationState.CreateProperty<GreetingState>(ComponentDialogsBotAccessors.GreetingStateName),
                };
            });

            // Bot service configuration
            services.AddScoped<ComponentDialogsBot>();

            // Application services configuration
            services.AddScoped<RegistrationRepo>();
            services.AddScoped<IBotUserServices, BotUserServices>();

            RootScope = services.BuildServiceProvider().CreateScope();

            Log.Verbose("----- INSTANCE CREATED - {ClassName}", GetType().Name);
        }

        public IConfigurationRoot Configuration { get; }

        public IServiceScope RootScope { get; }

        public IServiceScope CreateScope()
        {
            return RootScope.ServiceProvider.CreateScope();
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Log.CloseAndFlush();

                    RootScope.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TestHostFixture() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        #endregion IDisposable Support
    }
}
