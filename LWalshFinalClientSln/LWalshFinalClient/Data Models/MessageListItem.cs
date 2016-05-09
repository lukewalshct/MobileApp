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
    class MessageListItem
    {
        public string message { get; set; }
        public string sender { get; set; }
        public string timestamp { get; set; }
    }
}