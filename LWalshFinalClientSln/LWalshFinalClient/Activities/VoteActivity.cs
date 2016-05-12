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
using Newtonsoft.Json;

namespace LWalshFinalClient
{
    /// <summary>
    /// This class represents the voting screen that allows the user to view all the votes
    /// for the household. Information about each vote is displayed, including the type of vote,
    /// the target member, its status, and two voting buttons for the user to vote for or 
    /// against the proposed vote. Users are able to propose their a vote of their own and select
    /// a member (either themselves or another household member) and propose that that member
    /// have currency added or subtracted from their balance.
    /// </summary>
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
        bool isProposingVote;
        Household currentHousehold;
        List<HouseholdMember> members;
        List<Vote> householdVotes;
        public HouseholdMember currentMember;
        ListView voteListView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //declare the client
            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());

            // Set our view from the "Vote" layout resource
            SetContentView(Resource.Layout.Vote);

            //assign buttons, views, etc
            this.homeButton = FindViewById<Button>(Resource.Id.homeButtonVote);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButtonVote);
            this.messagesButton = FindViewById<Button>(Resource.Id.messagesButtonVote);
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

            //assign click event handlers
            this.homeButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;
            this.messagesButton.Click += navigationClick;
            this.proposeVoteButton.Click += proposeVoteClick;
            this.cancelButton.Click += cancelClick;
            this.submitButton.Click += submitClick;

            //indicates whether the user is editing a vote for proposal
            this.isProposingVote = false;

            //get the intent parameters and initialize the appropriate class variables
            //this is done so that when the user navigates to this screen from one of the
            //other screens, the user's authentication data is preserved so the screen can show
            //the relevant data
            getIntentParameters();
            //update the display
            updateDisplay();
        }

        /// <summary>
        /// Updates the display
        /// </summary>
        public async void updateDisplay()
        {
            //hide the menu that allows the user to propose a vote when the screen initializes
            this.proposeTableLayout.Visibility = ViewStates.Gone;
            this.submitButtonsLayout.Visibility = ViewStates.Gone;
            this.proposeAnonCheckBox.Visibility = ViewStates.Gone;

            //refresh household and members list
            await getHousehold();
            //get current member
            await getCurrentMember();
            //get all household votes
            await getHouseholdVotes();
            //get the members of the household to populate the dropdown menu
            setMemberSpinnerDropdown();
            displayVotes();

            //show the vote proposal fields if the user clicked to propose a new vote
            this.proposeTableLayout.Visibility = this.isProposingVote ? ViewStates.Visible : ViewStates.Gone;
            this.submitButtonsLayout.Visibility = this.isProposingVote ? ViewStates.Visible : ViewStates.Gone;
            this.proposeAnonCheckBox.Visibility = this.isProposingVote ? ViewStates.Visible : ViewStates.Gone;
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
        /// Event handler triggered when the user clicks the "Propose a New Vote!" Button.
        /// Sets the screen mode to "creating proposal" so that the fields to create a new
        /// proposal appear.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void proposeVoteClick(Object sender, EventArgs e)
        {
            this.isProposingVote = true;
            updateDisplay();
        }

        /// <summary>
        /// Event handler triggered when the user clicks the Submit button to submit their
        /// proposal. Calls a helper method makes a post call to Azure's Vote resource.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private async void submitClick(Object sender, EventArgs e)
        {            
            bool submitSuccess = await submitVote();
            //if a successful vote reset and update the screen
            if (submitSuccess)
            {
                this.isProposingVote = false;
                updateDisplay();
            }
        }

        /// <summary>
        /// Makes a POST call to the vote resource in Azure to 'submit' the user's
        /// proposed vote.
        /// </summary>
        /// <returns>Bool indicating success or failure</returns>
        private async Task<bool> submitVote()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);

            //retrieve info about the vote from the user's input
            string targetMemberName = this.memberSpinner.SelectedItem.ToString();
            string balanceChange = this.balanceChangeEditText.Text;
            string description = this.descriptionTextEditText.Text;
            bool isAnonymous = this.proposeAnonCheckBox.Checked;
            
            //check to make sure we have the necessary info
            if (targetMemberName == null || targetMemberName == "")
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
                    //create a new vote and add the relevant data
                    Vote vote = new Vote();
                    //get the target member
                    HouseholdMember targetMember = this.members.Where(x => x.firstName == targetMemberName).Single();
                                        
                    vote.targetMemberID = targetMember.userId;
                    vote.targetMemberName = targetMember.firstName;
                    vote.balanceChange = int.Parse(balanceChange);
                    vote.description = description;
                    vote.householdID = this.currentHHID;
                    vote.isAnonymous = isAnonymous;
                    vote.voteType = VoteType.Karma;
                    
                    //make POST call to the vote resource
                    JToken payload = JObject.FromObject(vote);
                    JToken result = await this.client.InvokeApiAsync("vote/newvote", payload);

                    //if success, display success message and reset the fields
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
                    message = await getMobileServiceExceptionMessage(ex);
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

        /// <summary>
        /// Event handler that triggers when the cancel button is clicked. Essentially
        /// cancels the proposed vote being created and resets the screen.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void cancelClick(Object sender, EventArgs e)
        {
            this.isProposingVote = false;
            //reset fields for next vote
            this.balanceChangeEditText.Text = "";
            this.descriptionTextEditText.Text = "";
            this.proposeAnonCheckBox.Checked = false;
            updateDisplay();
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
            else if (sender == this.householdInfoButton)
            {
                activityType = typeof(HouseholdActiviy);
            }
            else if (sender == this.messagesButton)
            {
                activityType = typeof(MessageActivity);
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
        /// Makes a GET request to the houshold resource and gets the household and list of members.
        /// </summary>
        /// <returns>Bool indicating success or failure</returns>
        private async Task<bool> getHousehold()
        {
            string errorMessage = "";
            try
            {
                //make GET request
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
                        member.Id = (string)((JObject)m)["id"];
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
                errorMessage = await getMobileServiceExceptionMessage(ex);
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

        /// <summary>
        /// Makes a call to the Household resource to try and get the current member.
        /// This is different from the User since sometimes the user may not be a 
        /// member of the household.
        /// </summary>
        /// <returns>Bool indicating success or failure</returns>
        private async Task<bool> getCurrentMember()
        {
            string errorMessage = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            try
            {
                //make request to the household resource using the household and user ids
                JToken m = await this.client.InvokeApiAsync("household/byid/" + this.currentHHID +
                    "/getmember/" + this.currentUserID.Substring(9), HttpMethod.Get, null);

                //if successful request, parse the result and update the current household member
                if (m.HasValues)
                {
                    this.currentMember = new HouseholdMember();
                    this.currentMember.Id = (string)((JObject)m)["id"];
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
                errorMessage = await getMobileServiceExceptionMessage(ex);
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

        /// <summary>
        /// Makes a GET request to the vote resource to get a list of the household votes.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> getHouseholdVotes()
        {
            string errorMessage = "";
            try
            {
                //make request and pass the current household id
                JToken result = await this.client.InvokeApiAsync("vote/byhhid/" + this.currentHHID, HttpMethod.Get, null);

                if (result.HasValues)
                {                    
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
                        newVote.targetMemberName = (string)v["targetMemberName"];
                        newVote.isAnonymous = (bool)v["isAnonymous"];
                        newVote.description = (string)v["description"];
                        newVote.votesFor = (int)v["votesFor"];
                        newVote.votesAgainst = (int)v["votesAgainst"];
                        newVote.voteStatus = (string)v["voteStatus"];
                        newVote.votesNeeded = (int)v["votesNeeded"];
                        newVote.Id = (string)v["id"];
                        newVote.voteStatus = (string)v["voteStatus"];

                        //parse members voted
                        JArray membersVoted = (JArray)v["membersVoted"];

                        foreach(var m in membersVoted)
                        {
                            string memberID = m.Value<string>();
                            newVote.membersVotedIDs.Add(memberID);
                        }
                        this.householdVotes.Add(newVote);
                    }
                    return true;
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                errorMessage = await getMobileServiceExceptionMessage(ex); errorMessage = ex.Message;
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
        /// Displays the list of household votes to the user as a scrollable list. This is accomplished by using
        /// an adapter (VoteScrollAdapter) which binds the data to the listview (voteListView).
        /// </summary>  
        private void displayVotes()
        {
            if (this.householdVotes != null && this.householdVotes.Count > 0)
            {
                try
                {
                    List<VoteListItem> voteListItems = this.householdVotes.Select(x =>
                        new VoteListItem
                        {
                            membersVotedIDs =  x.membersVotedIDs,
                            voteID = x.Id,
                            targetMember = x.targetMemberName,
                            voteType = x.voteType.ToString(),
                            balanceChange = x.balanceChange,
                            description = x.description,
                            voteStatus = x.voteStatus,
                            statusText = x.votesFor + " votes for, " + x.votesAgainst + " against (" + x.votesNeeded + " needed)"
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
        /// <summary>
        /// Helper method that uses the list of household members to populate the dropdown menu
        /// where the user can choose a member when proposing a vote.
        /// </summary>
        private void setMemberSpinnerDropdown()
        {
            string[] memberNames = this.members.Select(x => x.firstName).ToArray();
            var spinnerAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, memberNames);
            spinnerAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            this.memberSpinner.Adapter = spinnerAdapter;
        }
        
        /// <summary>
        /// Makes a call to the Vote resource and submits a user's vote (for or against) for the 
        /// selected vote. This method is actually called from the VoteScrollAdapter since the
        /// voting buttons are defined in that view.
        /// </summary>
        /// <param name="isYesVote">Indicates whether the vote is "for"</param>
        /// <param name="voteID">Vote id</param>
        /// <returns>Bool indicating success or failure</returns>
        public async Task<bool> sendVote(bool isYesVote, string voteID)
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            try
            {
                //create a new votecast
                VoteCast vote = new VoteCast();
                vote.voteID = voteID;
                string userVote = isYesVote ? "for" : "against";
                vote.vote = userVote;                            

                //make POST call to the vote resource
                JToken payload = JObject.FromObject(vote);
                JToken result = await this.client.InvokeApiAsync("vote/castvote", payload);

                //if successful, display success message            
                if (result.HasValues)
                {
                    message = "Successfully voted " + userVote + " the proposal!";
                    builder.SetMessage(message);
                    builder.Create().Show();                    
                    return true;
                }
            }
            catch (MobileServiceInvalidOperationException ex)
            {
                message = await getMobileServiceExceptionMessage(ex);
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

        /// <summary>
        /// Helper method that returns the message content from a MobileServiceInvalidOperationException. Useful
        /// because the message returned from the Azure APIs need to be parsed from JSON.
        /// </summary>
        /// <param name="ex">The MobileServiceInvalidOperationException</param>
        /// <returns>String representation of the error message</returns>
        private async Task<string> getMobileServiceExceptionMessage(MobileServiceInvalidOperationException ex)
        {
            string message = "";
            HttpResponseMessage response = ex.Response;
            if (response != null)
            {
                string jsonString = await response.Content.ReadAsStringAsync();
                JObject jObject = JsonConvert.DeserializeObject<JObject>(jsonString);
                message = (string)jObject["message"];
            }
            return message;
        }
    }
}