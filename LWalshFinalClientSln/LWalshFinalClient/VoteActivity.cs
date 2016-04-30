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
    [Activity(Label = "Proposals Under Vote")]
    public class VoteActivity : Activity
    {
        MobileServiceClient client;

        Button homeButton;
        Button messagesButton;
        Button householdInfoButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());

            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Vote);

            // Create your application here
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButton);
            this.messagesButton = FindViewById<Button>(Resource.Id.messagesButton);

            this.homeButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;
            this.messagesButton.Click += navigationClick;
        }

        private void navigationClick(Object sender, EventArgs e)
        {

        }
    }
}