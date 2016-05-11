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
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace LWalshFinalClient
{
    /// <summary>
    /// This class represents the landing screen for a household which contains basic info about
    /// the household such as its name, description, currency, landlord, and a list of members.
    /// The landlord can edit this info. To users that are members of the household, there are
    /// buttons that take the user to the Voting and Messaging screens, and for users that are
    /// not members there's a button that allows the user to request to join the household.
    /// </summary>
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
        User currentUser;
        bool isMember;
        bool isEditInfo;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            //declare the client
            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
                        
            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Household);

            // Assign buttons, views, etc
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

            //Assign click event handlers
            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.messagesButton.Click += navigationClick;
            this.saveEditButton.Click += saveEditClick;
            this.cancelEditButton.Click += cancelEditClick;
            this.joinButton.Click += joinClick;
            this.isMember = false;
            this.isEditInfo = false;
            //get the intent parameters and initialize the appropriate class variables
            //this is done so that when the user navigates to this screen from one of the
            //other screens, the user's authentication data is preserved so the screen can show
            //the relevant data
            getIntentParameters();
            //update display            
            updateDisplay();
        }

        /// <summary>
        /// Updates the display
        /// </summary>
        private async void updateDisplay()
        {            
            this.joinButton.Visibility = ViewStates.Gone;
            //get the household info
            await getHousehold();
            //get info about the user
            await getUser();
            //the user is a member, get info about the current member
            this.isMember = await getCurrentMember();
            //display the basic household info
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
            //display a list of members in the household
            displayMembers();
            //enable/disable buttons based on whether the user is a member
            if (this.isMember)
            {
                //if member, allow navigation to message and voting screens
                this.votesButton.Clickable = true;
                this.messagesButton.Clickable = true;
                this.votesButton.Enabled = true;
                this.messagesButton.Enabled = true;                                          
            }
            else
            {
                //if not member, do not allow navigation to the message and voting screens
                //show the "Request to Join" button and alert the user they are not a member
                this.votesButton.Clickable = false;
                this.messagesButton.Clickable = false;
                this.votesButton.Enabled = false;
                this.messagesButton.Enabled = false;
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                string message = "You are not a member of this household. Please click the 'Request to Join Household' button " +
                    "to join the household. \n\nOnce the request is made, all household members must approve your membership.";
                builder.SetMessage(message);
                builder.Create().Show();
            }
            //enable/disable buttons based on the user's landlord status
            if (this.isMember && this.currentMember != null && this.currentMember.isLandlord)
            {
                //if the member is the landlord, display buttons that allow the user to edit 
                //the household info
                this.landlordTitleLayout.Visibility = ViewStates.Visible;
                this.userTitleLayout.Visibility = ViewStates.Gone;
                this.saveEditButton.Clickable = true;
                this.cancelEditButton.Clickable = true;                                
            }
            else
            {
                //if user is not landlord, do not allow household info to be edited
                this.landlordTitleLayout.Visibility = ViewStates.Gone;
                this.userTitleLayout.Visibility = ViewStates.Visible;
                this.saveEditButton.Clickable = false;
                this.cancelEditButton.Clickable = false;
            }
            //if the household info is currently being edited, allow the fields
            //to be clicked and edited
            if (this.isEditInfo)
            {
                this.hhNameEditText.Enabled = true;
                this.hhCurrencyEditText.Enabled = true;
                this.hhDescriptionEditText.Enabled = true;
            }
            //else the text fields are not editable/clickable
            else
            {
                this.hhNameEditText.Enabled = false;                
                this.hhCurrencyEditText.Enabled = false;
                this.hhDescriptionEditText.Enabled = false;
                this.hhNameEditText.SetTextColor(Color.White);
                this.hhCurrencyEditText.SetTextColor(Color.White);
                this.hhDescriptionEditText.SetTextColor(Color.White);
            }
            //display cancel and save buttons if the info is being edited; else hide
            this.cancelEditButton.Visibility = this.isEditInfo ? ViewStates.Visible : ViewStates.Invisible;
            this.saveEditButton.Text = this.isEditInfo ? "Save" : "Edit Info";
            this.joinButton.Visibility = this.isMember ? ViewStates.Gone : ViewStates.Visible;
        }

        /// <summary>
        /// Event handler triggered when the "Request to Join" button is clicked. This calls
        /// the sendJoinRequest method.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private async void joinClick(Object sender, EventArgs e)
        {
            await sendJoinRequest();            
        }

        /// <summary>
        /// Makes a POST request to the Household resource in Azure, adding a NewMember
        /// vote to the household's list of votes with the user as the target member.
        /// The result is that a proposal to allow the user to join the household appears
        /// in the household members' voting screen, and if it's passed the user will
        /// become a member of the household.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> sendJoinRequest()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            try
            {
                //create a new vote that represents a NewMember vote/proposal
                Vote vote = new Vote();

                vote.targetMemberID = this.currentUserID;
                if (this.currentUser != null)
                {
                    vote.targetMemberName = this.currentUser.firstName;
                }
                vote.balanceChange = 0;
                vote.description = "Request to join household";
                vote.householdID = this.currentHHID;
                vote.isAnonymous = false;
                vote.voteType = VoteType.NewMember;

                JToken payload = JObject.FromObject(vote);
                JToken result = await this.client.InvokeApiAsync("vote/newvote", payload);

                //if successful, display message to the user
                if (result.HasValues)
                {
                    message = "Successfully submitted request to join the household! The request will now appear in the household's" +
                        "vote list to all household members. All current household members must approve your request in order to join.";
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

        /// <summary>
        /// Event handler when the save/edit button are clicked. This button appears only for 
        /// the landlord and when clicked allows the landlord to edit the household info. When
        /// clicked ("Save") while editing, a POST call is then made to the Household resource 
        /// to update the household info in the Azure SQL database.
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private async void saveEditClick(Object sender, EventArgs e)
        {
            //if the button is clicked while the info is being edited (displaying "Save"),
            //make a call to the Household resource to update the Azure SQL database
            if (this.isEditInfo)
            {
                bool successfulUpdate = await updateHouseholdInfo();
                //if the update is successful set the state of the screen to "not editing"
                if (successfulUpdate)
                {
                    this.isEditInfo = false;
                }
            }
            //if the household info wasn't already being edited, set the screen state to "editing"
            else
            {
                this.isEditInfo = true;
            }
            //update the display
            updateDisplay();
        }

        /// <summary>
        /// Method called when the saveEditButton click event is triggered; makes a call to the 
        /// Household resource to update the household info in the Azure SQL database.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> updateHouseholdInfo()
        {
            string message = "";
            AlertDialog.Builder builder = new AlertDialog.Builder(this);            
            try
            {
                //create a new household based on the current household but using
                //the new name, description, and currency entered by the landlord
                Household hh = new Household();
                hh.id = this.currentHHID;
                hh.landlordIDP = this.currentUserID;
                hh.name = this.hhNameEditText.Text;
                hh.description = this.hhDescriptionEditText.Text;
                hh.currencyName = this.hhCurrencyEditText.Text;

                //make call to the household resource
                JToken payload = JObject.FromObject(hh);
                JToken result = await this.client.InvokeApiAsync("household/editinfo", payload);

                //if successful, display success message
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
        /// Event handler triggered when the cancel button is clicked. The cancel button
        /// is only displayed when the info is being edited and this method essentially
        /// cancels all changes and returns the screen to a "not editing" state
        /// </summary>
        /// <param name="sender">The button</param>
        /// <param name="e">The event args</param>
        private void cancelEditClick(Object sender, EventArgs e)
        {
            this.isEditInfo = false;
            updateDisplay();
        }

        /// <summary>
        /// Method that makes a call to the Household resource and retrieves information about 
        /// the household to display to the user. This is called when then screen is first created 
        /// or if any updates are made.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> getHousehold()
        {
            string errorMessage = "";
            try
            {
                //make call to the household resource
                JToken result = await this.client.InvokeApiAsync("household/byid/" + this.currentHHID, HttpMethod.Get, null);

                //if successful call, update the household data
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
                errorMessage = await getMobileServiceExceptionMessage(ex);
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
        /// Makes a call to the User resource to get the current user.
        /// </summary>
        /// <returns>Bool indicating success or failure</returns>
        private async Task<bool> getUser()
        {
            string errorMessage = "";
            try
            {
                //make call to the User resource
                JToken result = await this.client.InvokeApiAsync("user/byauth", HttpMethod.Get, null);

                //if successful request, update the current user
                if (result.HasValues)
                {
                    //parse the user info
                    this.currentUser = new User();

                    this.currentUser.firstName = (string)((JObject)result)["firstName"];
                    this.currentUser.lastName = (string)((JObject)result)["lastName"];
                    this.currentUser.IDPUserID = (string)((JObject)result)["IDPUserID"];                    
                    
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
            if (errorMessage != "")
            {
                builder.SetMessage(errorMessage);
                builder.Create().Show();
            }
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
        /// Displays the list of household members to the user as a scrollable list. This is accomplished by using
        /// an adapter (MemberScrollAdapter) which binds the data to the listview (membersListView).
        /// </summary>
        /// </summary>
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
    
    }
}