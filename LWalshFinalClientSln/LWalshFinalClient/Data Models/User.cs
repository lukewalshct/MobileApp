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

namespace LWalshFinalClient.Data_Models
{
    class User
    {
        public string firstName { get; set; }

        public string lastName { get; set; }

        public string IDPUserID { get; set; }
    }
}