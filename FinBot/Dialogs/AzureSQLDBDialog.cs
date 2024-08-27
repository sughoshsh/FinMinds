// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.13.2
using CoreBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;


namespace CoreBot.Dialogs
{
    public class AzureSQLDBDialog : CancelAndHelpDialog
    {
        private readonly FinacleSqldbContext _context;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FinacleSqldbContext> _logger;
        private readonly DbContextOptions<FinacleSqldbContext> _option;

        public string choice;
        public class DialogOptions
        {
            public int StartAtStep { get; set; }
            public string UserChoice { get; set; }
            
        }
        public AzureSQLDBDialog(FinacleSqldbContext context
            ,IConfiguration configuration, ILogger<FinacleSqldbContext> logger
            ,IServiceScopeFactory serviceScopeFactory
            )
            : base(nameof(AzureSQLDBDialog))
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _configuration = configuration;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                async (stepContext, cancellationToken) => 
                {
    
                // Check if we should skip this step
                if (stepContext.Options is DialogOptions options && options.StartAtStep == 1)
                {
                    return await stepContext.NextAsync(null, cancellationToken);
                }
                    return await IntroStepAsync(stepContext, cancellationToken);
                },
                IntroStepAsync1,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Please choose an option to proceed further."), cancellationToken);
            List<string> operationList = new List<string> { "Get Cpty Position Details",
                "Get Trading Book Dormancy Status",
                "Get EOD Feed Delivery Status",
                "Know Trade Message Delivery Status" };
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

        private async Task<DialogTurnResult> IntroStepAsync1(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Retrieve the UserChoice value from the dialog options
            if (stepContext.Options is DialogOptions options && !string.IsNullOrEmpty(options.UserChoice))
            {
                stepContext.Values["UserChoice"] = options.UserChoice;
            }
            else if (stepContext.Result != null && stepContext.Result is FoundChoice foundChoice)
            {
                stepContext.Values["UserChoice"] = foundChoice.Value;
            }
            
            choice = (string)stepContext.Values["UserChoice"];
            if (choice.Equals("Get Cpty Position Details"))
            {
                //Ask for cpty code and fetch details from DB

                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter cpty code name.")
                }, cancellationToken);
            }
            else if (choice.Equals("Get Trading Book Dormancy Status"))
            {

                //Ask for book name and fetch details from DB
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter trading book name.")
                }, cancellationToken);
            }
            else if (choice.Equals("Get EOD Feed Delivery Status"))
            {

                //ask for the feed file name and system name
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter the System name.")
                }, cancellationToken);
            }
            else if (choice.Equals("Know Trade Message Delivery Status"))
            {
                //ask for trade number
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Enter trade number")
                }, cancellationToken);

            }
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("No Match, ")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            // DB Fetch Operation.
            stepContext.Values["PromtValue"] = (String)stepContext.Result;
                        
            //customer details
            if (choice.Equals("Get Cpty Position Details"))
             {
                var cust = (string)stepContext.Values["PromtValue"];
                if (cust != null)
                {
                    CustomerDetail customer = FetchCptyPosition(cust);
                    if (customer == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Customer with code {cust} not found."), cancellationToken);
                    }
                    else
                    {
                        var replyText = customer.CustCode + " > Status:: " + customer.CustStatus + " Position:: " + customer.CustLivePosition;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                    }
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to get details for more codes?")
                    }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Customer Code/name is Empty, Please re-enter.")
                    }, cancellationToken);
                }
            
            }
            //Book Details
            else if (choice.Equals("Get Trading Book Dormancy Status"))
            {
                var book = (string)stepContext.Values["PromtValue"];
                if (book != null)
                {
                    TradingBook bookdetails = FetchBookPosition(book);
                    if (bookdetails == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Book with name {book} not found."), cancellationToken);
                    }
                    else
                    {
                        var replyText = bookdetails.BookName + " > Status :: " + bookdetails.BookStatus + " Position :: " + bookdetails.BookLivePosition;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                    }
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to get details for more books?")
                    }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Book Code/name is Empty, Please re-enter.")
                    }, cancellationToken);
                }

            }
            ////feed pub details
            else if (choice.Equals("Get EOD Feed Delivery Status"))
            {
                var sys = (string)stepContext.Values["PromtValue"];
                if (sys != null)
                {
                    Eodpublisher pubDetails = FetchPubStatus(sys);
                    if (pubDetails == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"System with name {sys} not found."), cancellationToken);
                    }
                    else
                    {
                        var replyText = pubDetails.SystemName + " > Status:: " + pubDetails.PubStatus + " EOD Date :: " + pubDetails.EodDate;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                    }
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to get publisher details for more systems?")
                    }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("System name is Empty, Please re-enter.")
                    }, cancellationToken);
                }

            }
            ////Trade Flow Details
            else if (choice.Equals("Know Trade Message Delivery Status"))
            {
                var id = (string)stepContext.Values["PromtValue"];
                if (id != null)
                {
                    TradeFlow flowStatus = FetchTradeFlowStatus(id);
                    if (flowStatus == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Trade Number {id} not found."), cancellationToken);
                    }
                    else
                    {
                        var replyText = flowStatus.TradeId + " > Status:: " + flowStatus.LoadStatus + " Load Date:: " + flowStatus.LoadDate;
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);
                    }
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to get details for more trades?")
                    }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Trade Number is Empty, Please re-enter.")
                    }, cancellationToken);
                }

            }
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Incorrect Option selected.?")
            }, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                // Store the UserChoice value in the dialog state
                stepContext.Values["UserChoice"] = stepContext.Values.ContainsKey("UserChoice") 
                    ? stepContext.Values["UserChoice"] : null;
                return await stepContext.ReplaceDialogAsync(InitialDialogId, new DialogOptions { StartAtStep = 1, UserChoice = (string)stepContext.Values["UserChoice"] }
                , cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        public CustomerDetail FetchCptyPosition(string str)
        {
            CustomerDetail position;
            try
            {
                
                position = (from c in _context.CustomerDetails
                          where c.CustCode == str
                          select c).FirstOrDefault(); 
                
            }
            catch (SqlException sqlEx)
            {
                // Handle SQL-specific exceptions
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (InvalidOperationException invOpEx)
            {
                // Handle invalid operation exceptions
                Console.WriteLine($"Invalid Operation: {invOpEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            return position;
        }
        public TradingBook FetchBookPosition(string str)
        {
            TradingBook position;
            try
            {
                position = (from b in _context.TradingBooks
                            where b.BookName == str
                            select b).FirstOrDefault(); //Query for trading book position details with book name.
            }
            catch (SqlException sqlEx)
            {
                // Handle SQL-specific exceptions
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (InvalidOperationException invOpEx)
            {
                // Handle invalid operation exceptions
                Console.WriteLine($"Invalid Operation: {invOpEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            return position;
        }
        public Eodpublisher FetchPubStatus(string str)
        {
            Eodpublisher status;
            try
            {
                status = (from p in _context.Eodpublishers
                            where p.SystemName == str
                            select p).FirstOrDefault(); //Query for feed pub status.
            }
            catch (SqlException sqlEx)
            {
                // Handle SQL-specific exceptions
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (InvalidOperationException invOpEx)
            {
                // Handle invalid operation exceptions
                Console.WriteLine($"Invalid Operation: {invOpEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            return status;
        }
        public TradeFlow FetchTradeFlowStatus(string str)
        {
            TradeFlow status;
            try
            {
                status = (from t in _context.TradeFlows
                          where t.TradeId == str
                          select t).FirstOrDefault(); //Query for trade flow status.
            }
            catch (SqlException sqlEx)
            {
                // Handle SQL-specific exceptions
                Console.WriteLine($"SQL Error: {sqlEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (InvalidOperationException invOpEx)
            {
                // Handle invalid operation exceptions
                Console.WriteLine($"Invalid Operation: {invOpEx.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Optionally, log the error or rethrow it
                throw;
            }
            return status;
        }
    }
}