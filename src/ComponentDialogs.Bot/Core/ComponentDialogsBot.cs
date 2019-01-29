// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ComponentDialogBot.Dialogs.Greeting;
using ComponentDialogs.Bot.Dialogs.Greeting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ComponentDialogs.Bot.Core
{
    public class ComponentDialogsBot : IBot
    {
        private readonly ComponentDialogsBotAccessors _accessors;
        private readonly ILogger<ComponentDialogsBot> _logger;

        public ComponentDialogsBot(
            ILogger<ComponentDialogsBot> logger,
            ComponentDialogsBotAccessors accessors,
            GreetingDialog greetingDialog)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));
            if (greetingDialog == null) throw new ArgumentNullException(nameof(greetingDialog));

            Dialogs = new DialogSet(_accessors.DialogState)
                .Add(greetingDialog);

            _logger.LogTrace("----- INSTANCE CREATED - {ClassName}", GetType().Name);
        }

        public DialogSet Dialogs { get; }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Create the dialog context
                var dialogContext = await Dialogs.CreateContextAsync(turnContext, cancellationToken);

                _logger.LogTrace("----- ComponentDialogsBot - ActiveDialog: {ActiveDialog} - DialogStack: {@DialogStack}", dialogContext.ActiveDialog?.Id, dialogContext.Stack);

                if (dialogContext.ActiveDialog != null)
                {
                    var dialogResult = await dialogContext.ContinueDialogAsync(cancellationToken);

                    return;
                }

                var greetingState = await _accessors.GetAsync<GreetingState>(turnContext, cancellationToken);

                if (greetingState.CallName == null)
                {
                    await dialogContext.BeginDialogAsync(GreetingDialog.GreetingDialogId, null, cancellationToken);

                    return;
                }

                // Set the conversation state from the turn context.
                var state = await _accessors.SetAsync<CounterState>(turnContext, s => s.TurnCount++, cancellationToken);
                // Echo back to the user whatever they typed.
                var responseMessage = $"Hi {greetingState.CallName} (Turn {state.TurnCount}): You typed \"{turnContext.Activity.Text}\"";

                await turnContext.SendActivityAsync(responseMessage);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }
    }
}