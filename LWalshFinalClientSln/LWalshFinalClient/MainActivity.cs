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
        Button testButton;
        Button registerButton;
        Button getFriendsButton;
        bool isLoggedIn;

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
            this.testButton = FindViewById<Button>(Resource.Id.testButton);
            this.registerButton = FindViewById<Button>(Resource.Id.registerButton);
            this.getFriendsButton = FindViewById<Button>(Resource.Id.getFriendsButton);

            this.loginButton.Click += loginButtonClick;
            this.testButton.Click += getTest;
            this.quitButton.Click += quitClick;
            this.registerButton.Click += registerClick;
            this.getFriendsButton.Click += getFriendsClick;

            this.isLoggedIn = false;

            updateDisplay();
        }

        private void updateDisplay()
        {
            this.loginButton.Text = this.isLoggedIn ? "Logout" : "Login";
        }

        private async void loginButtonClick(Object sender, EventArgs e)
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
                    await client.LogoutAsync();
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
        /// <summary>
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
        }
    }
}

