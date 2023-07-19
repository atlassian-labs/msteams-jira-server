using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Bot.Builder.Dialogs;

namespace MicrosoftTeamsIntegration.Artifacts.Bots.DialogRouter
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "SA1401", Justification = "Public API")]
    public sealed class DialogRoute
    {
        public readonly int Order;
        public readonly Regex? RegexCommand;
        public readonly string[] TextCommandList;
        public readonly bool IgnoreCase;
        public readonly bool IgnoreNonAlphanumericCharacters;
        public readonly double Threshold;
        public readonly Type DialogType;
        public readonly bool IsAuthenticationRequired;
        public readonly object? Options;
        public Dialog? Dialog;

        public DialogRoute(
            Type dialogType,
            string[] textCommandList,
            bool isAuthenticationRequired = true,
            double threshold = 0.5,
            bool ignoreCase = true,
            bool ignoreNonAlphaNumericCharacters = true,
            object? options = null)
        {
            DialogType = dialogType;
            TextCommandList = textCommandList;
            IsAuthenticationRequired = isAuthenticationRequired;
            Threshold = threshold;
            IgnoreCase = ignoreCase;
            IgnoreNonAlphanumericCharacters = ignoreNonAlphaNumericCharacters;
            Options = options;
        }

        public DialogRoute(
            Type dialogType,
            string command,
            bool isAuthenticationRequired = true,
            double threshold = 0.5,
            bool ignoreCase = true,
            bool ignoreNonAlphaNumericCharacters = true,
            object? options = null)
            : this(
                dialogType,
                new[] { command },
                isAuthenticationRequired,
                threshold,
                ignoreCase,
                ignoreNonAlphaNumericCharacters,
                options)
        {
        }

        public DialogRoute(
            Type dialogType,
            Regex regexCommand,
            int order = 0,
            bool isAuthenticationRequired = true,
            bool ignoreCase = true,
            bool ignoreNonAlphaNumericCharacters = true,
            object? options = null)
        {
            DialogType = dialogType;
            RegexCommand = regexCommand;
            Order = order;
            IsAuthenticationRequired = isAuthenticationRequired;
            IgnoreCase = ignoreCase;
            IgnoreNonAlphanumericCharacters = ignoreNonAlphaNumericCharacters;
            Options = options;
            TextCommandList = Array.Empty<string>();
        }
    }
}
