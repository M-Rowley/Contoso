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
                HttpClient luisClient = new HttpClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                string userMessage = activity.Text; //User's incoming message
                string endOutput = ""; //Used for final reply

                //LUIS is used to determine the type of query and, for greetings/GETs, execute the method
                string luisQuery = await luisClient.GetStringAsync(new Uri("https://api.projectoxford.ai/luis/v2.0/apps/90953ceb-19de-4415-9d85-0e11a325f27f?subscription-key=46c1c462651649af974ac4ec39dfa474&q=" + userMessage)); //sends GET request to luis
                string intent = JObject.Parse(luisQuery)["topScoringIntent"]["intent"].ToString(); //deserializes json object, stores top intent as string

                if (intent.Equals("greeting"))
                {
                    //Greets user, lists commands
                    endOutput = $"Hi there, {activity.From.Name}! I'm ContosoBot, welcome to Contoso Bank! How can I help you today?";
                }

                if (intent.Equals("exchange"))
                {
                    //Calculates exchange rates between two currencies
                    double currencyDouble;
                    string fromCurrency, toCurrency, currencyString;

                    //checks if message is either a number, or two currency codes followed by a number
                    if (double.TryParse(userMessage, out currencyDouble) || double.TryParse(userMessage.Substring(8), out currencyDouble))
                    {
                        //if input is just a number, uses the previous values of currency codes and input as amount
                        if (double.TryParse(userMessage, out currencyDouble))
                        {
                            toCurrency = userData.GetProperty<string>("toCurrencyLast");
                            fromCurrency = userData.GetProperty<string>("fromCurrencyLast");
                        }
                        else
                        {
                        
                            //else gets currency codes and amount from substrings
                            fromCurrency = userMessage.ToUpper().Substring(0, 3);
                            toCurrency = userMessage.ToUpper().Substring(4, 3);
                            currencyString = userMessage.Substring(8);
                            currencyDouble = double.Parse(currencyString);

                            userData.SetProperty<string>("toCurrencyLast", toCurrency);
                            userData.SetProperty<string>("fromCurrencyLast", fromCurrency);
                            await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
                        }

                        //queries the fixer.io API
                        HttpClient HttpClient = new HttpClient();
                        string getCurrency = await HttpClient.GetStringAsync(new Uri("http://api.fixer.io/latest?base=" + fromCurrency + "&symbols=" + toCurrency));
                        //deserializes JSON object and takes toCurrency rate
                        var rate = double.Parse(JObject.Parse(getCurrency)["rates"][toCurrency].ToString());
                        
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

                        //finalises card and replies
                        Attachment cardAttachment = exchangeCard.ToAttachment();
                        thumbnailReply.Attachments.Add(cardAttachment);
                        await connector.Conversations.SendToConversationAsync(thumbnailReply);
                    }
                    else
                    {
                        //reset the exchange intent so it's processed as a normal reply, not a card reply
                        intent = "";
                        endOutput = "The command to calculate an exchange is:\n\n\"[Original Currency Code (3)] [New Currency Code (3)] [amount]\"\n\n You may also simply enter a number to use the previous currencies";
                    }
                }

                if (intent.Equals("getAccounts"))
                {
                    //GET
                    //queries database using AzureDatabaseServices GET method, creates list of bank accounts in database
                    List<ContosoBankAccounts> bankAccountList = await AzureDatabaseService.AzureDatabaseServiceInstance.getAccounts();
                    foreach (ContosoBankAccounts bankAccount in bankAccountList)
                    {
                        endOutput += $"Bank Account = {bankAccount.id}\n\n Account Balance = ${bankAccount.balance}\n\n";
                    }
                }

                if (intent.Equals("createAccount"))
                {
                    //POST
                    //takes input of form "create bank account [name(5)] [amount]"
                    if (userMessage.ToLower().Substring(0, 19).Equals("create bank account"))
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
                    else
                    {
                        endOutput = "The command to create a bank account is:\n\n\"create bank account [id(5)] [amount]\"";
                    }
                }

                if (intent.Equals("updateAccount"))
                {
                    //PUT
                    //takes input of form "update bank account [name(5)] [amount]"
                    if (userMessage.ToLower().Substring(0, 19).Equals("update bank account"))
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
                    else
                    {
                        endOutput = "The command to update a bank account is:\n\n\"update bank account [id(5)] [amount]\"";
                    }
                }

                if (intent.Equals("deleteAccount"))
                {
                    //DELETE
                    //takes input of form "delete bank account [name(5)]"
                    if (userMessage.ToLower().Substring(0, 19).Equals("delete bank account"))
                    {
                        ContosoBankAccounts bankAccount = new ContosoBankAccounts()
                        {
                            id = userMessage.Substring(20)
                        };

                        endOutput = $"Account \"{bankAccount.id}\" deleted";
                        await AzureDatabaseService.AzureDatabaseServiceInstance.deleteAccount(bankAccount);
                    }
                    else
                    {
                        endOutput = "The command to delete a bank account is:\n\n\"delete bank account [id(5)]\"";
                    }
                }

                if (intent.Equals("none"))
                {
                    //Catch-all for when LUIS gets confused. AGAIN.
                    endOutput = "I didn't understand that command, sorry :(";
                }

                //reply
                if (!intent.Equals("exchange"))
                {
                    Activity reply = activity.CreateReply(endOutput);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }

                //end
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