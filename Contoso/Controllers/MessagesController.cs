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
                
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                var userMessage = activity.Text;
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