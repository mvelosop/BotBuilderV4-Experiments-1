using ComponentDialogs.Bot.Core;
using FluentAssertions;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Registration.Application.Contracts;
using Registration.Domain.Model;
using Registration.Infrastructure;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ComponentDialogs.Bot.ScenarioTests.Scenarios
{
    public class GreetingScenarios : IClassFixture<TestingHost>, IDisposable
    {
        private readonly ILogger<GreetingScenarios> _logger;
        private readonly IServiceScope _testingScope;

        public GreetingScenarios(TestingHost testingHost)
        {
            _testingScope = testingHost?.CreateScope() ?? throw new ArgumentNullException(nameof(testingHost));
            _logger = _testingScope.ServiceProvider.GetRequiredService<ILogger<GreetingScenarios>>();
            _logger.LogTrace("----- INSTANCE CREATED - {ClassName}", GetType().Name);
        }

        [Fact]
        public async Task GetsUserCallNameAndReplyBackUsingIt()
        {
            // Arrange -----------------
            var services = GetService<IBotUserServices>();

            var testFlow = CreateBotTestFlow<ComponentDialogsBot>()
                .Send("Hi")
                .AssertReply("Hi User1! You are not registered in our database.")
                .AssertReply("Please enter your name")
                .Send("Miguel")
                .AssertReply("Thanks Miguel, How do you want me to call you?")
                .Send("Mike")
                .AssertReply("Thanks Mike, I'll echo you from now on, just type anything")
                .Send("anything")
                .AssertReply("Hi Mike (Turn 1): You typed \"anything\"");

            // Act ---------------------
            await testFlow.StartTestAsync();

            // Assert ------------------
            var users = await services.GetListAsync();

            var expectedUsr = new BotUser
            {
                ChannelId = "test",
                UserId = "User1",
                Name = "Miguel",
                CallName = "Mike"
            };

            users.Should().BeEquivalentTo(new[] { expectedUsr });

        }

        [Fact]
        public async Task UseCallNameFromPreviousInteraction()
        {
            // Arrange -----------------
            var repo = GetService<RegistrationRepo>();

            repo.Users.Add(new BotUser
            {
                ChannelId = "test",
                UserId = "User1",
                Name = "Eduard",
                CallName = "Ed"
            });

            var testFlow = CreateBotTestFlow<ComponentDialogsBot>()
                .Send("Hi")
                .AssertReply("Hi Ed, nice to talk to you again!")
                .Send("Howdy")
                .AssertReply("Hi Ed (Turn 1): You typed \"Howdy\"");

            // Act ---------------------
            await testFlow.StartTestAsync();

            // Assert ------------------
        }

        private TestFlow CreateBotTestFlow<TBot>()
            where TBot : IBot
        {
            var conversationState = GetService<ConversationState>();
            var bot = GetService<TBot>();

            var adapter = new TestAdapter().Use(new AutoSaveStateMiddleware(conversationState));

            adapter.OnTurnError = async (context, exception) =>
            {
                _logger.LogError(exception, "----- BOT ERROR - Activity: {@Activity}", context.Activity);
                await context.SendActivityAsync($"ERROR: {exception.Message}");
            };

            return new TestFlow(adapter, bot.OnTurnAsync);
        }

        private T GetService<T>()
        {
            return _testingScope.ServiceProvider.GetRequiredService<T>();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _testingScope.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GreetingScenarios() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}