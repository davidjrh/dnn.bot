using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Modules.Html;
using DotNetNuke.Services.Localization;
using DotNetNuke.Web.Api;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using Twilio;
using Message = Microsoft.Bot.Connector.Message;

namespace Dnn.Bot.Controllers
{
    [BotAuthentication]
    public class MessagesController : DnnApiController
    {
        internal class MyState
        {
            public string User { get; set; }
            public int UnknownMessageCount { get; set; }
            public bool CanaryMode { get; set; }
            public string TwoFactorAuthCode { get; set; }
            public bool IsValidIdentity { get; set; }
            public bool WaitingForPageTitle { get; set; }
            public int TabId { get; set; }
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// 
        // We allow DNN Anonymous calls to this method, because relies on the [BotAuthentication]
        // class attribute
        [AllowAnonymous]
        [HttpPost]
        public async Task<Message> Post([FromBody]Message message)
        {
            Message replyMessage;
            // BUG There is a bug on the GetBotPerUserInConversationData and can't implicitily convert as <MyState>
            var storedState = message.GetBotPerUserInConversationData<object>("myState")?.ToString();
            var myState = string.IsNullOrEmpty(storedState)
                ? new MyState()
                : JsonConvert.DeserializeObject<MyState>(storedState);

            if (message.Type == "Message")
            {
                if (!string.IsNullOrEmpty(myState.TwoFactorAuthCode))
                {
                    replyMessage = ValidateTwoFactorAuthCode(message, myState);
                }
                else
                {
                    replyMessage = GetSimpleAnswer(message, ref myState);
                    if (replyMessage == null)
                    {
                        replyMessage = GetUnknownReply(message, myState);
                    }
                }

                replyMessage.SetBotPerUserInConversationData("myState", myState);
            }
            else
            {
                replyMessage = HandleSystemMessage(message);
            }
            return replyMessage;
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }

        private Message GetSimpleAnswer(Message message, ref MyState myState)
        {
            var m = string.IsNullOrWhiteSpace(message.Text) ? string.Empty : message.Text.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(m) && message.Attachments.Count == 0)
            {
                myState.UnknownMessageCount = 0;
                return message.CreateReplyMessage($"Do you know what **whitespace** is?", "en");
            }

            if (m.Contains("hi!") || m.Contains("hello"))
            {
                myState.UnknownMessageCount = 0;
                return
                    message.CreateReplyMessage(
                        $"Hi, I'm **DNNBot**, your personal assistant to help you with your **DNN** websites", "en");
            }

            if (m.Contains("tecojotescacho"))
            {
                myState.UnknownMessageCount = 0;
                myState.CanaryMode = true;
                return message.CreateReplyMessage(
                    $"Changing to **Canary-English** through Skype Translator...wait a moment...ok! Try now!", "en");
            }
            if (m.Contains("banana"))
            {
                myState.UnknownMessageCount = 0;
                return message.CreateReplyMessage(
                        !myState.CanaryMode ? "Platano" : "Platano with 'pintitas', the best for humans", "en");
            }

            if (m.Contains("bye") || m.Contains("adios"))
            {
                myState = new MyState();
                var r = message.CreateReplyMessage();
                r.Attachments = new List<Attachment>
                {
                    new Attachment()
                    {
                        ContentUrl = "http://storage.intelequia.com/images/terminator1.jpg",
                        ContentType = "image/jpeg",
                        Text = "I'll be back!",
                    }
                };
                return r;
            }

            if (m.Contains("begin"))
            {
                myState.UnknownMessageCount = 0;
                myState.User = "";
                myState.TwoFactorAuthCode = "";
                myState.WaitingForPageTitle = false;
                myState.TabId = 0;
                return message.CreateReplyMessage($"Sure, what is your name?", "en");
            }

            if (m.Contains("david") && m.Contains("rodriguez") || m.Contains("rodríguez"))
            {
                myState.UnknownMessageCount = 0;
                myState.User = "David";
                myState.TwoFactorAuthCode = (new Random()).Next(100000, 999999).ToString();
                var twilio = new TwilioRestClient(ConfigurationManager.AppSettings["Twilio.AccountSid"], ConfigurationManager.AppSettings["Twilio.AuthToken"]);
                var sms = twilio.SendMessage(ConfigurationManager.AppSettings["Twilio.From"],
                    ConfigurationManager.AppSettings["Twilio.To"],
                    $"DnnBot sent you the code {myState.TwoFactorAuthCode} to verify your identity");
                return
                    message.CreateReplyMessage(
                        $"ok {myState.User}, I have you in my contact list. I've just sent to your mobile phone a code to verify your identity, can you give it to me?", "en");
            }

            if (m.Contains("news"))
            {
                myState.UnknownMessageCount = 0;
                if (!myState.IsValidIdentity)
                {
                    return UnauthorizedMessage(message, myState);
                }
                return
                    message.CreateReplyMessage(
                        $"Since last time we talked, I have detected that your website database **dnnfileservice** has an index fragmentation higher than 30%. **Do you want me to index the database for you?**", "en");
            }

            if (m == "sí" || m == "si" || m == "yes")
            {
                myState.UnknownMessageCount = 0;
                return message.CreateReplyMessage($"That's not the way (hint: be more polite)", "en");
            }

            if (m.Contains("please"))
            {
                myState.UnknownMessageCount = 0;
                if (!myState.IsValidIdentity)
                {
                    return UnauthorizedMessage(message, myState);
                }
                // TODO Call the automation webhook here
                CallSqlDatabaseReindexWebhook();
                return message.CreateReplyMessage($"Starting automatic reindexing on database **dnnfileservice**. I will send an e-mail to you when finishing.", "en");
            }


            /* CREATING A DNN PAGE */
            if (m.Contains("create") && m.Contains("page"))
            {
                myState.UnknownMessageCount = 0;
                if (!myState.IsValidIdentity)
                {
                    return UnauthorizedMessage(message, myState);
                }
                myState.WaitingForPageTitle = true;
                return
                    message.CreateReplyMessage(
                        $"Sure, can you give me the page title?", "en");
            }

            if (myState.WaitingForPageTitle)
            {
                myState.WaitingForPageTitle = false;
                myState.UnknownMessageCount = 0;
                if (!myState.IsValidIdentity)
                {
                    return UnauthorizedMessage(message, myState);
                }
                // TODO create DNN page with title m;                
                var tabId = TabController.Instance.AddTabAfter(new TabInfo()
                {
                    PortalID = 0,
                    TabName = m
                }, TabController.Instance.GetTabsByPortal(0).LastOrDefault().Value.TabID);
                myState.TabId = tabId;
                var tab = TabController.Instance.GetTab(tabId, 0);                
                return
                    message.CreateReplyMessage(
                        $"I have created the page. It's available at {tab.FullUrl}", "en");
            }


            /* END OF CREATING A DNN PAGE */


            /* ADDING AN IMAGE */
            if (m.Contains("picture"))
            {
                myState.UnknownMessageCount = 0;
                if (!myState.IsValidIdentity)
                {
                    return UnauthorizedMessage(message, myState);
                }
                if (myState.TabId == 0)
                {
                    return message.CreateReplyMessage(
                        $"You would like to create a page first, right?", "en");
                }
                return
                    message.CreateReplyMessage(
                        $"Yes, of course. Can you send me the picture?", "en");
            }

            if (message.Attachments.Count > 0)
            {
                if (myState.TabId == 0)
                {
                    return message.CreateReplyMessage(
                        $"You would like to create a page first, right?", "en");
                }
                var tab = TabController.Instance.GetTab(myState.TabId, 0);
                if (tab == null)
                    return
                        message.CreateReplyMessage(
                            $"Seems the page no longer exist. Did you delete it?", "en");

                try
                {

                    var fileUri = new Uri(message.Attachments[0].ContentUrl);
                    var filename = message.Attachments[0].ContentUrl.Contains("?") 
                        ? fileUri.Query.Substring(1).Split('&').FirstOrDefault(x => x.StartsWith("file=")).Split('=')[1]
                        : message.Attachments[0].Title;
                    var picUrl = DownloadAttachment(filename, message.Attachments[0].ContentUrl,
                        message.Attachments[0].ContentType);
                    AddPictureToPage(myState.TabId, picUrl);
                    return
                            message.CreateReplyMessage(
                            $"Ok, I have the added the image {filename} to the page. Do you want anything else?", "en");
                }
                catch (Exception ex)
                {
                    return
                        message.CreateReplyMessage($"{ex}");

                }

            }

            /* END OF ADDING AN IMAGE */

            if (m.Contains("thanks"))
            {
                myState.UnknownMessageCount = 0;
                return message.CreateReplyMessage($"¡Machango!", "en");
            }


            return null;
        }

        private void CallSqlDatabaseReindexWebhook()
        {
            var request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["Webhook.Url"]);
            request.Accept = "application/json";
            request.ContentType = "application/json";
            request.Method = "POST";
            var encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes("");
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(bytes, 0, bytes.Length);
            }
            using (var response = request.GetResponse())
            {
                using (var stream = response.GetResponseStream())
                {
                    // TODO Do something with the result
                }
            }
        }

        private Message UnauthorizedMessage(Message message, MyState myState)
        {
            myState.IsValidIdentity = false;
            return message.CreateReplyMessage($"I have to know who you are and validate your identity before doing that", "en");
        }

        private Message ValidateTwoFactorAuthCode(Message message, MyState myState)
        {
            myState.IsValidIdentity = false;
            if (message.Text.Trim() == myState.TwoFactorAuthCode ||
                message.Text.Trim() == ConfigurationManager.AppSettings["TwoFactorAuthMasterKey"])
            {
                myState.TwoFactorAuthCode = "";
                myState.IsValidIdentity = true;
                return
                    message.CreateReplyMessage(
                        $"Welcome back {myState.User}, oh, great almighty Lord and Master of DNN and the Universe. Those who are going to work greet you.", "en");
            }
            myState.TwoFactorAuthCode = "";
            return
                message.CreateReplyMessage(
                    $"I'm sorry, but that is not the correct code. Are you cheating?", "en");
        }

        private Message GetUnknownReply(Message message, MyState myState)
        {
            Message replyMessage;
            switch (myState.UnknownMessageCount)
            {
                case 0:
                    myState.UnknownMessageCount++;
                    replyMessage = message.CreateReplyMessage($"I don't understand you", "en");
                    break;
                case 1:
                    myState.UnknownMessageCount++;
                    replyMessage = message.CreateReplyMessage(
                        $"I still don't undestand you, perhaps is that canary accent when typing", "en");
                    break;
                default:
                    myState.UnknownMessageCount = 0;
                    replyMessage = message.CreateReplyMessage($"Mmmmm...¿42?", "en");
                    break;
            }
            return replyMessage;
        }

        private string DownloadAttachment(string filename, string url, string contentType)
        {
            var folder = DotNetNuke.Services.FileSystem.FolderManager.Instance.GetFolder(0, "Images/");
            //Create a WebRequest to get the file
            var fileReq = (HttpWebRequest)HttpWebRequest.Create(url);
            //Create a response for this request
            var fileResp = (HttpWebResponse)fileReq.GetResponse();
            var file = DotNetNuke.Services.FileSystem.FileManager.Instance.AddFile(folder, filename,
                fileResp.GetResponseStream(), true, false, contentType);
            return file.RelativePath;
        }

        private void AddPictureToPage(int tabId, string imageUrl)
        {
            var moduleDef = ModuleDefinitionController.GetModuleDefinitionByFriendlyName("Text/HTML");
            var tab = TabController.Instance.GetTab(tabId, 0);
            var objModule = new ModuleInfo();
            objModule.Initialize(0);
            objModule.PortalID = 0;
            objModule.TabID = tab.TabID;
            objModule.ModuleOrder = 0;
            objModule.ModuleTitle = tab.Title;
            objModule.PaneName = "ContentPane";
            objModule.ModuleDefID = moduleDef.ModuleDefID; // Text/HTML
            objModule.CacheTime = 1200;
            ModuleController.Instance.InitialModulePermission(objModule, objModule.TabID, 0);
            objModule.CultureCode = Null.NullString;
            objModule.AllTabs = false;
            objModule.Alignment = "";
            var moduleId = ModuleController.Instance.AddModule(objModule);


            //creating the content object, and adding the content to the module
            var htmlTextController = new HtmlTextController();
            var workflowStateController = new WorkflowStateController();

            int workflowId = htmlTextController.GetWorkflow(moduleId, tabId, 0).Value;

            HtmlTextInfo htmlContent = htmlTextController.GetTopHtmlText(moduleId, false, workflowId);
            if (htmlContent == null)
            {
                htmlContent = new HtmlTextInfo();
                htmlContent.ItemID = -1;
                htmlContent.StateID = workflowStateController.GetFirstWorkflowStateID(workflowId);
                htmlContent.WorkflowID = workflowId;
                htmlContent.ModuleID = moduleId;
                htmlContent.IsPublished = true;
                htmlContent.Approved = true;
                htmlContent.IsActive = true;
            }

            htmlContent.Content = $"<img src='/Portals/0/{imageUrl}' />";

            int draftStateId = workflowStateController.GetFirstWorkflowStateID(workflowId);
            int nextWorkflowStateId = workflowStateController.GetNextWorkflowStateID(workflowId, htmlContent.StateID);
            int publishedStateId = workflowStateController.GetLastWorkflowStateID(workflowId);

            htmlTextController.UpdateHtmlText(htmlContent, htmlTextController.GetMaximumVersionHistory(0));
        }
    }
}

