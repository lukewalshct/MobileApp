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
    class HouseholdMember
    {
        public string firstName { get; set; }
        public string lastName { get; set; }

        public string status { get; set; }

        public double karma { get; set; }

        public bool isLandlord { get; set; }

        public bool isLandlordVote { get; set; }

        public bool isEvictVote { get; set; }
        public bool isApproveVote { get; set; }
        public string userId { get; set; }
        public string householdId { get; set; }
    }

    //public enum Status { Approved, Declined, Pending }
}