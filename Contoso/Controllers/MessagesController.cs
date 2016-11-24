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
        /// Contoso Bank Bot
        /// Greets on receiving "Hello"
        /// Returns an exchange rate card on receiving {currency code} {currency code} {amount}
        /// interacts with a database on "create/get/update/delete bank account(s) [name] [amount]
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            { 
                MobileServiceClient mobileClient = AzureDatabaseService.AzureDatabaseServiceInstance.AzureClient;
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                var userMessage = activity.Text;
                bool accountsRequest = false; //Used for sorting between database commands and greet/exchange commands
                string endOutput = ""; //Used for final reply

                //Sorts database commands
                if (userMessage.ToLower().Contains("account"))
                {
                    accountsRequest = true;
                }

                //split between database and greet/exchange functions
                if (accountsRequest)
                {
                    endOutput = "Didn't understand that request, sorry";
                    //GET
                    //takes input of form "get bank accounts"
                    if (userMessage.ToLower().Equals("get bank accounts"))
                    {
                        endOutput = "";
                        //queries database using AzureDatabaseServices GET method, creates list of bank accounts in database
                        List<ContosoBankAccounts> bankAccountList = await AzureDatabaseService.AzureDatabaseServiceInstance.getAccounts();
                        foreach (ContosoBankAccounts bankAccount in bankAccountList)
                        {
                            endOutput += $"Bank Account = {bankAccount.id}\n\n Account Balance = ${bankAccount.balance}\n\n";
                        }
                    }

                    //POST
                    //takes input of form "create bank account [name(5)] [amount]"
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
                    //takes input of form "update bank account [name(5)] [amount]"
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
                    //takes input of form "delete bank account [name(5)]"
                    if (userMessage.ToLower().Contains("delete"))
                    {
                        ContosoBankAccounts bankAccount = new ContosoBankAccounts()
                        {
                            id = userMessage.Substring(20)
                        };


                        endOutput = $"Account \"{bankAccount.id}\" deleted";
                        await AzureDatabaseService.AzureDatabaseServiceInstance.deleteAccount(bankAccount);
                    }

                    //generates reply
                    Activity reply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
                else{
                    //Greets user, lists commands
                    if (userMessage.ToLower().Equals("hello"))
                    {
                        endOutput = $"Hi there, {activity.From.Name}! I'm ContosoBot, welcome to Contoso Bank!\n\nThe commands available are:\n\nExchange Rate lookup: (Currency Code (from)) (Currency Code(to)) (amount)\n\n get/create/update/delete bank account [Name (5 chars)] [amount]";

                        Activity reply = activity.CreateReply(endOutput);
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                    else
                    //Attempts currency exchange of form "[Currency Code (3)] [Currency Code (3)] [Amount]" using fixer.io
                    {
                        double currencyDouble;
                        string fromCurrency, toCurrency, currencyString;

                        //if input is just a number, uses the previous values of currency codes and input as amount
                        if (double.TryParse(userMessage, out currencyDouble))
                        {
                            toCurrency = userData.GetProperty<string>("toCurrencyLast");
                            fromCurrency = userData.GetProperty<string>("fromCurrencyLast");
                        }else{
                            //else gets currency codes and amount from substrings
                            fromCurrency = userMessage.ToUpper().Substring(0, 3);
                            toCurrency = userMessage.ToUpper().Substring(4, 3);
                            currencyString = userMessage.Substring(8);
                            currencyDouble = double.Parse(currencyString);

                            userData.SetProperty<string>("toCurrencyLast", toCurrency);
                            userData.SetProperty<string>("fromCurrencyLast", fromCurrency);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }

                        HttpClient HttpClient = new HttpClient();
                        string getCurrency = await HttpClient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + fromCurrency + "&symbols=" + toCurrency)); //sends GET request to fixer.io server
                        var json = JObject.Parse(getCurrency); //deserializes json object
                        var rate = double.Parse(json["rates"][toCurrency].ToString()); //takes toCurrency rate from fixer.io data object

                        //creates thumbnail card
                        Activity thumbnailReply = activity.CreateReply();
                        thumbnailReply.Recipient = activity.From;
                        thumbnailReply.Type = "message";
                        thumbnailReply.Attachments = new List<Attachment>();

                        //adds contoso logo to card
                        List<CardImage> cardImage = new List<CardImage>();
                        cardImage.Add(new CardImage(url: "http://i.imgur.com/je1BEZr.png"));

                        ThumbnailCard exchangeCard = new ThumbnailCard()
                        {
                            Title = $"{fromCurrency} to {toCurrency}",
                            Subtitle = $"{currencyDouble} {fromCurrency} is equal to {rate * currencyDouble} {toCurrency}\n\nType another amount to use the same currencies",
                            Images = cardImage
                        };

                        Attachment cardAttachment = exchangeCard.ToAttachment();
                        thumbnailReply.Attachments.Add(cardAttachment);
                        await connector.Conversations.SendToConversationAsync(thumbnailReply);
                    }
                }
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