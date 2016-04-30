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
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.client = new MobileServiceClient("https://lwalshfinal.azurewebsites.net/", new HttpAutoProxyHandler());
            //this.client = new MobileServiceClient("http://localhost:50103/");

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Household);

            // Get our button from the layout resource,
            // and attach an event to it
            this.homeButton = FindViewById<Button>(Resource.Id.homeButton);

            this.homeButton.Click += homeClick;
        }

        private void homeClick(Object sender, EventArgs e)
        {

        }
    }
}