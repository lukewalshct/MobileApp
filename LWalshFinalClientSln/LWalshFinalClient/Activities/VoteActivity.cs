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
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using LWalshFinalClient.Data_Models;
using LWalshFinalClient.Resources;

namespace LWalshFinalClient
{
    [Activity(Label = "Proposals Under Vote")]
    public class VoteActivity : Activity
    {
        MobileServiceClient client;

        Button homeButton;
        Button messagesButton;
        Button householdInfoButton;
        Button proposeVoteButton;
        Button submitButton;
        Button cancelButton;
        Spinner memberSpinner;
        EditText balanceChangeEditText;
        EditText descriptionTextEditText;
        TableLayout proposeTableLayout;
        LinearLayout submitButtonsLayout;
        CheckBox proposeAnonCheckBox;
        public string currentUserID;
        public string currentHHID;
        TextView memberNameTextView;
        TextView voteTypeTextView;
        bool isProposingVote;
        Household currentHousehold;
        List<HouseholdMember> members;
        List<Vote> householdVotes;
        HouseholdMember currentMember;
        ListView voteListView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());

            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Vote);

            // Create your application here
            this.homeButton = FindViewById<Button>(Resource.Id.homeButtonVote);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButtonVote);
            this.messagesButton = FindViewById<Button>(Resource.Id.messagesButtonVote);
            this.memberNameTextView = FindViewById<TextView>(Resource.Id.memberName);
            this.voteTypeTextView = FindViewById<TextView>(Resource.Id.voteType);
            this.proposeVoteButton = FindViewById<Button>(Resource.Id.proposalButton);
            this.submitButton = FindViewById<Button>(Resource.Id.submitButton);
            this.cancelButton = FindViewById<Button>(Resource.Id.cancelButton);
            this.memberSpinner = FindViewById<Spinner>(Resource.Id.memberSpinner);
            this.balanceChangeEditText = FindViewById<EditText>(Resource.Id.balanceChangeEditText);
            this.descriptionTextEditText = FindViewById<EditText>(Resource.Id.descriptionEditText);
            this.proposeTableLayout = FindViewById<TableLayout>(Resource.Id.proposeVoteTable);
            this.submitButtonsLayout = FindViewById<LinearLayout>(Resource.Id.submitLayout);
            this.proposeAnonCheckBox = FindViewById<CheckBox>(Resource.Id.anonCheckBox);
            this.voteListView = FindViewById<ListView>(Resource.Id.voteListView);

            this.homeButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;
            this.messagesButton.Click += navigationClick;
            this.proposeVoteButton.Click += proposeVoteClick;
            this.cancelButton.Click += cancelClick;
            this.submitButton.Click += submitClick;

            this.memberNameTextView.Visibility = ViewStates.Invisible;
            this.voteTypeTextView.Visibility = ViewStates.Invisible;
            this.isProposingVote = false;

            getIntentParameters();
            updateDisplay();
        }

        private async void updateDisplay()
        {
            this.proposeTableLayout.Visibility = ViewStates.Gone;
            this.submitButtonsLayout.Visibility = ViewStates.Gone;
            this.proposeAnonCheckBox.Visibility = ViewStates.Gone;

            //refresh household and members list
            await getHousehold();
            await getCurrentMember();
            await getHouseholdVotes();
            setMemberSpinnerDropdown();
            displayVotes();

            this.proposeTableLayout.Visibility = this.isProposingVote ? ViewStates.Visible : ViewStates.Gone;
            this.submitButtonsLayout.Visibility = this.isProposingVote ? ViewStates.Visible : ViewStates.Gone;
            this.proposeAnonCheckBox.Visibility = this.isProposingVote ? ViewStates.Visible : ViewStates.Gone;
        }

        private void getIntentParameters()
        {
            if (this.Intent.Extras != null)
            {
                this.currentUserID = this.Intent.Extras.GetString("currentUserID");
                this.currentHHID = this.Intent.Extras.GetString("currentHHID");
                //if the passed client is non-null, use the passed client
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

        private void proposeVoteClick(Object sender, EventArgs e)
        {
            this.isProposingVote = true;
            updateDisplay();
        }

        private async void submitClick(Object sender, EventArgs e)
        {            
            bool submitSuccess = await submitVote();
            if (submitSuccess)
            {
                this.isProposingVote = false;
                updateDisplay();
            }
        }

        private async Task<bool> submitVote()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            string targetMember = this.memberSpinner.SelectedItem.ToString();
            string balanceChange = this.balanceChangeEditText.Text;
            string description = this.descriptionTextEditText.Text;
            bool isAnonymous = this.proposeAnonCheckBox.Checked;
            
            //check to make sure we have the necessary info
            if (targetMember == null || targetMember == "")
            {
                message = "Please select a member to submit the vote";
            }
            else if (balanceChange == null || balanceChange == "" || balanceChange == "-")
            {
                message = "Please enter a valid amount for the proposed balance change";
            }
            else
            {
                try
                {
                    Vote vote = new Vote();

                    vote.targetMemberID = this.members.Where(x => x.firstName == targetMember).Single().userId;
                    vote.balanceChange = int.Parse(balanceChange);
                    vote.description = description;
                    vote.householdID = this.currentHHID;
                    vote.isAnonymous = isAnonymous;
                    vote.voteType = VoteType.Karma;
                    
                    JToken payload = JObject.FromObject(vote);
                    JToken result = await this.client.InvokeApiAsync("vote/newvote", payload);

                    if (result.HasValues)
                    {
                        message = "Successfully submitted proposal! The vote will now appear in the household's" +
                            "vote list to all household members.";
                        builder.SetMessage(message);
                        builder.Create().Show();
                        //reset fields for next vote
                        this.balanceChangeEditText.Text = "";
                        this.descriptionTextEditText.Text = "";
                        this.proposeAnonCheckBox.Checked = false;
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
                builder.SetMessage(message);
                return true;
            }            
            builder.SetMessage(message);
            builder.Create().Show();
            return false;
        }

        private void cancelClick(Object sender, EventArgs e)
        {
            this.isProposingVote = false;
            updateDisplay();
        }

        private void navigationClick(Object sender, EventArgs e)
        {
            Type activityType = null;

            if (sender == this.homeButton)
            {
                activityType = typeof(MainActivity);
            }
            else if (sender == this.householdInfoButton)
            {
                activityType = typeof(HouseholdActiviy);
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

                    this.currentHousehold.name = (string)((JObject)result)["name"];
                    this.currentHousehold.description = (string)((JObject)result)["description"];
                    this.currentHousehold.currencyName = (string)((JObject)result)["currencyName"];
                    this.currentHousehold.landlordName = (string)((JObject)result)["landlordName"];

                    //parse members
                    JArray membersJArray = (JArray)((JObject)result)["members"];

                    this.members = new List<HouseholdMember>();

                    foreach (var m in membersJArray)
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

        private async Task<bool> getHouseholdVotes()
        {
            string errorMessage = "";
            try
            {
                JToken result = await this.client.InvokeApiAsync("vote/byhhid/" + this.currentHHID, HttpMethod.Get, null);

                if (result.HasValues)
                {
                    //parse the household info and list of members
                    this.householdVotes = new List<Vote>();

                    //parse votes
                    JArray votesJArray = (JArray)result;
                    foreach (var v in votesJArray)
                    {
                        Vote newVote = new Vote();

                        newVote.balanceChange =(int)v["balanceChange"];
                        string voteType = (string)v["voteType"];
                        if (voteType != null && voteType != "")
                        {                        
                            switch (voteType)
                            {
                                case "Karma":
                                    newVote.voteType = VoteType.Karma;
                                    break;
                                case "Landlord":
                                    newVote.voteType = VoteType.Landlord;
                                    break;
                                case "NewMember":
                                    newVote.voteType = VoteType.NewMember;
                                    break;
                                case "EvictMember":
                                    newVote.voteType = VoteType.EvictMember;
                                    break;
                            }                            
                        }                        
                        newVote.targetMemberID = (string)v["targetMemberID"];
                        newVote.isAnonymous = (bool)v["isAnonymous"];
                        newVote.description = (string)v["description"];
                        newVote.votesFor = (int)v["votesFor"];
                        newVote.votesAgainst = (int)v["votesAgainst"];
                        newVote.voteStatus = (string)v["voteStatus"];
                        newVote.votesNeeded = (int)v["votesNeeded"];
                        newVote.targetMemberName = (string)v["targetMemberName"];

                        this.householdVotes.Add(newVote);
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

        private void displayVotes()
        {
            if (this.householdVotes != null && this.householdVotes.Count > 0)
            {
                try
                {
                    List<VoteListItem> voteListItems = this.householdVotes.Select(x =>
                        new VoteListItem
                        {
                            targetMember = x.targetMemberName,
                            voteType = x.voteType.ToString(),
                            balanceChange = x.balanceChange,
                            description = x.description,
                            statusText = x.votesFor + " votes for, " + x.votesAgainst + " votes against (" + x.votesNeeded + ")"
                        }).ToList();
                    VoteScrollAdapter votesAdapter = new VoteScrollAdapter(this, voteListItems);
                    this.voteListView.Adapter = votesAdapter;
                }
                catch (Exception ex)
                {
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetMessage(ex.Message);
                    builder.Create().Show();
                }
            }
        }
        private void setMemberSpinnerDropdown()
        {
            string[] memberNames = this.members.Select(x => x.firstName).ToArray();
            var spinnerAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, memberNames);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            this.memberSpinner.Adapter = spinnerAdapter;
        }
    }
}