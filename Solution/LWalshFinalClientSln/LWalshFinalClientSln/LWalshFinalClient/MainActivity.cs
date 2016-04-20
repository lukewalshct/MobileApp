using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

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

        private void loginButtonClick(Object sender, EventArgs e)
        {
            if (client.CurrentUser == null || client.CurrentUser.UserId == null)
            {
                this.isLoggedIn = true;
                MobileServiceAuthenticationProvider providerType;

                if (this.loginTwitter.Checked)
                {
                    providerType = MobileServiceAuthenticationProvider.Twitter;
                }
                else if (this.loginGoogle.Checked)
                {
                    providerType = MobileServiceAuthenticationProvider.Google;
                }
                else if (this.loginFacebook.Checked)
                {
                    providerType = MobileServiceAuthenticationProvider.Facebook;
                }
                else
                {
                    providerType = MobileServiceAuthenticationProvider.MicrosoftAccount;
                }

                await AuthenticateUserAsync(providerType);
            }
            else
            {
                this.isLoggedIn = false;
                this.isProfileView = false;
                this.remainingVotesValue.Text = "";
                await client.LogoutAsync();
            }
            await saveCurrentItem();
            refreshFeaturesList();
            updateDisplay(null);
        }
    }
}

