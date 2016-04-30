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

namespace LWalshFinalClient
{
    [Activity(Label = "LWalshFinalClient", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public MobileServiceClient client;

        Button loginButton;
        Button quitButton;
        Button createHHButton;
        Button registerButton;
        Button getHHButton;
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
            this.registerButton = FindViewById<Button>(Resource.Id.registerButton);
            this.getHHButton = FindViewById<Button>(Resource.Id.getHHButton);
            this.householdsListView = FindViewById<ListView>(Resource.Id.HHListView);
            this.HHNameTextView = FindViewById<TextView>(Resource.Id.HHName);
            this.HHLandlordTextView = FindViewById<TextView>(Resource.Id.HHLandlord);

            this.loginButton.Click += loginClick;
            this.createHHButton.Click += createHHClick;
            this.quitButton.Click += quitClick;
            this.registerButton.Click += registerClick;
            this.getHHButton.Click += getHHClick;

            this.isLoggedIn = false;
            this.isRegistered = false;
            this.isMyHHListView = true;
            this.isHomeScreen = true;
            
            updateDisplay();
        }

        private void getHHClick(Object sender, EventArgs e)
        {            
            updateDisplay();
        }

        private void updateDisplay()
        {
            this.loginButton.Text = this.isLoggedIn ? "Logout" : "Login";
            this.registerButton.Visibility = this.isRegistered ? ViewStates.Gone : ViewStates.Visible;
            this.HHNameTextView.Visibility = ViewStates.Gone;
            this.HHLandlordTextView.Visibility = ViewStates.Gone;
            //if it's the home screen, retrieve the list of households to which the user belongs
            if (this.isHomeScreen)
            {
                this.householdsListView.Enabled = true;
                updateHouseholdList();
                displayHouseholds();
            }
            else
            {
                this.householdsListView.Enabled = false;
            }
        }


        private async void updateHouseholdList()
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

                            //newHH.currencyName = (string)((JObject)hh)["currencyName"];
                            hhListItem.name = (string)((JObject)hh)["name"];
                            //newHH.landlordIDP = (string)((JObject)hh)["landlordIDP"];
                            hhListItem.landlordName = (string)((JObject)hh)["landlordName"];
                            //newHH.description = (string)((JObject)hh)["description"];

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
                }
                else
                {
                    this.isLoggedIn = false;
                    this.isRegistered = false;
                    this.currentUserID = "";
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
                string message;

                try
                {
                    // Authenticate using provider type passed in. 
                    await client.LoginAsync(this, providerType);

                    message = "You are now logged in.";
                }
                catch (InvalidOperationException ex)
                {
                    message = "You must log in. Login Required" + ex.Message;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                }

                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetMessage(message);
                builder.Create().Show();
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
                message = "You must be logged in and registered to create a new household.";
            }
            else
            {
                try
                {
                    JToken result = await this.client.InvokeApiAsync("household/create", HttpMethod.Post, null);

                    if (result.HasValues)
                    {
                        message = (string)result.Children().FirstOrDefault().ToString();
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
            updateDisplay();
        }

        private async void getFriendsClick(Object sender, EventArgs e)
        {
            try
            {
                string uri = "user/byid/12345/friends";
                JToken result = await this.client.InvokeApiAsync(uri, HttpMethod.Get, null);
            }
            catch (Exception ex)
            {

            }
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
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        async void registerClick(object sender, EventArgs e)
        {
            string message = "";
            if (!this.isLoggedIn)
            {
                message = "You must login to register.";
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
                        this.isRegistered = true;
                        message = "You are registered and may now access your households!";
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
            updateDisplay();
        }


    }
}

