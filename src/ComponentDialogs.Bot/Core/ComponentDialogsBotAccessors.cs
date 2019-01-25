using ComponentDialogBot.Dialogs.Greeting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;

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


        public async Task<CounterState> GetCounterStateAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            return await CounterState.GetAsync(context, () => new CounterState(), cancellationToken);
        }

        public async Task<GreetingState> GetGreetingStateAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            return await GreetingState.GetAsync(context, () => new GreetingState(), cancellationToken);
        }

        public async Task<CounterState> SetCounterStateAsync(ITurnContext context, Action<CounterState> updateAction, CancellationToken cancellationToken)
        {
            var state = await GetCounterStateAsync(context, cancellationToken);

            updateAction.Invoke(state);

            await CounterState.SetAsync(context, state, cancellationToken);

            return state;
        }

        public async Task<GreetingState> SetGreetingStateAsync(ITurnContext context, Action<GreetingState> updateAction, CancellationToken cancellationToken)
        {
            var state = await GetGreetingStateAsync(context, cancellationToken);

            updateAction.Invoke(state);

            await GreetingState.SetAsync(context, state, cancellationToken);

            return state;
        }

    }
}