// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Registration.Application.Contracts;
using Registration.Domain.Model;
using System.Threading;
using System.Threading.Tasks;

namespace ComponentDialogs.Bot.Core
{
    public class ComponentDialogsBot : IBot
    {
        public const string GreetingDialogId = nameof(GreetingDialogId);
        public const string TextPromptId = nameof(TextPromptId);

        private readonly ComponentDialogsBotAccessors _accessors;
        private readonly IBotUserServices _botUserServices;
        private readonly ILogger<ComponentDialogsBot> _logger;

        public ComponentDialogsBot(
            ComponentDialogsBotAccessors accessors,
            ILogger<ComponentDialogsBot> logger,
            IBotUserServices botUserServices)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
            _botUserServices = botUserServices ?? throw new System.ArgumentNullException(nameof(botUserServices));

            Dialogs = new DialogSet(_accessors.DialogState)
                .Add(new TextPrompt(TextPromptId))
                .Add(new WaterfallDialog(GreetingDialogId)
                    .AddStep(Step1CheckRegistrationAsync)
                    .AddStep(Step2GetCallNameAsync)
                    .AddStep(Step3ThankYouAsync));

            _logger.LogTrace("----- INSTANCE CREATED - {ClassName}", GetType().Name);
        }

        public DialogSet Dialogs { get; }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.LogTrace("----- ComponentDialogBot - Receiving activity - Text: {Text} - Activity: {@Activity}", turnContext.Activity.Text, turnContext.Activity);

            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Create the dialog context
                var dialogContext = await Dialogs.CreateContextAsync(turnContext, cancellationToken);

                if (dialogContext.ActiveDialog != null)
                {
                    var dialogResult = await dialogContext.ContinueDialogAsync(cancellationToken);

                    return;
                }

                var greetingState = await _accessors.GetGreetingStateAsync(turnContext, cancellationToken);

                if (greetingState.CallName == null)
                {
                    await dialogContext.BeginDialogAsync(GreetingDialogId, null, cancellationToken);

                    return;
                }

                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                state.TurnCount++;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Echo back to the user whatever they typed.
                var responseMessage = $"Hi {greetingState.CallName} (Turn {state.TurnCount}): You typed \"{turnContext.Activity.Text}\"";

                await turnContext.SendActivityAsync(responseMessage);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

        private BotUser GetBotUser(WaterfallStepContext stepContext)
        {
            return (BotUser)stepContext.Values[nameof(BotUser)];
        }

        private async Task<DialogTurnResult> Step1CheckRegistrationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogTrace("----- ComponentDialogBot.Step1CheckRegistrationAsync - Beginning step");

            var context = stepContext.Context;

            // Check if user is already registered
            var user = await _botUserServices.FindByChannelUserIdAsync(context.Activity.ChannelId, context.Activity.From.Name);

            if (user != null)
            {
                await _accessors.SetGreetingStateAsync(stepContext.Context, state => state.CallName = user.CallName, cancellationToken);

                // Get GreetingState from conversation state
                await context.SendActivityAsync($"Hi {user.CallName}, nice to talk to you again!");

                return await stepContext.EndDialogAsync();
            }

            await context.SendActivityAsync($"Hi {context.Activity.From.Name}! You are not registered in our database.");

            var botUser = new BotUser
            {
                ChannelId = context.Activity.ChannelId,
                UserId = context.Activity.From.Name,
            };

            StoreBotUser(stepContext, botUser);

            return await stepContext.PromptAsync(
                TextPromptId,
                new PromptOptions { Prompt = MessageFactory.Text("Please enter your name") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> Step2GetCallNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogTrace("----- ComponentDialogBot.Step2GetCallNameAsync - Beginning step");

            var botUser = GetBotUser(stepContext);
            botUser.Name = (string)stepContext.Result;

            return await stepContext.PromptAsync(
                TextPromptId,
                new PromptOptions { Prompt = MessageFactory.Text($"Thanks {botUser.Name}, How do you want me to call you?") },
                cancellationToken);
        }

        private async Task<DialogTurnResult> Step3ThankYouAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogTrace("----- ComponentDialogBot.Step3ThankYouAsync - Beginning step");

            var botUser = GetBotUser(stepContext);

            botUser.CallName = (string)stepContext.Result;

            await _botUserServices.AddAsync(botUser);

            _logger.LogTrace("----- ComponentDialogBot.Step3ThankYouAsync - Checking users in database: {@Users}", await _botUserServices.GetListAsync());

            await _accessors.SetGreetingStateAsync(stepContext.Context, state => state.CallName = botUser.CallName, cancellationToken);

            await stepContext.Context.SendActivityAsync($"Thanks {botUser.CallName}, I'll echo you from now on, just type anything", cancellationToken: cancellationToken);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private void StoreBotUser(WaterfallStepContext stepContext, BotUser botUser)
        {
            stepContext.Values[nameof(BotUser)] = botUser;
        }
    }
}