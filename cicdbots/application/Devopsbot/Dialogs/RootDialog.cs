using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Devopsbot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
       

            if (activity.Text.Contains("VSTS"))
            {
                await context.PostAsync($"You mentioned VSTS above, but I think you meant Azure DevOps. ");


            }

            context.Wait(MessageReceivedAsync);
        }
    }
}