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
    /// <summary>
    /// This class represents the message screen that displays all the messages for a household.
    /// Users can create/post messages to this forum and view messages posted by other users. 
    /// All messages on this screen are viewable by all household members, and the user has the
    /// ability to search/filter previous messages. The messages are stored in Azure Document 
    /// DB and the search ability utilizes Azure Search.
    /// </summary>
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
        
        //the properties below pertain to connecting to Azure search:
        //specify the Azure search name, index, and api key
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
            
            //set the MobileServiceClient
            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            
            //set the Azure Search client
            this.searchServiceClient = new SearchServiceClient(SearchServiceName, new SearchCredentials(SearchServiceQueryAPIKey));

            //set the Azure search index
            this.searchIndexClient = this.searchServiceClient.Indexes.GetClient(SearchIndexName);

            // Set our view from the "message" layout resource
            SetContentView(Resource.Layout.Message);

            //assign buttons, views, etc
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);
            this.votesButton = FindViewById<Button>(Resource.Id.votesButton);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButton);
            this.sendButton = FindViewById<Button>(Resource.Id.sendButton);
            this.messageEditText = FindViewById<MultiAutoCompleteTextView>(Resource.Id.messageEntryText);
            this.messagesListView = FindViewById<ListView>(Resource.Id.messagesListView);
            this.searchButton = FindViewById<Button>(Resource.Id.searchButton);
            this.resetButton = FindViewById<Button>(Resource.Id.resetButton);
            this.searchEditText = FindViewById<EditText>(Resource.Id.searchEditText);

            //assign button click event handles
            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;
            this.sendButton.Click += sendClick;
            this.searchButton.Click += searchClick;
            this.resetButton.Click += searchClick;

            //get the intent parameters and initialize the appropriate class variables
            //this is done so that when the user navigates to this screen from one of the
            //other screens, the user's authentication data is preserved so the screen can show
            //the relevant data
            getIntentParameters();
            //update display
            updateDisplay(false);            
        }

        /// <summary>
        /// A method that retrieves the parameters from the intent that created
        /// the activity. This ensures that the user authentication data is preserved.
        /// If these parameters exist, the MobileServicesClient is overwritten
        /// by a new client that uses the user's authentication data.
        /// </summary>
        private void getIntentParameters()
        {
            if (this.Intent.Extras != null)
            {
                this.currentUserID = this.Intent.Extras.GetString("currentUserID");
                this.currentHHID = this.Intent.Extras.GetString("currentHHID");
                //recreate the client using the user's authenticatino and overwrite current client
                string clientUserID = this.Intent.Extras.GetString("clientUserID");
                this.client.CurrentUser = new MobileServiceUser(clientUserID);
                this.client.CurrentUser.MobileServiceAuthenticationToken = this.Intent.Extras.GetString("clientAuthToken");
            }
        }

        /// <summary>
        /// Updates the display
        /// </summary>
        /// <param name="isSearch"></param>
        private async void updateDisplay(bool isSearch)
        {
            if (!isSearch)
            {
                await getHouseholdMessages();
            }
            displayMessages();
            this.messageEditText.Text = "";
        }

        /// <summary>
        /// Event handler that triggers when the "Search" button is clicked. This
        /// allows the user to search through the household's messages based on a 
        /// search term. The household that meet the search criteria are then
        /// displayed to the user. The messages are stored in Document DB and 
        /// Azure Search is used to implement this functionality. If the sender
        /// is the cancel button and not the search button, the fields and screen
        /// are reset.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private async void searchClick(Object sender, EventArgs e)
        {
            //if search button, search the documents
            if (sender == this.searchButton)
            {
                await SearchDocuments();
            }
            //else reset
            else
            {
                this.searchEditText.Text = "";
                updateDisplay(false);
            }
        }

        /// <summary>
        /// Event handler that's triggered when the "Send" button is clicked. This
        /// action makes a call on Azure's Message controller which then stores
        /// the message in Document DB using the message schema.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The sender</param>
        private async void sendClick(Object sender, EventArgs e)
        {
            //call helper method that submits the message
            bool submitSuccess = await submitMessage();
            if (submitSuccess)
            {
                this.searchEditText.Text = "";     
                updateDisplay(false);
            }
        }

        /// <summary>
        /// Helper method that makes a POST call to Azure's Message resource
        /// which stores the message text the user entered in Azure Document DB.
        /// </summary>
        /// <returns>Bool indicating success or failure</returns>
        private async Task<bool> submitMessage()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            string messageText = this.messageEditText.Text;

            //check to make sure there is text, else prompt the user to enter some
            if (messageText == "")
            {
                message = "Please enter a message to send.";
            }
            else
            {
                try
                {
                    //create a new message object
                    LWalshFinalClient.Data_Models.Message newMessage = new LWalshFinalClient.Data_Models.Message();

                    newMessage.hhid = this.currentHHID;
                    newMessage.userid = this.currentUserID;
                    newMessage.message = this.messageEditText.Text;
                    newMessage.timeStamp = DateTime.Now.ToLocalTime().ToString();
                    
                    //make the call to the Message resource
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

        /// <summary>
        /// Method that makes a GET request to Azure's Message controller to update the 
        /// screen with all the messages for the household.
        /// </summary>
        /// <returns>Bool indicating success or failure</returns>
        private async Task<bool> getHouseholdMessages()
        {
            string errorMessage = "";
            try
            {
                //make GET request to the message resource, passing the household id
                JToken result = await this.client.InvokeApiAsync("message?id=" + this.currentHHID, HttpMethod.Get, null);

                if (result.HasValues)
                {
                    
                    this.messages = new List<LWalshFinalClient.Data_Models.Message>();

                    //parse messages
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

        /// <summary>
        /// Displays the list of messages to the user as a scrollable list. This is accomplished by using
        /// an adapter (MessageScrollAdapter) which binds the data to the listview (messagesListView).
        /// </summary>
        private void displayMessages()        
        {
            //if there are messages, display them
            if (this.messages != null && this.messages.Count > 0)
            {
                try
                {
                    //create a list of message list items from the messages
                    List<MessageListItem> messageListItems = this.messages.Select(x =>
                        new MessageListItem
                        {
                            message = x.message,
                            sender = x.memberName,
                            timestamp = x.timeStamp
                        }).ToList();
                    //create the message adapter and assign it to the listview
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
            //else hid the messages list view
            else
            {
                this.messagesListView.Adapter = null;
            }
        }

        /// <summary>
        /// Event handler triggered by a user clicking on one of the navigation buttons on 
        /// the bottom of the screen. The user is then brought to the appropriate screen.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void navigationClick(Object sender, EventArgs e)
        {
            //the activityType represents the activity or "screen" the user 
            //will switch to
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

            //prepare for the new Activity (screen) to be launched
            //we want to preserve the data about the user and login status (user should be logged in)
            //when the new screen is created
            //create a new intent
            Intent newActivity = new Intent(this, activityType);
            //put the relevant data fields into the bundle as strings. These will be retrieved
            //by the new screen
            var bundle = new Bundle();
            bundle.PutString("isLoggedIn", "true");
            bundle.PutString("currentUserID", this.currentUserID);
            bundle.PutString("currentHHID", this.currentHHID);           
            bundle.PutString("clientUserID", this.client.CurrentUser.UserId);
            bundle.PutString("clientAuthToken", this.client.CurrentUser.MobileServiceAuthenticationToken);
            newActivity.PutExtras(bundle);

            //launch the new activity/screen
            StartActivity(newActivity);
        }

        /// <summary>
        /// Method that calls the Azure Search service to search the Azure Document DB for the
        /// relevant search term. Updates display with the search results.
        /// </summary>
        /// <returns>Task</returns>
        private async Task SearchDocuments()
        {
            this.messages = null;
            this.messages = new List<LWalshFinalClient.Data_Models.Message>();
            //user entered search text pulled from the screen
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
                        //only display results for this household
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