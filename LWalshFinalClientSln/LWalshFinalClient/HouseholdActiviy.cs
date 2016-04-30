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
        }

        private void getIntentParameters()
        {
            if (this.Intent.Extras != null)
            {
                this.currentUserID = this.Intent.Extras.GetString("currentUserID");
                this.currentHHID = this.Intent.Extras.GetString("currentHHID");
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
            newActivity.PutExtras(bundle);

            //newActivity.PutExtra("MyData", "Data from Activity1");
            StartActivity(newActivity);
        }
    }
}