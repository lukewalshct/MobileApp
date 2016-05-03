using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using LWalshFinalClient.Resources;
using System.Web.Script.Serialization;

namespace LWalshFinalClient
{
    [Activity(Label = "Home", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public MobileServiceClient client;

        Button loginButton;
        Button quitButton;
        Button createHHButton;                
        bool isLoggedIn;
        bool isRegistered;
        bool isMyHHListView;
        bool isHomeScreen;
        string currentUserID;
        List<HHListItem> HHListItems;
        ListView householdsListView;
        TextView HHNameTextView;
        TextView HHLandlordTextView;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            //this.client = new MobileServiceClient("http://localhost:50103/");

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            this.loginButton = FindViewById<Button>(Resource.Id.loginButton);
            this.quitButton = FindViewById<Button>(Resource.Id.quitButton);
            this.createHHButton = FindViewById<Button>(Resource.Id.createHHButton);            
            this.householdsListView = FindViewById<ListView>(Resource.Id.HHListView);
            this.HHNameTextView = FindViewById<TextView>(Resource.Id.HHName);
            this.HHLandlordTextView = FindViewById<TextView>(Resource.Id.HHLandlord);

            this.loginButton.Click += loginClick;
            this.createHHButton.Click += createHHClick;
            this.quitButton.Click += quitClick;
            this.householdsListView.ItemClick += householdsListViewItemClick;

            this.isLoggedIn = false;
            this.isRegistered = false;
            this.isMyHHListView = true;
            this.isHomeScreen = true;

            //string text = this.Intent.GetStringExtra("MyData") ?? "Data not available";
            //if the activity was instantiated by an intent with parameters, get the parameters
            //and initialize the appropriate class variables
            getIntentParameters();
            updateDisplay();
        }

        private void getIntentParameters()
        {
            if (this.Intent.Extras != null)
            {                
                var isLoggedInString = this.Intent.Extras.GetString("isLoggedIn");
                this.currentUserID = this.Intent.Extras.GetString("currentUserID");
                if (isLoggedInString == "true")
                {
                    this.isLoggedIn = true;
                    this.isRegistered = true;
                }
                this.client.CurrentUser.UserId = this.Intent.Extras.GetString("clientUserID");
                this.client.CurrentUser.MobileServiceAuthenticationToken = this.Intent.Extras.GetString("clientAuthToken");
                //var clientJson = this.Intent.Extras.GetString("client");                
                //if (clientJson != null)
                //{
                //    MobileServiceClient client = new JavaScriptSerializer().Deserialize<MobileServiceClient>(clientJson);
                //    this.client = client;
                //}
            }
        }
        private async void updateDisplay()
        {
            this.loginButton.Text = this.isLoggedIn ? "Logout" : "Login";
            this.householdsListView.Visibility = (this.isLoggedIn && this.isRegistered) ? ViewStates.Visible : ViewStates.Invisible;            
            this.HHNameTextView.Visibility = ViewStates.Gone;
            this.HHLandlordTextView.Visibility = ViewStates.Gone;
            //if it's the home screen, retrieve the list of households to which the user belongs
            if (this.isHomeScreen)
            {
                this.householdsListView.Enabled = true;
                await updateHouseholdList();
                displayHouseholds();
            }
            else
            {
                this.householdsListView.Enabled = false;
            }
        }


        private async Task<bool> updateHouseholdList()
        {
            //if the user is viewing his/her own households, get households for that user
            if(this.isMyHHListView)
            {
                try
                {
                    string uri = "household/byuser/" + this.currentUserID.Substring(9);
                    JToken result = await this.client.InvokeApiAsync(uri, HttpMethod.Get, null);

                    if(result != null && result.HasValues)
                    {
                        List<HHListItem> households = new List<HHListItem>();

                        foreach(var hh in result)
                        {
                            HHListItem hhListItem = new HHListItem();
                                                        
                            hhListItem.name = (string)((JObject)hh)["name"];                            
                            hhListItem.landlordName = (string)((JObject)hh)["landlordName"];
                            hhListItem.id = (string)((JObject)hh)["id"];
                            
                            households.Add(hhListItem);
                        }

                        this.HHListItems = households;
                    }
                }
                catch (Exception ex)
                {

                }
            }
            //else get the list of households the users friends are a part of but the user is not a member
            else
            {

            }
            return true;
        }

        //if home screen, displays the households
        private void displayHouseholds()
        {
            if (this.HHListItems != null && this.HHListItems.Count > 0)
            {
                HHScrollAdapter householdsAdapter = new HHScrollAdapter(this, this.HHListItems);
                this.householdsListView.Adapter = householdsAdapter;
            }
        }

        private async void loginClick(Object sender, EventArgs e)
        {
            try {
                if (client.CurrentUser == null || client.CurrentUser.UserId == null)
                {
                    MobileServiceAuthenticationProvider providerType = MobileServiceAuthenticationProvider.Facebook;
                    await AuthenticateUserAsync(providerType);
                    this.isLoggedIn = true;
                    this.isRegistered = await registerUser();
                }
                else
                {
                    this.isLoggedIn = false;
                    this.isRegistered = false;
                    this.currentUserID = "";
                    this.HHListItems = null;
                    await client.LogoutAsync();
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetMessage("You are now logged out");
                    builder.Create().Show();
                }
            }
            catch(Exception ex)
            {

            }
            updateDisplay();
        }

        /// <summary>
        /// Authenticates the user through the given IDP.
        /// </summary>
        /// <param name="providerType">IDP type for authentication.</param>
        public async Task AuthenticateUserAsync(MobileServiceAuthenticationProvider providerType)
        {

            while (client.CurrentUser == null || client.CurrentUser.UserId == null)
            {
                string errorMessage = "";

                try
                {
                    // Authenticate using provider type passed in. 
                    await client.LoginAsync(this, providerType);                   
                }
                catch (InvalidOperationException ex)
                {
                    errorMessage = "You must log in. Login Required" + ex.Message;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                }

                if (errorMessage != "")
                {
                    AlertDialog.Builder builder = new AlertDialog.Builder(this);
                    builder.SetMessage(errorMessage);
                    builder.Create().Show();
                }
            }
        }

        private async void getTest(Object sender, EventArgs e)
        {
            try
            {
                JToken result = await this.client.InvokeApiAsync("user", HttpMethod.Get, null);
            }
            catch (Exception ex)
            {

            }
        }

        private async void createHHClick(Object sender, EventArgs e)
        {
            string message = "";
            if (!this.isLoggedIn || !this.isRegistered)
            {
                message = "You must be logged in to create a new household.";
            }
            else
            {
                try
                {
                    JToken result = await this.client.InvokeApiAsync("household/create", HttpMethod.Post, null);

                    if (result.HasValues)
                    {
                        message = (string)result["message"];
                        updateDisplay();
                    }
                }
                catch (MobileServiceInvalidOperationException ex)
                {
                    message = ex.Message;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(message);
            builder.Create().Show();            
        }
        
        /// Quits the app.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        async void quitClick(object sender, EventArgs e)
        {
            this.FinishAffinity();
        }


        /// <summary>
        /// Registers the user with the Azure service if they are logged in.
        /// </summary>
        private async Task<bool> registerUser()
        {
            string message = "";
            if (!this.isLoggedIn)
            {
                message = "Login failed, please try again.";
                return false;
            }
            else
            {
                try
                {
                    JToken result = await this.client.InvokeApiAsync("registration", null);

                    if (result.HasValues)
                    {
                        //set the current user id for use in future calls
                        this.currentUserID = (string)result["message"];                        
                        message = "You are logged in and may now access your households!";
                        return true;
                    }
                }
                catch (MobileServiceInvalidOperationException ex)
                {
                    message = ex.Message;                    
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }
            }
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(message);
            builder.Create().Show();
            return false;     
        }

        private void householdsListViewItemClick(Object sender, AdapterView.ItemClickEventArgs e)
        {
            if (this.HHListItems != null && this.HHListItems.Count > 0)
            {
                HHListItem item = this.HHListItems[e.Position];

                Type activityType = typeof(HouseholdActiviy);
                Intent newActivity = new Intent(this, activityType);
                var bundle = new Bundle();
                bundle.PutString("MyData", "Data from Activity1");
                bundle.PutString("isLoggedIn", "true");
                bundle.PutString("currentUserID", this.currentUserID);
                bundle.PutString("currentHHID", item.id);
                //pass the authentication data
                //string clientJson = new JavaScriptSerializer().Serialize(this.client);
                //bundle.PutString("client", clientJson);
                bundle.PutString("clientUserID", this.client.CurrentUser.UserId);
                bundle.PutString("clientAuthToken", this.client.CurrentUser.MobileServiceAuthenticationToken);
                newActivity.PutExtras(bundle);

                //newActivity.PutExtra("MyData", "Data from Activity1");
                StartActivity(newActivity);
            }
        }
    }
}

