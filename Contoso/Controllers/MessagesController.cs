using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Contoso.Models;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Contoso.DataModels;
using Microsoft.WindowsAzure.MobileServices;

namespace Contoso
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                string endOutput = "";

                MobileServiceClient mobileClient = AzureDatabaseService.AzureDatabaseServiceInstance.AzureClient;
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                var userMessage = activity.Text;
                bool accountsRequest = false;

                if (userMessage.ToLower().Contains("account"))
                {
                    accountsRequest = true;
                }

                if (accountsRequest)
                {
                    endOutput = "Didn't understand that request, sorry";
                    //GET
                    if (userMessage.ToLower().Equals("get bank accounts"))
                    {
                        endOutput = "";
                        //queries database using AzureDatabaseServices GET method, creates list of bank accounts in database
                        List<ContosoBankAccounts> bankAccountList = await AzureDatabaseService.AzureDatabaseServiceInstance.getAccounts();
                        //endOutput = $"{AzureDatabaseService.AzureDatabaseServiceInstance.AzureClient}";
                        foreach (ContosoBankAccounts bankAccount in bankAccountList)
                        {
                            endOutput += $"Bank Account = {bankAccount.id}\n\n Account Balance = ${bankAccount.balance}\n\n";
                        }
                    }

                    //POST
                    if (userMessage.ToLower().Contains("create bank account"))
                    {
                        ContosoBankAccounts bankAccount = new ContosoBankAccounts()
                        {
                            id = userMessage.Substring(20, 5),
                            balance = double.Parse(userMessage.Substring(26)),
                            createdAt = DateTime.Now
                        };

                        await AzureDatabaseService.AzureDatabaseServiceInstance.addAccount(bankAccount);
                        endOutput = $"Account \"{bankAccount.id}\" created";
                    }

                    //PUT
                    if (userMessage.ToLower().Contains("update"))
                    {
                        ContosoBankAccounts bankAccount = new ContosoBankAccounts()
                        {
                            id = userMessage.Substring(20, 5),
                            balance = double.Parse(userMessage.Substring(26)),
                            createdAt = DateTime.Now
                        };

                        await AzureDatabaseService.AzureDatabaseServiceInstance.updateAccount(bankAccount);
                        endOutput = $"Account \"{bankAccount.id}\" updated to {bankAccount.balance}";
                    }

                    //DELETE
                    if (userMessage.ToLower().Contains("delete"))
                    {
                        ContosoBankAccounts bankAccount = new ContosoBankAccounts()
                        {
                            id = userMessage.Substring(20)
                        };


                        endOutput = $"Account \"{bankAccount.id}\" deleted";
                        await AzureDatabaseService.AzureDatabaseServiceInstance.deleteAccount(bankAccount);
                    }

                    Activity reply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else{
                    //hello!
                    if (userMessage.ToLower().Equals("hello"))
                    {
                        endOutput = $"Hi there, {activity.From.Name}! I'm ContosoBot, welcome to Contoso Bank!";

                        Activity reply = activity.CreateReply(endOutput);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    {
                        string fromCurrency = userMessage.ToUpper().Substring(0, 3);
                        string toCurrency = userMessage.ToUpper().Substring(4, 3);
                        string currencyString = userMessage.Substring(8);
                        double currencyDouble = double.Parse(currencyString);
                        //endOutput = $"interpreted as: convert {currencyDouble} {fromCurrency} to {toCurrency}\n";

                        HttpClient HttpClient = new HttpClient();
                        string getCurrency = await HttpClient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + fromCurrency + "&symbols=" + toCurrency));
                        var json = JObject.Parse(getCurrency);
                        var rate = double.Parse(json["rates"][toCurrency].ToString());

                        //endOutput += $"\nResult = {rate * currencyDouble} {toCurrency}";

                        Activity thumbnailReply = activity.CreateReply();
                        thumbnailReply.Recipient = activity.From;
                        thumbnailReply.Type = "message";
                        thumbnailReply.Attachments = new List<Attachment>();

                        List<CardImage> cardImage = new List<CardImage>();
                        cardImage.Add(new CardImage(url: "http://i.imgur.com/je1BEZr.png"));

                        ThumbnailCard exchangeCard = new ThumbnailCard()
                        {
                            Title = $"{fromCurrency} to {toCurrency}",
                            Subtitle = $"{currencyString} {fromCurrency} is equal to {rate * currencyDouble} {toCurrency}",
                            Images = cardImage
                        };

                        Attachment cardAttachment = exchangeCard.ToAttachment();
                        thumbnailReply.Attachments.Add(cardAttachment);
                        await connector.Conversations.SendToConversationAsync(thumbnailReply);
                    }
                }

                /*if (userMessage.ToLower().Contains("account")) {
                    //GET
                    if (userMessage.ToLower().Equals("get bank accounts"))
                    {
                        //queries database using AzureDatabaseServices GET method, creates list of bank accounts in database
                        List<ContosoBankAccounts> bankAccountList = await AzureDatabaseService.instance.getAccounts();
                        foreach (ContosoBankAccounts bankAccount in bankAccountList)
                        {
                            endOutput += $"Bank Account Number = {bankAccount.id}\n Account Balance = ${bankAccount.balance}\n";
                            Activity reply = activity.CreateReply(endOutput);
                            await connector.Conversations.ReplyToActivityAsync(reply);
                        }
                    }

                    //POST
                    if (userMessage.ToLower().Equals("create bank account"))
                    {
                        ContosoBankAccounts bankAccount = new ContosoBankAccounts();
                        bankAccount.id = userMessage.Substring(20, 5);
                        bankAccount.balance = double.Parse(userMessage.Substring(25, 10));
                        bankAccount.createdAt = DateTime.Now;

                        await AzureDatabaseService.instance.addAccount(bankAccount);
                        endOutput = $"Account {bankAccount.id} created";
                        Activity reply = activity.CreateReply(endOutput);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }*/
                /*else
                {
                    string fromCurrency = userMessage.ToUpper().Substring(0, 3);
                    string toCurrency = userMessage.ToUpper().Substring(4, 3);
                    string currencyString = userMessage.Substring(8);
                    double currencyDouble = double.Parse(currencyString);
                    //endOutput = $"interpreted as: convert {currencyDouble} {fromCurrency} to {toCurrency}\n";

                    HttpClient HttpClient = new HttpClient();
                    string getCurrency = await HttpClient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + fromCurrency + "&symbols=" + toCurrency));
                    var json = JObject.Parse(getCurrency);
                    var rate = double.Parse(json["rates"][toCurrency].ToString());

                    //endOutput += $"\nResult = {rate * currencyDouble} {toCurrency}";

                    Activity thumbnailReply = activity.CreateReply();
                    thumbnailReply.Recipient = activity.From;
                    thumbnailReply.Type = "message";
                    thumbnailReply.Attachments = new List<Attachment>();

                    List<CardImage> cardImage = new List<CardImage>();
                    cardImage.Add(new CardImage(url: "http://i.imgur.com/je1BEZr.png"));

                    ThumbnailCard exchangeCard = new ThumbnailCard()
                    {
                        Title = $"{fromCurrency} to {toCurrency}",
                        Subtitle = $"{currencyString} {fromCurrency} is equal to {rate * currencyDouble} {toCurrency}",
                        Images = cardImage
                    };

                    Attachment cardAttachment = exchangeCard.ToAttachment();
                    thumbnailReply.Attachments.Add(cardAttachment);
                    await connector.Conversations.SendToConversationAsync(thumbnailReply);
                }*/

                
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}