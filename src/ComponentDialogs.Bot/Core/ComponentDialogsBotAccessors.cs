// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using ComponentDialogBot.Dialogs.Greeting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;

namespace ComponentDialogs.Bot.Core
{
    public class ComponentDialogsBotAccessors
    {
        public ComponentDialogsBotAccessors(
            ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public static string CounterStateName { get; } = $"{nameof(ComponentDialogsBotAccessors)}.CounterState";

        public static string DialogStateName { get; } = $"{nameof(ComponentDialogsBotAccessors)}.DialogState";

        public static string GreetingStateName { get; } = $"{nameof(ComponentDialogsBotAccessors)}.GreetingState";

        public IStatePropertyAccessor<CounterState> CounterState { get; set; }

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public IStatePropertyAccessor<GreetingState> GreetingState { get; set; }

        public ConversationState ConversationState { get; }
    }
}
