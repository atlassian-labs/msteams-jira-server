using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MicrosoftTeamsIntegration.Artifacts.Bots
{
    public class TeamsBotHttpAdapter : CloudAdapter
    {
        public TeamsBotHttpAdapter(
            IHostingEnvironment env,
            IConfiguration configuration,
            ILogger<TeamsBotHttpAdapter> logger,
            TelemetryInitializerMiddleware telemetryInitializerMiddleware,
            IHttpClientFactory httpClientFactory,
            ConversationState? conversationState = null,
            UserState? userState = null)
            : base(configuration, httpClientFactory, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogCritical(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                try
                {
                    // Send a catch-all apology to the user.
                    await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");

                    if (env.IsDevelopment())
                    {
                        await turnContext.SendActivityAsync(exception.ToString());

                        // Send a trace activity, which will be displayed in the Bot Framework Emulator
                        await SendTraceActivityAsync(turnContext, exception);
                    }
                }
                catch (Exception e)
                {
                    // In case if application has ME and doesn't have Bot turnContext.SendActivityAsync
                    // is throwing exception. We don't want to prevent app executing in this scenario.
                    logger.LogWarning($"Exception caught on attempting to SendActivityAsync : {e.Message}");
                }

                // Delete the conversationState and userState for the current conversation to prevent the
                // bot from getting stuck in a error-loop caused by being in a bad state.
                // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                if (conversationState != null)
                {
                    try
                    {
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }

                if (userState != null)
                {
                    try
                    {
                        await userState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Exception caught on attempting to Delete UserState : {e.Message}");
                    }
                }
            };

            Use(telemetryInitializerMiddleware);

            Use(new ShowTypingMiddleware());
            Use(new SetLocaleMiddleware("en-us"));

            if (env.IsDevelopment())
            {
                Use(new EventDebuggerMiddleware());
                Use(new ConsoleOutputMiddleware());
            }
        }

        private static async Task SendTraceActivityAsync(ITurnContext turnContext, Exception exception)
        {
            // Only send a trace activity if we're talking to the Bot Framework Emulator
            if (turnContext.Activity.ChannelId == Channels.Emulator)
            {
                var traceActivity = new Activity(ActivityTypes.Trace)
                {
                    Label = "TurnError",
                    Name = "OnTurnError Trace",
                    Value = exception.Message,
                    ValueType = "https://www.botframework.com/schemas/error",
                };

                // Send a trace activity
                await turnContext.SendActivityAsync(traceActivity);
            }
        }
    }
}
