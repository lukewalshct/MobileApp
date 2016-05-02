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
    [Activity(Label = "Messages/Notifications")]
    public class MessageActivity : Activity
    {
        MobileServiceClient client;
        Button homeButton;
        Button votesButton;
        Button householdInfoButton;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            //this.client = new MobileServiceClient("http://localhost:50103/");

            // Set our view from the "household" layout resource
            SetContentView(Resource.Layout.Message);

            // Get our button from the layout resource,
            // and attach an event to it
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);
            this.votesButton = FindViewById<Button>(Resource.Id.votesButton);
            this.householdInfoButton = FindViewById<Button>(Resource.Id.HHInfoButton);

            this.homeButton.Click += navigationClick;
            this.votesButton.Click += navigationClick;
            this.householdInfoButton.Click += navigationClick;

            string text = this.Intent.GetStringExtra("MyData") ?? "Data not available";
        }

        private void navigationClick(Object sender, EventArgs e)
        {

        }
    }
}