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

        }
    }
}

