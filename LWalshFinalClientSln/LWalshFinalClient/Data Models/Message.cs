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
    class Message
    {                
        public string hhid { get; set; }

        public string userid { get; set; }

        public string message { get; set; }

        public string memberName { get; set; }

        public string timeStamp { get; set; }
    }
}