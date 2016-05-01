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

namespace LWalshFinalClient
{
    [Activity(Label = "Household", MainLauncher = false, Icon = "@drawable/icon")]
    class HouseholdActiviy : Activity
    {
        public MobileServiceClient client;

        Button homeButton;
        Button votesButton;
        Button messagesButton;
        string currentUserID;
        string currentHHID;
        Household currentHousehold;
        List<HouseholdMember> members;

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

            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.messagesButton.Click += navigationClick;

            //if the activity was instantiated by an intent with parameters, get the parameters
            //and initialize the appropriate class variables
            getIntentParameters();
            updateDisplay();
        }

        private async void updateDisplay()
        {
            await getHousehold();
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
            bundle.PutString("MyData", "Data from Activity1");
            bundle.PutString("isLoggedIn", "true");
            bundle.PutString("currentUserID", this.currentUserID);
            //serialize the mobilserivce client so user data stays intact
            //var clientJson = new JavaScriptSerializer().Serialize(this.client);
            //bundle.PutString("client", clientJson);
            newActivity.PutExtras(bundle);


            //newActivity.PutExtra("MyData", "Data from Activity1");
            StartActivity(newActivity);
        }
    
    }
}