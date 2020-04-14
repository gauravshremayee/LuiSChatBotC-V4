// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreBot;
using CoreBot.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class ActiveDirDialog : CancelAndHelpDialog
    {
        private const string DestinationStepMsgText = "Enter the type of document you want to search ";
        private const string OriginStepMsgText = "Enter the name of document you want to search";
        string personname = null;
        string email = null;
        public ActiveDirDialog()
            : base(nameof(ActiveDirDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                DestinationStepAsync,
                ConfirmStepAsync,
                FinalStepAsync,
            }));



            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        //private static async Task SendIntroCardAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        //{
        //    var card = new HeroCard();
        //    card.Title = "Welcome to Bot Framework!";
        //    card.Text = @"Welcome to Welcome Users bot sample! This Introduction card
        //                 is a great way to introduce your Bot to the user and suggest
        //                 some things to get them started. We use this opportunity to
        //                 recommend a few next steps for learning more creating and deploying bots.";
        //    card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };
        //    card.Buttons = new List<CardAction>()
        //    {
        //        new CardAction(ActionTypes.OpenUrl, "HR Contingent", null, "HR Contingency", "Get an overview", "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
        //        new CardAction(ActionTypes.OpenUrl, "Management Tools", null, "Management Tools", "Ask a question", "https://stackoverflow.com/questions/tagged/botframework"),
        //    };

        //    var response = MessageFactory.Attachment(card.ToAttachment());
        //    await turnContext.SendActivityAsync(response, cancellationToken);
        //}
        private async Task<DialogTurnResult> DestinationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personNameDetails = (ActiveDirDetails)stepContext.Options;
            //docNameDetails.DocType = (string)stepContext.Result;

            personname = personNameDetails.personName;
            email = personNameDetails.email;
            if (personNameDetails.personName == null)
            {
                var promptMessage = MessageFactory.Text(DestinationStepMsgText, DestinationStepMsgText, InputHints.ExpectingInput);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            }

            return await stepContext.NextAsync(personNameDetails.personName, cancellationToken);
        }

        private static async Task SendResCardAsync(ITurnContext turnContext, CancellationToken cancellationToken, List<string> resSpList)

        {
            var card = new HeroCard();
            card.Title = "Sharepoint Result";
            card.Text = @"Click on below result";
            card.Images = new List<CardImage>() { new CardImage("https://aka.ms/bf-welcome-card-image") };

            foreach (var rs in resSpList)
            {

                string[] subLink = rs.Split('/');
                string docTitle = subLink[subLink.Length - 1];
                card.Buttons = new List<CardAction>()
            {
                new CardAction(ActionTypes.OpenUrl, docTitle, null, docTitle, "All",rs),


            };
            }
            var response = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(response, cancellationToken);
        }




        private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var personNameDetails = (ActiveDirDetails)stepContext.Options;
           

            var messageText = $"Please confirm personName: {personNameDetails.personName}  and emailId: {personNameDetails.email}  Is this correct?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if ((bool)stepContext.Result)
            {
                var bookingDetails = (BookingDetails)stepContext.Options;

                return await stepContext.EndDialogAsync(bookingDetails, cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}
