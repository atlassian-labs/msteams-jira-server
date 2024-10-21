using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Hosting;
using MicrosoftTeamsIntegration.Artifacts.Bots.Dialogs;
using MicrosoftTeamsIntegration.Artifacts.Extensions;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter
{
    [PublicAPI]
    public sealed partial class DialogRouteService : IDialogRouteService
    {
        private static readonly string[] DialogRouteCommands = new[] { "cancel", "back", "undo", "reset" };
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

            var hostingEnvironment = serviceProvider.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            var routes = new List<DialogRoute>
            {
                new DialogRoute(typeof(AmbiguousActionDialog), string.Empty, isAuthenticationRequired: false)
                {
                    Dialog = new AmbiguousActionDialog(hostingEnvironment?.IsDevelopment() ?? false)
                },
                new DialogRoute(
                    typeof(CancelDialog),
                    DialogRouteCommands,
                    isAuthenticationRequired: false)
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
                    bestMatchedRegexRoute = HandleRegexRoute(route, messageText, bestMatchedRegexRoute);
                    if (bestMatchedRegexRoute != null && route.Order <= bestMatchedRegexRoute.Order)
                    {
                        return bestMatchedRegexRoute;
                    }
                }
                else
                {
                    KeyValuePair<DialogRoute?, double> textCommandRouteResult =
                        HandleTextCommandRoute(route, messageText, bestMatchedScore, bestMatchedRoute);
                    bestMatchedRoute = textCommandRouteResult.Key;
                    bestMatchedScore = textCommandRouteResult.Value;
                }
            }

            return bestMatchedRegexRoute ?? bestMatchedRoute;
        }

        private DialogRoute? HandleRegexRoute(DialogRoute route, string messageText, DialogRoute? bestMatchedRegexRoute)
        {
            var regexMatches = CheckIfRegexCommandMatches(messageText, route);

            if (!regexMatches)
            {
                return bestMatchedRegexRoute;
            }

            if (bestMatchedRegexRoute != null)
            {
                if (route.Order > bestMatchedRegexRoute.Order)
                {
                    return bestMatchedRegexRoute;
                }

                if (route.Order == bestMatchedRegexRoute.Order)
                {
                    return HandleAmbiguousRoutes(route, bestMatchedRegexRoute);
                }
            }

            return route;
        }

        private DialogRoute? HandleAmbiguousRoutes(DialogRoute currentRoute, DialogRoute bestMatchedRegexRoute)
        {
            if (_routes.TryGetValue(nameof(AmbiguousActionDialog), out var dialogRoute) &&
                dialogRoute?.Dialog is AmbiguousActionDialog ambiguousActionDialog)
            {
                ambiguousActionDialog.AmbiguousRoutes = new[] { bestMatchedRegexRoute, currentRoute };
                return dialogRoute;
            }

            return null;
        }

        private static KeyValuePair<DialogRoute?, double> HandleTextCommandRoute(
            DialogRoute route,
            string messageText,
            double bestMatchedScore,
            DialogRoute? bestMatchedRoute)
        {
            var score = FindBestMatch(
                route.TextCommandList,
                messageText,
                route.Threshold,
                route.IgnoreCase,
                route.IgnoreNonAlphanumericCharacters);

            if (score > bestMatchedScore)
            {
                bestMatchedScore = score.GetValueOrDefault();
                return new KeyValuePair<DialogRoute?, double>(route, bestMatchedScore);
            }

            return new KeyValuePair<DialogRoute?, double>(bestMatchedRoute, bestMatchedScore);
        }

        private static bool CheckIfRegexCommandMatches(string messageText, DialogRoute route)
        {
            var messageToCheck = route.IgnoreNonAlphanumericCharacters
                ? Regex.Replace(messageText, @"[^A-Za-z0-9 ]", string.Empty)
                : messageText;

            return route?.RegexCommand?.IsMatch(messageToCheck) ?? false;
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
            var utteranceToCheck = ProcessUtterance(utterance, ignoreNonAlphanumeric);
            var tokens = utterance.Split(' ');

            foreach (var choice in choices)
            {
                var choiceValue = ProcessChoice(choice, ignoreNonAlphanumeric);
                double score = CalculateScore(utteranceToCheck, choiceValue, tokens, ignoreCase);

                if (score >= threshold)
                {
                    matches.Add(score);
                }
            }

            return matches.ToArray();
        }

        private static string ProcessUtterance(string utterance, bool ignoreNonAlphanumeric)
        {
            return ignoreNonAlphanumeric
                ? Regex.Replace(utterance, @"[^A-Za-z0-9 ]", string.Empty)
                : utterance;
        }

        private static string ProcessChoice(string choice, bool ignoreNonAlphanumeric)
        {
            var choiceValue = choice.Trim();
            if (ignoreNonAlphanumeric)
            {
                choiceValue = ChoiceRegex().Replace(choiceValue, string.Empty);
            }

            return choiceValue;
        }

        private static double CalculateScore(
            string utteranceToCheck,
            string choiceValue,
            string[] tokens,
            bool ignoreCase)
        {
            double score;

            if (choiceValue.Contains(
                    utteranceToCheck,
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                score = (double)decimal.Divide(utteranceToCheck.Length, choiceValue.Length);
            }
            else if (utteranceToCheck.Contains(
                         choiceValue,
                         ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                score = Math.Min(0.5 + ((double)choiceValue.Length / utteranceToCheck.Length), 0.9);
            }
            else
            {
                score = CalculateTokenScore(tokens, choiceValue, ignoreCase);
            }

            return score;
        }

        private static double CalculateTokenScore(string[] tokens, string choiceValue, bool ignoreCase)
        {
            double score = 0;
            var matched = tokens.Where(token =>
                choiceValue.Contains(token, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                .Aggregate(string.Empty, (current, token) => current + token);

            if (!string.IsNullOrEmpty(matched))
            {
                score = (double)decimal.Divide(matched.Length, choiceValue.Length);
            }

            return score;
        }

        [GeneratedRegex(@"[^A-Za-z0-9 ]")]
        private static partial Regex ChoiceRegex();
    }
}
