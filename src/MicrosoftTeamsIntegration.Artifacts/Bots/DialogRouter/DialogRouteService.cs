using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Bots.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Extensions;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter
{
    [PublicAPI]
    public sealed class DialogRouteService : IDialogRouteService
    {
        private readonly Dictionary<string, DialogRoute> _routes;

        public DialogRouteService(IServiceProvider serviceProvider, params DialogRoute[] dialogRoutes)
        {
            foreach (var dialogRoute in dialogRoutes)
            {
                var dialog = serviceProvider.GetService(dialogRoute.DialogType) as Dialog;
                if (dialog is null)
                {
                    throw new NullReferenceException($"{nameof(dialog)} cannot be null");
                }

                dialogRoute.Dialog = dialog;
            }

            var hostingEnvironment = serviceProvider.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment;
            var routes = new List<DialogRoute>
            {
                new DialogRoute(typeof(AmbiguousActionDialog), string.Empty, isAuthenticationRequired: false)
                {
                    Dialog = new AmbiguousActionDialog(hostingEnvironment?.IsDevelopment() ?? false)
                },
                new DialogRoute(typeof(CancelDialog), new[] { "cancel", "back", "undo", "reset" }, isAuthenticationRequired: false)
                {
                    Dialog = new CancelDialog()
                }
            };

            routes.AddRange(dialogRoutes);

            _routes = new Dictionary<string, DialogRoute>(routes.Count);
            var ordered = routes.OrderBy(r => r.Order).ToList();
            foreach (var dialogRoute in ordered)
            {
                _routes.Add(dialogRoute.Dialog!.Id, dialogRoute);
            }
        }

        public Dialog[] GetRegisteredDialogs()
        {
            return _routes.Values.Select(x => x.Dialog!).ToArray();
        }

        public DialogRoute? FindBestMatch(string messageText)
        {
            var bestMatchedScore = 0d;
            DialogRoute? bestMatchedRoute = null;
            DialogRoute? bestMatchedRegexRoute = null;

            foreach (var route in _routes.Values)
            {
                if (route.RegexCommand != null)
                {
                    var regexMatches = CheckIfRegexCommandMatches(messageText, route);
                    if (regexMatches)
                    {
                        // we should return matched regex route if it is already
                        // selected and current route has higher order than it
                        if (bestMatchedRegexRoute != null && route.Order > bestMatchedRegexRoute.Order)
                        {
                            return bestMatchedRegexRoute;
                        }

                        // we should return default route when matched regex route
                        // is already selected and current route has the same priority,
                        // as we don't allow regex routes to overlap each other
                        if (bestMatchedRegexRoute != null && route.Order == bestMatchedRegexRoute.Order)
                        {
                            if (_routes.TryGetValue(nameof(AmbiguousActionDialog), out var dialogRoute))
                            {
                                if (dialogRoute?.Dialog is AmbiguousActionDialog ambiguousActionDialog)
                                {
                                    ambiguousActionDialog.AmbiguousRoutes = new[] { bestMatchedRegexRoute, route };
                                }

                                return dialogRoute;
                            }
                        }

                        bestMatchedRegexRoute = route;
                    }

                    continue;
                }

                var score = FindBestMatch(
                    route.TextCommandList,
                    messageText,
                    route.Threshold,
                    route.IgnoreCase,
                    route.IgnoreNonAlphanumericCharacters);

                if (score > bestMatchedScore)
                {
                    bestMatchedScore = score.GetValueOrDefault();
                    bestMatchedRoute = route;
                }
            }

            return bestMatchedRegexRoute ?? bestMatchedRoute;
        }

        private static bool CheckIfRegexCommandMatches(string messageText, DialogRoute route)
        {
            if (route?.RegexCommand is null)
            {
                return false;
            }

            var messageToCheck = route.IgnoreNonAlphanumericCharacters
                ? Regex.Replace(messageText, @"[^A-Za-z0-9 ]", string.Empty)
                : messageText;

            return route.RegexCommand.IsMatch(messageToCheck);
        }

        private static double? FindBestMatch(
            string[] choices,
            string utterance,
            double threshold = 0.5,
            bool ignoreCase = true,
            bool ignoreNonAlphanumeric = true)
        {
            if (!choices.Any(x => x.HasValue()))
            {
                return null;
            }

            double? bestMatchScore = null;
            var scores = FindAllMatches(choices, utterance, threshold, ignoreCase, ignoreNonAlphanumeric);
            foreach (var score in scores)
            {
                if (bestMatchScore == null || score > bestMatchScore)
                {
                    bestMatchScore = score;
                }
            }

            return bestMatchScore;
        }

        private static double[] FindAllMatches(
            string[] choices,
            string utterance,
            double threshold = 0.6,
            bool ignoreCase = true,
            bool ignoreNonAlphanumeric = true)
        {
            var matches = new List<double>(choices.Length);

            var utteranceToCheck = ignoreNonAlphanumeric
                ? Regex.Replace(utterance, @"[^A-Za-z0-9 ]", string.Empty)
                : utterance;

            var tokens = utterance.Split(' ');

            foreach (var choice in choices)
            {
                double score = 0;
                var choiceValue = choice.Trim();
                if (ignoreNonAlphanumeric)
                {
                    Regex.Replace(choiceValue, @"[^A-Za-z0-9 ]", string.Empty);
                }

                if (choiceValue.IndexOf(
                        utteranceToCheck,
                        ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0)
                {
                    score = (double)decimal.Divide(utteranceToCheck.Length, choiceValue.Length);
                }
                else if (utteranceToCheck.IndexOf(choiceValue, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0)
                {
                    score = Math.Min(0.5 + ((double)choiceValue.Length / utteranceToCheck.Length), 0.9);
                }
                else
                {
                    foreach (var token in tokens)
                    {
                        var matched = string.Empty;

                        if (choiceValue.IndexOf(
                                token,
                                ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) >= 0)
                        {
                            matched += token;
                        }

                        score = (double)decimal.Divide(matched.Length, choiceValue.Length);
                    }
                }

                if (score >= threshold)
                {
                    matches.Add(score);
                }
            }

            return matches.ToArray();
        }
    }
}
