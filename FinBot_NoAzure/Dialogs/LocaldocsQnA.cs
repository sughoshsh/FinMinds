using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.AI.QnA.Models;
using Microsoft.Bot.Builder.Dialogs;
using CoreBot.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;



namespace CoreBot.Dialogs
{
    public class LocaldocsQnA : CancelAndHelpDialog
    {
        public class QnAPair
        {
            public string Question { get; set; }
            public string Answer { get; set; }
        }
        public List<QnAPair> LoadLocalQnA()
        {
            try
            {
                //local json doc path
                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Docs", "QnAFile_1.json");
                var json = File.ReadAllText(filePath);
                var qnaPairs = JsonConvert.DeserializeObject<List<QnAPair>>(json);
                return qnaPairs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Loading Local QnA: {ex.Message}");
            return new List<QnAPair>(); }
        }



        public string GetAnswer(string question, List<QnAPair> docs)
        {
            var keywords = question.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var qna = docs
                      .Where(q => keywords.Any(keyword => q.Question.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                      .OrderByDescending(q => keywords.Count(keyword => q.Question.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                      .FirstOrDefault();

            return qna?.Answer ?? "Sorry, I don't have an answer for that.";

        }

        public LocaldocsQnA()
            : base(nameof(LocaldocsQnA))
        {
            
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
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
            
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Please enter the question.")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["Question"] = (string)stepContext.Result;
            var localQnA = LoadLocalQnA();

            var answer = GetAnswer((string)stepContext.Result, localQnA);
                //localQnA.Where(qa =>
            //qa.Question.Contains((string)stepContext.Result, StringComparison.OrdinalIgnoreCase)).Select(qa => qa.Answer).ToList();
               // localQnA.FirstOrDefault(qa => string.Equals(qa.Question, (string)stepContext.Result,
               // StringComparison.OrdinalIgnoreCase))?.Answer;

            //if (answer.Any())
            if (!string.IsNullOrEmpty(answer))
            {
                Console.WriteLine("Relevant Answers:");
                //foreach (var ans in answer)
                //{
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
                //}
            }
            else
            {
                Console.WriteLine("No Answer found for the given question");
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("No Answer found for the given question."), cancellationToken);
            }
            // Call Language QnA Maker service
            //var httpClient = _httpClientFactory.CreateClient();

            //var qnaMaker = new CustomQuestionAnswering(new QnAMakerEndpoint
            //{
            //    KnowledgeBaseId = _configuration["LearnProjectName"],
            //    EndpointKey = _configuration["LanguageEndPointKey"],
            //    Host = _configuration["LanguageEndpointHostName"],
            //    QnAServiceType = ServiceType.Language
            //},
            //null, httpClient
            //);

            //var options = new QnAMakerOptions { Top = 1 };

            // The actual call to the QnA Maker service.
            //var response = await qnaMaker.GetAnswersAsync(stepContext.Context, options);
            //if (response != null && response.Length > 0)
            //{
            //    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
            //}
            //else
            //{
            //    await stepContext.Context.SendActivityAsync(MessageFactory.Text("No answers were found."), cancellationToken);
            //}

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to ask more questions?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
    }
}