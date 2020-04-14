// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T>
        where T : Dialog
    {
        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationTokens)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeCard = SendIntroCardAsync(turnContext, cancellationTokens);
                    //var response = MessageFactory.Attachment(welcomeCard, ssml: "Welcome to Bot Framework!");
                    //await turnContext.SendActivityAsync(response, cancellationTokens);
                    //await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationTokens);
                }
            }
        }

        private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var card = new HeroCard();
            card.Title = "Welcome to Bot Framework!";
            card.Text = @"Selct one of the below menu to proceed";
            card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
            card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.ImBack, "Active Directory Search", null, "Active Directory Search", "Active Directory Search", "ActiveDirectory Help"),
                new CardAction(ActionTypes.ImBack, "ServiceNow", null, "ServiceNow", "ServiceNow", "ServiceNow"),
                new CardAction(ActionTypes.ImBack, "Management Tools", null, "Management Tools", "Management Tools", "Management Tools"),
                new CardAction(ActionTypes.ImBack, "Search Analytics", null, "Search Analytics", "Search Analytics", "Search Analytics"),
                new CardAction(ActionTypes.ImBack, "Sharepoint Search", null, "Sharepoint Search", "Sharepoint Search", "Sharepoint Search"),
                new CardAction(ActionTypes.ImBack, "Comment/Feedback", null, "Comment/Feedback", "Comment/Feedback", "Feedback"),

            };

            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }

        // Load attachment from embedded resource.
        private Attachment CreateAdaptiveCardAttachment()
        {
            var cardResourcePath = "CoreBot.Cards.welcomeCard.json";

            using (var stream = GetType().Assembly.GetManifestResourceStream(cardResourcePath))
            {
                using (var reader = new StreamReader(stream))
                {
                    var adaptiveCard = reader.ReadToEnd();
                    return new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(adaptiveCard),
                    };
                }
            }
        }
    }
}
