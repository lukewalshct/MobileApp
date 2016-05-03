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
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using LWalshFinalClient.Resources;
using Android.Graphics;
using LWalshFinalClient.Data_Models;

namespace LWalshFinalClient
{
    [Activity(Label = "Household", MainLauncher = false, Icon = "@drawable/icon")]
    class HouseholdActiviy : Activity
    {
        public MobileServiceClient client;

        Button homeButton;
        Button votesButton;
        Button messagesButton;
        Button cancelEditButton;
        Button saveEditButton;
        Button joinButton;
        string currentUserID;
        string currentHHID;
        Household currentHousehold;
        List<HouseholdMember> members;
        HouseholdMember currentMember;
        EditText hhNameEditText;
        EditText hhDescriptionEditText;
        EditText hhCurrencyEditText;
        TextView hhLandlordTextView;
        ListView membersListView;
        LinearLayout landlordTitleLayout;
        LinearLayout userTitleLayout;
        bool isMember;
        bool isEditInfo;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            //this.client = new MobileServiceClient("http://localhost:50103/");

            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Household);

            // Get our button from the layout resource,
            // and attach an event to it
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);
            this.votesButton = FindViewById<Button>(Resource.Id.votesButton);
            this.messagesButton = FindViewById<Button>(Resource.Id.messagesButton);
            this.hhNameEditText = FindViewById<EditText>(Resource.Id.hhNameEditText);
            this.hhDescriptionEditText = FindViewById <EditText>(Resource.Id.hhDescEditText);
            this.hhCurrencyEditText = FindViewById<EditText>(Resource.Id.hhCurEditText);
            this.hhLandlordTextView = FindViewById<TextView>(Resource.Id.hhLandlordTextView);
            this.membersListView = FindViewById<ListView>(Resource.Id.memberListView);
            this.cancelEditButton = FindViewById<Button>(Resource.Id.cancelEditButton);
            this.saveEditButton = FindViewById<Button>(Resource.Id.editSaveButton);
            this.landlordTitleLayout = FindViewById<LinearLayout>(Resource.Id.landlordTitleLayout);
            this.userTitleLayout = FindViewById<LinearLayout>(Resource.Id.userTitleLayout);
            this.joinButton = FindViewById<Button>(Resource.Id.joinButton);

            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.messagesButton.Click += navigationClick;
            this.saveEditButton.Click += saveEditClick;
            this.cancelEditButton.Click += cancelEditClick;
            this.joinButton.Click += joinClick;
            this.isMember = false;
            this.isEditInfo = false;
            //if the activity was instantiated by an intent with parameters, get the parameters
            //and initialize the appropriate class variables
            getIntentParameters();
            updateDisplay();
        }

        private async void updateDisplay()
        {
            this.joinButton.Visibility = ViewStates.Gone;
            await getHousehold();
            this.isMember = await getCurrentMember();
            if (this.currentHousehold != null)
            {
                this.hhNameEditText.Text = this.currentHousehold.name;
                this.hhDescriptionEditText.Text = this.currentHousehold.description;
                this.hhLandlordTextView.Text = this.currentHousehold.landlordName;
                this.hhCurrencyEditText.Text = this.currentHousehold.currencyName;
            }
            else
            {
                this.hhNameEditText.Text = "";
                this.hhDescriptionEditText.Text = "";
                this.hhLandlordTextView.Text = "";
                this.hhCurrencyEditText.Text = "";
            }
            displayMembers();
            if (this.isMember)
            {
                this.votesButton.Clickable = true;
                this.messagesButton.Clickable = true;
                this.votesButton.Enabled = true;
                this.messagesButton.Enabled = true;                                          
            }
            else
            {
                this.votesButton.Clickable = false;
                this.messagesButton.Clickable = false;
                this.votesButton.Enabled = false;
                this.messagesButton.Enabled = false;
            }
            if (this.isMember && this.currentMember != null && this.currentMember.isLandlord)
            {
                this.landlordTitleLayout.Visibility = ViewStates.Visible;
                this.userTitleLayout.Visibility = ViewStates.Gone;
                this.saveEditButton.Clickable = true;
                this.cancelEditButton.Clickable = true;                                
            }
            else
            {
                this.landlordTitleLayout.Visibility = ViewStates.Gone;
                this.userTitleLayout.Visibility = ViewStates.Visible;
                this.saveEditButton.Clickable = false;
                this.cancelEditButton.Clickable = false;
            }
            if (this.isEditInfo)
            {
                this.hhNameEditText.Enabled = true;
                this.hhCurrencyEditText.Enabled = true;
                this.hhDescriptionEditText.Enabled = true;
            }
            else
            {
                this.hhNameEditText.Enabled = false;                
                this.hhCurrencyEditText.Enabled = false;
                this.hhDescriptionEditText.Enabled = false;
                this.hhNameEditText.SetTextColor(Color.White);
                this.hhCurrencyEditText.SetTextColor(Color.White);
                this.hhDescriptionEditText.SetTextColor(Color.White);
            }
            this.cancelEditButton.Visibility = this.isEditInfo ? ViewStates.Visible : ViewStates.Invisible;
            this.saveEditButton.Text = this.isEditInfo ? "Save" : "Edit Info";
            this.joinButton.Visibility = this.isMember ? ViewStates.Gone : ViewStates.Visible;
        }

        private async void joinClick(Object sender, EventArgs e)
        {
            await sendJoinRequest();            
        }

        private async Task<bool> sendJoinRequest()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            try
            {
                NewVote vote = new NewVote();

                vote.targetMemberID = this.currentUserID;
                vote.balanceChange = 0;
                vote.description = "Request to join household";
                vote.householdID = this.currentHHID;
                vote.isAnonymous = false;
                vote.voteType = VoteType.NewMember;

                JToken payload = JObject.FromObject(vote);
                JToken result = await this.client.InvokeApiAsync("vote/newvote", payload);

                if (result.HasValues)
                {
                    message = "Successfully submitted request to join the household! The request will now appear in the household's" +
                        "vote list to all household members. All current household members must approve your request in order to join.";
                    builder.SetMessage(message);
                    builder.Create().Show();
                    //reset fields for next vote
                    return true;
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                message = ex.Message;
                builder.SetMessage(message);
                builder.Create().Show();
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                builder.SetMessage(message);
                builder.Create().Show();
                return false;
            }
            if (message != "")
            {
                builder.SetMessage(message);
                builder.Create().Show();
            }
            return false;
        }

        private async void saveEditClick(Object sender, EventArgs e)
        {
            if (this.isEditInfo)
            {
                bool successfulUpdate = await updateHouseholdInfo();
                if (successfulUpdate)
                {
                    this.isEditInfo = false;
                }
            }
            else
            {
                this.isEditInfo = true;
            }
            updateDisplay();
        }

        private async Task<bool> updateHouseholdInfo()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);            
            try
            {
                Household hh = new Household();
                hh.id = this.currentHHID;
                hh.landlordIDP = this.currentUserID;
                hh.name = this.hhNameEditText.Text;
                hh.description = this.hhDescriptionEditText.Text;
                hh.currencyName = this.hhCurrencyEditText.Text;

                JToken payload = JObject.FromObject(hh);
                JToken result = await this.client.InvokeApiAsync("household/editinfo", payload);

                if (result.HasValues)
                {
                    message = "Successfully updated household info.";
                    builder.SetMessage(message);
                    builder.Create().Show();                    
                    return true;
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                message = ex.Message;
                builder.SetMessage(message);
                builder.Create().Show();
                return false;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                builder.SetMessage(message);
                builder.Create().Show();
                return false;
            }
            if (message != "")
            {
                builder.SetMessage(message);
                builder.Create().Show();
            }
            return false;

        }
        private void cancelEditClick(Object sender, EventArgs e)
        {
            this.isEditInfo = false;
            updateDisplay();
        }

        private async Task<bool> getHousehold()
        {
            string errorMessage = "";
            try
            {
                JToken result = await this.client.InvokeApiAsync("household/byid/" + this.currentHHID, HttpMethod.Get, null);

                if (result.HasValues)
                {
                    //parse the household info and list of members
                    this.currentHousehold = new Household();
                                        
                    this.currentHousehold.name =(string)((JObject)result)["name"];
                    this.currentHousehold.description = (string)((JObject)result)["description"];
                    this.currentHousehold.currencyName = (string)((JObject)result)["currencyName"];             
                    this.currentHousehold.landlordName = (string)((JObject)result)["landlordName"];

                    //parse members
                    JArray membersJArray = (JArray)((JObject)result)["members"];

                    this.members = new List<HouseholdMember>();

                    foreach(var m in membersJArray)
                    {
                        HouseholdMember member = new HouseholdMember();

                        member.firstName = (string)((JObject)m)["firstName"];
                        member.lastName = (string)((JObject)m)["lastName"];
                        member.status = (string)((JObject)m)["status"];
                        member.karma = (double)((JObject)m)["karma"];
                        member.isLandlord = (bool)((JObject)m)["isLandlord"];
                        member.isLandlordVote = (bool)((JObject)m)["isLandlordVote"];
                        member.isEvictVote = (bool)((JObject)m)["isEvictVote"];
                        member.isApproveVote = (bool)((JObject)m)["isApproveVote"];
                        member.userId = (string)((JObject)m)["userId"];

                        this.members.Add(member);
                    }
                    return true;
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                errorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(errorMessage);
            builder.Create().Show();
            return false;
        }      
    
        private async Task<bool> getCurrentMember()
        {
            string errorMessage = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            try
            {
                JToken m = await this.client.InvokeApiAsync("household/byid/" + this.currentHHID +
                    "/getmember/" + this.currentUserID.Substring(9), HttpMethod.Get, null);

                if (m.HasValues)
                {
                    this.currentMember = new HouseholdMember();

                    this.currentMember.firstName = (string)((JObject)m)["firstName"];
                    this.currentMember.lastName = (string)((JObject)m)["lastName"];
                    this.currentMember.status = (string)((JObject)m)["status"];
                    this.currentMember.karma = (double)((JObject)m)["karma"];
                    this.currentMember.isLandlord = (bool)((JObject)m)["isLandlord"];
                    this.currentMember.isLandlordVote = (bool)((JObject)m)["isLandlordVote"];
                    this.currentMember.isEvictVote = (bool)((JObject)m)["isEvictVote"];
                    this.currentMember.isApproveVote = (bool)((JObject)m)["isApproveVote"];
                    this.currentMember.userId = (string)((JObject)m)["userId"];

                    return true;
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                errorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            if (errorMessage != "")
            {
                builder.SetMessage(errorMessage);
                builder.Create().Show();
            }
            return false;
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
                //var clientJson = this.Intent.Extras.GetString("client");
                //if (clientJson != null)
                //{
                //    MobileServiceClient client = new JavaScriptSerializer().Deserialize<MobileServiceClient>(clientJson);
                //    this.client = client;
                //}
            }
        }

        private void displayMembers()
        {
            if (this.members != null && this.members.Count > 0)
            {        
                List<MemberListItem> memberListItems = this.members.Select(x =>
                    new MemberListItem { name = x.firstName, balance = 
                    this.currentHousehold.currencyName + ": " + x.karma.ToString() }).ToList();
                MemberScrollAdapter membersAdapter = new MemberScrollAdapter(this, memberListItems);
                this.membersListView.Adapter = membersAdapter;
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
            else if (sender == this.messagesButton)
            {
                activityType = typeof(MessageActivity);
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