using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.Middleware
{
    public class EventDebuggerMiddleware : IMiddleware
    {
        private static readonly string[] EventSeparator = new string[] { "/event:" };

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = turnContext.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                var text = activity.Text;
                var value = activity.Value?.ToString();

                if (!string.IsNullOrEmpty(text) && text.StartsWith("/event:"))
                {
                    var json = text.Split(EventSeparator, StringSplitOptions.None)[1];
                    var body = JsonConvert.DeserializeObject<Activity>(json);

                    turnContext.Activity.Type = ActivityTypes.Event;

                    turnContext.Activity.Name = body?.Name ?? turnContext.Activity.Name;
                    turnContext.Activity.Text = body?.Text ?? turnContext.Activity.Text;
                    turnContext.Activity.Value = body?.Value ?? turnContext.Activity.Value;
                }

                if (!string.IsNullOrEmpty(value) && value.Contains("event"))
                {
                    var root = JObject.Parse(value);
                    if (root is JObject eventActivity)
                    {
                        turnContext.Activity.Type = ActivityTypes.Event;

                        if (eventActivity["name"] != null)
                        {
                            turnContext.Activity.Name = eventActivity["name"]?.ToString();
                        }

                        if (eventActivity["text"] != null)
                        {
                            turnContext.Activity.Text = eventActivity["text"]?.ToString();
                        }

                        if (eventActivity["value"] != null)
                        {
                            turnContext.Activity.Value = eventActivity["value"]?.ToString();
                        }
                    }
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }
    }
}
