using Microsoft.Teams.AI;
using Microsoft.Teams.AI.Annotations;
using Microsoft.Teams.AI.Messages;
using Microsoft.Teams.AI.Models.OpenAI;
using Microsoft.Teams.AI.Prompts;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Api.Activities.Invokes;
using Microsoft.Teams.Api.TaskModules;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Apps.Activities.Invokes;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Cards;
using Microsoft.Teams.Common;

namespace My.Doom.Agent;

[TeamsController("main")]
public class MainController(Func<OpenAIChatPrompt> prompt)
{
    private static readonly string devTunnelUrl = "https://0dsfznrj-3978.euw.devtunnels.ms"; //Set the URL here to your dev tunnel URL
    public static CancellationTokenSource? StreamCancellation = null;
    public static bool playDoom = false;

    [Message]
    public async System.Threading.Tasks.Task OnMessage(IContext<Microsoft.Teams.Api.Activities.MessageActivity> context)
    {
        // Create a cancellation token source for the streaming
        StreamCancellation = new CancellationTokenSource();

        // Create a linked token source that cancels if either the context is cancelled or streaming is cancelled
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            context.CancellationToken,
            StreamCancellation.Token);

        try
        {
            var response = await prompt().Send(context.Activity.Text, null,
             (chunk) => System.Threading.Tasks.Task.Run(() => context.Stream.Emit(chunk)),
             linkedCts.Token);

            // Check if playDoom was set during streaming (in case cancellation didn't throw)
            if (playDoom)
            {
                playDoom = false;
                var card = CreateDialogLauncherCard();
                await context.Send(card);
                return;
            }
        }
        catch (OperationCanceledException) when (StreamCancellation.IsCancellationRequested)
        {
            // Streaming was cancelled due to StartDoom being called
            playDoom = false;
            var card = CreateDialogLauncherCard();
            await context.Send(card);
        }
        finally
        {
            StreamCancellation = null;
        }
    }

    [TaskFetch]
    public Microsoft.Teams.Api.TaskModules.Response OnTaskFetch([Context] Tasks.FetchActivity activity, [Context] IContext.Client client, [Context] Microsoft.Teams.Common.Logging.ILogger log)
    {
        return CreateWebpageDialog();
    }

    private static Microsoft.Teams.Cards.AdaptiveCard CreateDialogLauncherCard()
    {
        var card = new Microsoft.Teams.Cards.AdaptiveCard
        {
            Body = new List<CardElement>
    {   new Microsoft.Teams.Cards.Image(devTunnelUrl + "/tabs/dialog-form/logo.png"){
        HorizontalAlignment = HorizontalAlignment.Left
        },
        new TextBlock("Click the button to play Doom!")
        {
            Size = TextSize.Large,
            Weight = TextWeight.Bolder,
            HorizontalAlignment = HorizontalAlignment.Left
        }
    },
            Actions = new List<Microsoft.Teams.Cards.Action>
    {

        new TaskFetchAction()
        {
            Title = "Play Doom!",
            IconUrl = "https://user-images.githubusercontent.com/590297/84582475-795ece00-adba-11ea-9aa1-a62c308746ec.png"
        }

    }
        };

        return card;
    }

    private static Microsoft.Teams.Api.TaskModules.Response CreateWebpageDialog()
    {
        var taskInfo = new TaskInfo
        {
            Title = "Doom!",
            Width = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(1000),
            Height = new Union<int, Microsoft.Teams.Api.TaskModules.Size>(800),
            // Here we are using a webpage that is hosted in the same
            // server as the agent. This server needs to be publicly accessible,
            // needs to set up teams.js client library (https://www.npmjs.com/package/@microsoft/teams-js)
            // and needs to be registered in the manifest.
            Url = $"{devTunnelUrl}/tabs/dialog-form"
        };

        return new Microsoft.Teams.Api.TaskModules.Response(new ContinueTask(taskInfo));
    }
}

[Prompt]
[Prompt.Description("Allows the player to start Doom by clicking a button.")]
[Prompt.Instructions(
    "You are a helpful assistant.",
    "You starts the Game doom if the user requests it."
)]
public class DoomPrompt(IContext.Accessor accessor)
{
    private IContext<IActivity> context => accessor.Value!;

    [Function]
    [Microsoft.Teams.AI.Annotations.Function.Description("Allows the user to start the game Doom if the user requests it.")]
    public async Task<string> StartDoom()
    {
        MainController.playDoom = true;

        // Cancel the streaming if it's currently active
        MainController.StreamCancellation?.Cancel();

        return $"Doom started.";
    }
}
