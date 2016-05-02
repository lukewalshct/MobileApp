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
        HouseholdMember currentMember;

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
            //only refresh members list if a new proposal is being edited
            if (this.isProposingVote)
            {
                await getHousehold();
                await getCurrentMember();
                setMemberSpinnerDropdown();
            }
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
                var clientJson = this.Intent.Extras.GetString("client");
                if (clientJson != null)
                {
                    MobileServiceClient client = new JavaScriptSerializer().Deserialize<MobileServiceClient>(clientJson);
                    this.client = client;
                }
            }
        }

        private void proposeVoteClick(Object sender, EventArgs e)
        {
            this.isProposingVote = true;
            updateDisplay();
        }

        private void submitClick(Object sender, EventArgs e)
        {
            this.isProposingVote = false;
            updateDisplay();
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
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(errorMessage);
            builder.Create().Show();
            return false;
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