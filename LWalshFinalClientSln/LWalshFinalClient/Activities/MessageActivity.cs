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
        MultiAutoCompleteTextView messageEditText;
        public string currentUserID { get; set; }
        public string currentHHID { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            //this.client = new MobileServiceClient("http://localhost:50103/");

            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Message);

            // Get our button from the layout resource,
            // and attach an event to it
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);
            this.votesButton = FindViewById<Button>(Resource.Id.votesButton);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButton);
            this.sendButton = FindViewById<Button>(Resource.Id.sendButton);
            this.messageEditText = FindViewById<MultiAutoCompleteTextView>(Resource.Id.messageEntryText);

            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;
            this.sendButton.Click += sendClick;

            getIntentParameters();
            updateDisplay();
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

        private async void updateDisplay()
        {
            await getHouseholdMessages();
            displayMessages();
            this.messageEditText.Text = "";
        }

        private async void sendClick(Object sender, EventArgs e)
        {
            bool submitSuccess = await submitMessage();
            if (submitSuccess)
            {                
                updateDisplay();
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

            return false;
        }

        private void displayMessages()        
        {
            //if (this.householdVotes != null && this.householdVotes.Count > 0)
            //{
            //    try
            //    {
            //        List<VoteListItem> voteListItems = this.householdVotes.Select(x =>
            //            new VoteListItem
            //            {
            //                membersVotedIDs = x.membersVotedIDs,
            //                voteID = x.Id,
            //                targetMember = x.targetMemberName,
            //                voteType = x.voteType.ToString(),
            //                balanceChange = x.balanceChange,
            //                description = x.description,
            //                voteStatus = x.voteStatus,
            //                statusText = x.votesFor + " votes for, " + x.votesAgainst + " against (" + x.votesNeeded + " needed)"
            //            }).ToList();
            //        VoteScrollAdapter votesAdapter = new VoteScrollAdapter(this, voteListItems);
            //        this.voteListView.Adapter = votesAdapter;
            //    }
            //    catch (Exception ex)
            //    {
            //        AlertDialog.Builder builder = new AlertDialog.Builder(this);
            //        builder.SetMessage(ex.Message);
            //        builder.Create().Show();
            //    }
            //}
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
    }
}