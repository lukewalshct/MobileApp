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
    class Vote
    {       

        public string householdID { get; set; }

        public VoteType voteType { get; set; }

        public string targetMemberID { get; set; }

        public int balanceChange { get; set; }

        public bool isAnonymous { get; set; }

        public string description { get; set; }

        public int votesFor { get; set; }

        public int votesAgainst { get; set; }

        public string voteStatus { get; set; }

    }

    public enum VoteType { Karma, Landlord, NewMember, EvictMember }
}
