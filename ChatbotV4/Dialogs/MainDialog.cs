// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using AdaptiveCards;
using Newtonsoft.Json.Linq;
using CoreBot.Dialogs;
using CoreBot;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IntentsRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        private Activity promptMessage;

        public string CompaniesSelected { get; private set; }
        public object AdaptiveCard { get; private set; }
        public string CHOICEPROMPT { get; private set; }

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(IntentsRecognizer luisRecognizer, ActiveDirDialog activedirDialog,BookingDialog bookingDialog, SharepointDialog sharepointDialog,ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(activedirDialog);
            AddDialog(sharepointDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: LUIS is not configured. To enable all capabilities, add 'LuisAppId', 'LuisAPIKey' and 'LuisAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);

                return await stepContext.NextAsync(null, cancellationToken);
            }

            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Please wait while we are processing your request?\nOr Type Something";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }


        private async Task<DialogTurnResult> PromptWithAdaptiveCardAsync(
     WaterfallStepContext stepContext,
     CancellationToken cancellationToken)
        {
            // Define choices
            var choices = new[] { "One", "Two", "Three" };

            // Create card
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = choices.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),
            };

            // Prompt
            return await stepContext.PromptAsync(
                CHOICEPROMPT,
                new PromptOptions
                {
                    Prompt = (Activity)MessageFactory.Attachment(new Attachment
                    {
                        ContentType = AdaptiveCard.ToString(),
                // Convert the AdaptiveCard to a JObject
                Content = JObject.FromObject(card),
                    }),
                    Choices = ChoiceFactory.ToChoices(choices),
            // Don't render the choices outside the card
            Style = ListStyle.None,
                },
                cancellationToken);
        }

        //static async Task DisplayOptionsAsync(WaterfallStepContext turnContext, CancellationToken cancellationToken)
        //{
        //    // Create a HeroCard with options for the user to interact with the bot.
        //    var card = new HeroCard
        //    {
        //        Text = "You can upload an image or select one of the following choices",
        //        Buttons = new List<CardAction>
        //{
        //    // Note that some channels require different values to be used in order to get buttons to display text.
        //    // In this code the emulator is accounted for with the 'title' parameter, but in other channels you may
        //    // need to provide a value for other parameters like 'text' or 'displayText'.
        //    new CardAction(ActionTypes.ImBack, title: "1. Inline Attachment", value: "1"),
        //    new CardAction(ActionTypes.ImBack, title: "2. Internet Attachment", value: "2"),
        //    new CardAction(ActionTypes.ImBack, title: "3. Uploaded Attachment", value: "3"),
        //},
        //    };

        //    var reply = MessageFactory.Attachment(card.ToAttachment());
        //   // await turnContext.SendActivityAsync(reply, cancellationToken);
        //}

       

        

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


        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!_luisRecognizer.IsConfigured)
            {
                // LUIS is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }

            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<IntentOperations>(stepContext.Context, cancellationToken);
            Console.WriteLine(luisResult.Intents);
            //await stepContext.ReplaceDialogAsync(nameof(HRDialog), "Hello HR Tools", cancellationToken);
            //return await stepContext.BeginDialogAsync(nameof(HRDialog), new HRDetails(), cancellationToken);
            //var list = stepContext.Values[CompaniesSelected] as List<string>;

           // await stepContext.ReplaceDialogAsync(nameof(HRDialog), list, cancellationToken);
            switch (luisResult.TopIntent().intent)
            {


                case IntentOperations.Intent.ActiveDirectory_Help:
                    await ShowWarningForActiveDirectory(stepContext.Context, luisResult, cancellationToken);

                    var activedirDetails = new ActiveDirDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        personName = luisResult.ADEntities.personName,
                        email = luisResult.ADEntities.email,
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    //HR Tool should be called here 
                    return await stepContext.BeginDialogAsync(nameof(ActiveDirDialog), activedirDetails, cancellationToken);

                case IntentOperations.Intent.HRTool:
                   
                    //await ShowWarningForUnsupportedCities(stepContext.Context, luisResult, cancellationToken);

                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new BookingDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        Destination = luisResult.ToEntities.Airport,
                        Origin = luisResult.FromEntities.Airport,
                        TravelDate = luisResult.TravelDate,
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    //HR Tool should be called here 
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case IntentOperations.Intent.Sharepoint:
                   

                    var sharepointDetails = new SharepointDetails()
                    {
                        // Get destination and origin from the composite entities arrays.
                        SharepointSearch = luisResult.SPDocEntities.SharepointSearch,
                        DocType = luisResult.SPDocEntities.DocType,
                    };

                    // Run the BookingDialog giving it whatever details we have from the LUIS call, it will fill out the remainder.
                    //HR Tool should be called here 
                    return await stepContext.BeginDialogAsync(nameof(SharepointDialog), sharepointDetails, cancellationToken);

                //List<string> res = new List<string>();
                //List<string> list = new List<string>();
                //string searchstr =null;


                //var luisResultSp = await _luisRecognizer.RecognizeAsync<IntentOperations>(stepContext.Context, cancellationToken);
                //SharepointSearchDialog sharepointservice = new SharepointSearchDialog();
                //searchstr = luisResultSp.SPDocEntities.SharepointSearch.ToString();
                //string docTyp=null;
                //if (luisResultSp.SPDocEntities.DocType == null)
                //{

                //    var val=await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("Please enter your name.") }, cancellationToken);
                //    var welcomeCard = SendFileTypeCardAsync(stepContext.Context, cancellationToken);

                //}

                //else
                //{
                //     docTyp = luisResultSp.SPDocEntities.DocType.ToString();
                //}

                ////await sharepointservice.writeFile("Hey From MainDialog");

                //res = await sharepointservice.SharepointSearchEng(searchstr,docTyp);
                //SendResCardAsync(stepContext.Context, cancellationToken,res);


                //break;

                case IntentOperations.Intent.Feedback:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getFeedbackMessageText = "TODO: Feedback";
                    var getFeedbackMessage = MessageFactory.Text(getFeedbackMessageText, getFeedbackMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getFeedbackMessage, cancellationToken);
                    break;
                case IntentOperations.Intent.ServiceNow:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getServiceNowMessageText = "TODO: ServiceNow";
                    var getServiceNowMessage = MessageFactory.Text(getServiceNowMessageText, getServiceNowMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getServiceNowMessage, cancellationToken);
                    break;
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {luisResult.TopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        // Shows a warning if the requested From or To cities are recognized as entities but they are not in the Airport entity list.
        // In some cases LUIS will recognize the From and To composite entities as a valid cities but the From and To Airport values
        // will be empty if those entity values can't be mapped to a canonical item in the Airport.
        private static async Task ShowWarningForUnsupportedCities(ITurnContext context, IntentOperations luisResult, CancellationToken cancellationToken)
        {

            int a = 1;
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                   unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (a==1)
            {
                var messageText = $"Enter the document name to search in Sharepoint: {string.Join(',', unsupportedCities)}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private static async Task ShowWarningForActiveDirectory(ITurnContext context, IntentOperations luisResult, CancellationToken cancellationToken)
        {

            int a = 1;
            var unsupportedCities = new List<string>();

            var fromEntities = luisResult.FromEntities;
            if (!string.IsNullOrEmpty(fromEntities.From) && string.IsNullOrEmpty(fromEntities.Airport))
            {
                unsupportedCities.Add(fromEntities.From);
            }

            var toEntities = luisResult.ToEntities;
            if (!string.IsNullOrEmpty(toEntities.To) && string.IsNullOrEmpty(toEntities.Airport))
            {
                unsupportedCities.Add(toEntities.To);
            }

            if (a == 1)
            {
                var messageText = $"Search People using Phone Number ,Email id";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await context.SendActivityAsync(message, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is BookingDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
