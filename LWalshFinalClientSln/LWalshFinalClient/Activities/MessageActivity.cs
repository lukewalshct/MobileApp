using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using LWalshFinalClient.Data_Models;
using LWalshFinalClient.Resources;
using Microsoft.Azure.Search;
using System.Collections.ObjectModel;
using Microsoft.Azure.Search.Models;

namespace LWalshFinalClient
{

    [Activity(Label = "Messages/Notifications")]
    public class MessageActivity : Activity
    {
        MobileServiceClient client;
        Button homeButton;
        Button votesButton;
        Button householdInfoButton;
        Button sendButton;
        Button searchButton;
        Button resetButton;
        EditText searchEditText;
        ListView messagesListView;
        MultiAutoCompleteTextView messageEditText;
        List<LWalshFinalClient.Data_Models.Message> messages;        
        public string currentUserID { get; set; }
        public string currentHHID { get; set; }
        //search
        private const string SearchServiceName = "lwalshfinalsearch";
        private const string SearchIndexName = "messageindex";
        private const string SearchServiceQueryAPIKey = "2FC1A592EC69B5FD0AFF52B1B2FAF9E2";

        private SearchServiceClient searchServiceClient;
        private SearchIndexClient searchIndexClient;

        private ObservableCollection<LWalshFinalClient.Data_Models.Message> searchResultsCollection = 
            new ObservableCollection<LWalshFinalClient.Data_Models.Message>();

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            
            this.searchServiceClient = new SearchServiceClient(SearchServiceName, new SearchCredentials(SearchServiceQueryAPIKey));

            this.searchIndexClient = this.searchServiceClient.Indexes.GetClient(SearchIndexName);

            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Message);

            // Get our button from the layout resource,
            // and attach an event to it
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);
            this.votesButton = FindViewById<Button>(Resource.Id.votesButton);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButton);
            this.sendButton = FindViewById<Button>(Resource.Id.sendButton);
            this.messageEditText = FindViewById<MultiAutoCompleteTextView>(Resource.Id.messageEntryText);
            this.messagesListView = FindViewById<ListView>(Resource.Id.messagesListView);
            this.searchButton = FindViewById<Button>(Resource.Id.searchButton);
            this.resetButton = FindViewById<Button>(Resource.Id.resetButton);
            this.searchEditText = FindViewById<EditText>(Resource.Id.searchEditText);

            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;
            this.sendButton.Click += sendClick;
            this.searchButton.Click += searchClick;
            this.resetButton.Click += searchClick;

            getIntentParameters();
            updateDisplay(false);            
        }

        private void getIntentParameters()
        {
            if (this.Intent.Extras != null)
            {
                this.currentUserID = this.Intent.Extras.GetString("currentUserID");
                this.currentHHID = this.Intent.Extras.GetString("currentHHID");
                //recreate the authenticated user and add it to the client
                string clientUserID = this.Intent.Extras.GetString("clientUserID");
                this.client.CurrentUser = new MobileServiceUser(clientUserID);
                this.client.CurrentUser.MobileServiceAuthenticationToken = this.Intent.Extras.GetString("clientAuthToken");
            }
        }

        private async void updateDisplay(bool isSearch)
        {
            if (!isSearch)
            {
                await getHouseholdMessages();
            }
            displayMessages();
            this.messageEditText.Text = "";
        }

        private async void searchClick(Object sender, EventArgs e)
        {
            if (sender == this.searchButton)
            {
                await SearchDocuments();
            }
            else
            {
                this.searchEditText.Text = "";
                updateDisplay(false);
            }
        }

        private async void sendClick(Object sender, EventArgs e)
        {
            bool submitSuccess = await submitMessage();
            if (submitSuccess)
            {
                this.searchEditText.Text = "";     
                updateDisplay(false);
            }
        }

        private async Task<bool> submitMessage()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            string messageText = this.messageEditText.Text;

            //check to make sure we have the necessary info
            if (messageText == "")
            {
                message = "Please enter a message to send.";
            }
            else
            {
                try
                {
                    LWalshFinalClient.Data_Models.Message newMessage = new LWalshFinalClient.Data_Models.Message();

                    newMessage.hhid = this.currentHHID;
                    newMessage.userid = this.currentUserID;
                    newMessage.message = this.messageEditText.Text;
                    newMessage.timeStamp = DateTime.Now.ToLocalTime().ToString();
                    
                    JToken payload = JObject.FromObject(newMessage);
                    JToken result = await this.client.InvokeApiAsync("message", payload);

                    if (result.HasValues)
                    {
                        this.messageEditText.Text = "";
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    builder.SetMessage(message);
                    builder.Create().Show();
                    return false;
                }
                builder.SetMessage(message);
                return true;
            }
            builder.SetMessage(message);
            builder.Create().Show();
            return false;
        }
        private async Task<bool> getHouseholdMessages()
        {
            string errorMessage = "";
            try
            {
                JToken result = await this.client.InvokeApiAsync("message?id=" + this.currentHHID, HttpMethod.Get, null);

                if (result.HasValues)
                {
                    //parse the household info and list of members
                    this.messages = new List<LWalshFinalClient.Data_Models.Message>();

                    //parse votes
                    JArray messagesJArray = (JArray)result;
                    foreach (var m in messagesJArray)
                    {
                        LWalshFinalClient.Data_Models.Message newMessage = new LWalshFinalClient.Data_Models.Message();

                        newMessage.message = (string)m["message"];
                        newMessage.memberName = (string)m["memberName"];
                        newMessage.timeStamp = (string)m["timeStamp"];

                        this.messages.Add(newMessage);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            if (errorMessage != "")
            {
                builder.SetMessage(errorMessage);
                builder.Create().Show();
            }
            return false;
        }

        private void displayMessages()        
        {
            if (this.messages != null && this.messages.Count > 0)
            {
                try
                {
                    List<MessageListItem> messageListItems = this.messages.Select(x =>
                        new MessageListItem
                        {
                            message = x.message,
                            sender = x.memberName,
                            timestamp = x.timeStamp
                        }).ToList();

                    MessageScrollAdapter messagesAdapter = new MessageScrollAdapter(this, messageListItems);
                    this.messagesListView.Adapter = messagesAdapter;
                }
                catch (Exception ex)
                {
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetMessage(ex.Message);
                    builder.Create().Show();
                }
            }
            else
            {
                this.messagesListView.Adapter = null;
            }
        }
        
        private void navigationClick(Object sender, EventArgs e)
        {
            Type activityType = null;

            if (sender == this.homeButton)
            {
                activityType = typeof(MainActivity);
            }
            else if (sender == this.votesButton)
            {
                activityType = typeof(VoteActivity);
            }
            else if (sender == this.householdInfoButton)
            {
                activityType = typeof(HouseholdActiviy);
            }

            Intent newActivity = new Intent(this, activityType);
            var bundle = new Bundle();
            bundle.PutString("isLoggedIn", "true");
            bundle.PutString("currentUserID", this.currentUserID);
            bundle.PutString("currentHHID", this.currentHHID);
            //serialize the mobilserivce client so user data stays intact
            //var clientJson = new JavaScriptSerializer().Serialize(this.client);
            //bundle.PutString("client", clientJson);
            bundle.PutString("clientUserID", this.client.CurrentUser.UserId);
            bundle.PutString("clientAuthToken", this.client.CurrentUser.MobileServiceAuthenticationToken);
            newActivity.PutExtras(bundle);

            //newActivity.PutExtra("MyData", "Data from Activity1");
            StartActivity(newActivity);
        }

        private async Task SearchDocuments()
        {
            this.messages = null;
            this.messages = new List<LWalshFinalClient.Data_Models.Message>();
            string searchText = this.searchEditText.Text;

            if (searchText != "")
            {
                try
                {
                    // Search the azure index
                    DocumentSearchResult<LWalshFinalClient.Data_Models.Message> response =
                        await this.searchIndexClient.Documents.SearchAsync<LWalshFinalClient.Data_Models.Message>(searchText);

                    // Update results
                    foreach (SearchResult<LWalshFinalClient.Data_Models.Message> result in response.Results)
                    {
                        if (result.Document.hhid == this.currentHHID)
                        {
                            this.messages.Add(result.Document);
                        }
                    }
                    updateDisplay(true);
                }
                catch
                {
                    updateDisplay(false);
                }
            }
            else
            {
                updateDisplay(false);
            }
        }
    }
}