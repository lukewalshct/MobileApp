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

namespace LWalshFinalClient
{
    class Household
    {
        public string id { get; set; }
        public string name { get; set; }

        public string description { get; set; }

        public string currencyName { get; set; }

        public string landlordIDP { get; set; }

        public string landlordName { get; set; }
    }
}