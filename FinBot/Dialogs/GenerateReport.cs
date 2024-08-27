using CoreBot.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Schema;
using System.Runtime.Serialization;
using NuGet.ContentModel;
using Microsoft.Identity.Client;
using DocumentFormat.OpenXml.Presentation;
using ClosedXML.Excel;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;

namespace CoreBot.Dialogs
{
    public class GenerateReport : CancelAndHelpDialog
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
        public GenerateReport(FinacleSqldbContext context
            , IConfiguration configuration, ILogger<FinacleSqldbContext> logger
            , IServiceScopeFactory serviceScopeFactory)
            : base(nameof(GenerateReport))
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
            List<string> operationList = new List<string> { "Get All Customers Report", 
                "Get All Trading Book Report" };
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
            //stepContext.Values["UserChoice"] = ((FoundChoice)stepContext.Result).Value;

            choice = (string)stepContext.Values["UserChoice"];
            if (choice.Equals("Get All Customers Report"))
            {

                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Generate Cutomer Report? (Yes/No)")
                }, cancellationToken);
            }
            else if (choice.Equals("Get All Trading Book Report"))
            {
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
                {
                    Prompt = MessageFactory.Text("Generate Trading Book Report? (Yes/No)")
                }, cancellationToken);

            }

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("No Match")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["PromtValue"] = (String)stepContext.Result;
            var ans = (string)stepContext.Values["PromtValue"];
            //customer details
            if (choice.Equals("Get All Customers Report"))
            {

                if (ans != null && ans == "Yes")
                {
                    List<CustomerDetail> queryResult = FetchCustomerData();

                    if (queryResult == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Error generating Report..no data..."), cancellationToken);
                    }
                    else
                    {
                        var reportResult = GenerateCustomerExcelReport(queryResult);

                        var reply = MessageFactory.Text("Here is your report:");
                        reply.Attachments.Add(reportResult);

                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to generate more reports?")
                    }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Ans is No, Please re-try.")
                    }, cancellationToken);
                }

            }
            //Book Report
            else if (choice.Equals("Get All Trading Book Report"))
            {
                if (ans != null && ans == "Yes")
                {
                    List<TradingBook> queryResult = FetchTradingBookData();

                    if (queryResult == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Error generating Report..no data..."), cancellationToken);
                    }
                    else
                    {
                        var reportResult = GenerateBookExcelReport(queryResult);
                        var reply = MessageFactory.Text("Here is your report:");
                        reply.Attachments.Add(reportResult);

                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Would you like to generate more reports?")
                    }, cancellationToken);
                }
                else
                {
                    return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
                    {
                        Prompt = MessageFactory.Text("Ans is No, Please re-try.")
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

        public List<CustomerDetail> FetchCustomerData()
        {

            List<CustomerDetail> data;
            
            try
            {
                
                    data = _context.CustomerDetails.Select
                            (c => new CustomerDetail
                            {
                                CustCode = c.CustCode,
                                CustStatus = c.CustStatus,
                                CustLivePosition = c.CustLivePosition
                            })
                            .ToList();
               

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
            return data;
        }
        public List<TradingBook> FetchTradingBookData()
        {

            List<TradingBook> data;
                
                try
            {
                
                data = _context.TradingBooks.Select
                        (b => new TradingBook
                        {
                            BookName = b.BookName,
                            BookStatus = b.BookStatus,
                            BookLivePosition = b.BookLivePosition
                        })
                         .ToList();

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
            return data;
        }
        public Attachment GenerateCustomerExcelReport(List<CustomerDetail> customerData)
        {
            using (var workbook = new XLWorkbook())
            {
                
                var worksheet = workbook.Worksheets.Add("Customers");
                worksheet.Cell(1, 1).Value = "CustCode";
                worksheet.Cell(1, 2).Value = "CustStatus";
                worksheet.Cell(1, 3).Value = "Cust_Live_Position";


                for (int i = 0; i < customerData.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = customerData[i].CustCode;
                    worksheet.Cell(i + 2, 2).Value = customerData[i].CustStatus;
                    worksheet.Cell(i + 2, 3).Value = customerData[i].CustLivePosition;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    var attachment = new Attachment
                    {
                        Name = "CustomerReport.xlsx",
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        ContentUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{Convert.ToBase64String(stream.ToArray())}"
                    };

                    return attachment;


                }
            }
        }

        public Attachment GenerateBookExcelReport(List<TradingBook> bookData)
        {
            using (var workbook = new XLWorkbook())
            {

                var worksheet = workbook.Worksheets.Add("TradingBooks");
                worksheet.Cell(1, 1).Value = "BookName";
                worksheet.Cell(1, 2).Value = "BookStatus";
                worksheet.Cell(1, 3).Value = "Book_Live_Position";


                for (int i = 0; i < bookData.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = bookData[i].BookName;
                    worksheet.Cell(i + 2, 2).Value = bookData[i].BookStatus;
                    worksheet.Cell(i + 2, 3).Value = bookData[i].BookLivePosition;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Seek(0, SeekOrigin.Begin);

                    var attachment = new Attachment
                    {
                        Name = "TradingBookReport.xlsx",
                        ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        ContentUrl = $"data:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;base64,{Convert.ToBase64String(stream.ToArray())}"
                    };

                    return attachment;


                }
            }
        }
    }
}
