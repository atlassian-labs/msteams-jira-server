﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.Middleware
{
    public class ConsoleOutputMiddleware : IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            LogActivity(string.Empty, turnContext.Activity);
            turnContext.OnSendActivities(OnSendActivitiesAsync);

            await next(cancellationToken).ConfigureAwait(false);
        }

        private static string GetTextOrSpeak(IMessageActivity messageActivity)
        {
            return string.IsNullOrWhiteSpace(messageActivity.Text) ? messageActivity.Speak : messageActivity.Text;
        }

        private async Task<ResourceResponse[]> OnSendActivitiesAsync(ITurnContext context, List<Activity> activities, Func<Task<ResourceResponse[]>> next)
        {
            foreach (var response in activities)
            {
                LogActivity(string.Empty, response);
            }

            return await next().ConfigureAwait(false);
        }

        private static void LogActivity(string prefix, Activity contextActivity)
        {
            Console.WriteLine(string.Empty);
            if (contextActivity.Type == ActivityTypes.Message)
            {
                var messageActivity = contextActivity.AsMessageActivity();
                Console.WriteLine($"{prefix} [{DateTime.Now:ss.fff}] {GetTextOrSpeak(messageActivity)}");
            }
            else if (contextActivity.Type == ActivityTypes.Event)
            {
                var eventActivity = contextActivity.AsEventActivity();
                Console.WriteLine($"{prefix} Event: [{DateTime.Now:ss.fff}{DateTime.Now.Millisecond}] {eventActivity.Name}");
            }
            else
            {
                Console.WriteLine($"{prefix} {contextActivity.Type}: [{DateTime.Now:ss.fff}{DateTime.Now.Millisecond}]");
            }
        }
    }
}
