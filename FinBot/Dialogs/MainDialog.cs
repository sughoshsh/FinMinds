// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.2

using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CoreBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBot.Dialogs
{
    
    public class MainDialog : ComponentDialog
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        protected readonly ILogger Logger;
        
        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ILogger<FinacleSqldbContext> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory
            , IServiceScopeFactory serviceScopeFactory
           )
            : base(nameof(MainDialog))
        {

            Logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            var scope = _serviceScopeFactory.CreateScope();
            {
                var options = scope.ServiceProvider.GetRequiredService<FinacleSqldbContext>();
                var azureSQLDBDialog = new AzureSQLDBDialog(options, _configuration, logger, _serviceScopeFactory);
                AddDialog(azureSQLDBDialog);
                var generateReport = new GenerateReport(options, _configuration, logger, _serviceScopeFactory);
                AddDialog(generateReport);
            }
            AddDialog(new QnALearnDialog(configuration, httpClientFactory));
            AddDialog(new QnAfuncDialog(configuration, httpClientFactory));
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
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please choose an option to proceed further."), cancellationToken);
            List<string> operationList = new List<string> { "Learning"
                ,"Functional Queries"
                ,"Operation Queries"
                ,"Generate BAU Reports" };
            // Create card
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0))
            {
                // Use LINQ to turn the choices into submit actions
                Actions = operationList.Select(choice => new AdaptiveSubmitAction
                {
                    Title = choice,
                    Data = choice,  // This will be a string
                }).ToList<AdaptiveAction>(),
            };
            // Prompt
            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = (Activity)MessageFactory.Attachment(new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    // Convert the AdaptiveCard to a JObject
                    Content = JObject.FromObject(card),
                }),
                Choices = ChoiceFactory.ToChoices(operationList),
                // Don't render the choices outside the card
                Style = ListStyle.None,
            },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["UserOperation"] = ((FoundChoice)stepContext.Result).Value;

            string operation = (string)stepContext.Values["UserOperation"];
            if (operation.Equals("Operation Queries"))
            {
                return await stepContext.BeginDialogAsync(nameof(AzureSQLDBDialog), null, cancellationToken);
            }
            else if (operation.Equals("Generate BAU Reports"))
            { 
                return await stepContext.BeginDialogAsync(nameof(GenerateReport), null, cancellationToken);
            }
            else if (operation.Equals("Learning"))
            {
                return await stepContext.BeginDialogAsync(nameof(QnALearnDialog), null, cancellationToken);
            }
            else if (operation.Equals("Functional Queries"))
            {
                return await stepContext.BeginDialogAsync(nameof(QnAfuncDialog), null, cancellationToken);
            }
            else
            {
                return await stepContext.BeginDialogAsync(nameof(QnAfuncDialog), null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}