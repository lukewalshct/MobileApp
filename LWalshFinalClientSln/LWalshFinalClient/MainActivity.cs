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
        Button getFriendsButton;
        bool isLoggedIn;
        bool isRegistered;
        bool isMyHHListView;
        bool isHomeScreen;
        string currentUserID;

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
            this.getFriendsButton = FindViewById<Button>(Resource.Id.getFriendsButton);

            this.loginButton.Click += loginClick;
            this.createHHButton.Click += createHHClick;
            this.quitButton.Click += quitClick;
            this.registerButton.Click += registerClick;
            this.getFriendsButton.Click += getFriendsClick;

            this.isLoggedIn = false;
            this.isRegistered = false;
            this.isMyHHListView = false;
            this.isHomeScreen = true;

            updateDisplay();
        }

        private void updateDisplay()
        {
            this.loginButton.Text = this.isLoggedIn ? "Logout" : "Login";
            this.registerButton.Visibility = this.isRegistered ? ViewStates.Gone : ViewStates.Visible;
            //if it's the home screen, retrieve the list of households to which the user belongs
            if (this.isHomeScreen)
            {
                updateHouseholdList();
            }
        }


        private async void updateHouseholdList()
        {
            //if the user is viewing his/her own households, get households for that user
            if(this.isMyHHListView)
            {
                try
                {
                    JToken result = await this.client.InvokeApiAsync("household", HttpMethod.Get, null);
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

