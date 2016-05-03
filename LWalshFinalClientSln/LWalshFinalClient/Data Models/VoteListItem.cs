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
    class VoteListItem
    {      
        public string voteType { get; set; }

        public string targetMember { get; set; }

        public int balanceChange { get; set; }        

        public string description { get; set; }

        public string statusText { get; set; }        
    }
}