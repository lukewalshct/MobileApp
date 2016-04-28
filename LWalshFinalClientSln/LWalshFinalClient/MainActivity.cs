using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;

namespace LWalshFinalClient
{
    [Activity(Label = "LWalshFinalClient", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public MobileServiceClient client;

        Button loginButton;
        Button quitButton;
        bool isLoggedIn;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/");

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            loginButton = FindViewById<Button>(Resource.Id.loginButton);
            quitButton = FindViewById<Button>(Resource.Id.quitButton);

            loginButton.Click += loginButtonClick;
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
                    client.Logout();
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
    }
}

