using ComponentDialogBot.Dialogs.Greeting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;

namespace ComponentDialogs.Bot.Core
{
    public class ComponentDialogsBotAccessors
    {
        private static readonly string CounterStateName = $"{nameof(ComponentDialogsBotAccessors)}.CounterState";
        private static readonly string DialogStateName = $"{nameof(ComponentDialogsBotAccessors)}.DialogState";
        private static readonly string GreetingStateName = $"{nameof(ComponentDialogsBotAccessors)}.GreetingState";

        public ComponentDialogsBotAccessors(
            ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            CounterState = conversationState.CreateProperty<CounterState>(CounterStateName);
            DialogState = conversationState.CreateProperty<DialogState>(DialogStateName);
            GreetingState = conversationState.CreateProperty<GreetingState>(GreetingStateName);
        }

        public ConversationState ConversationState { get; }

        public IStatePropertyAccessor<CounterState> CounterState { get; }

        public IStatePropertyAccessor<DialogState> DialogState { get; }

        public IStatePropertyAccessor<GreetingState> GreetingState { get; }
    }
}