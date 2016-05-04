using Microsoft.Azure.Mobile.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    public class Vote : EntityData    {
        
        public ICollection<HouseholdMember> membersVoted { get; set; }

        public string targetMemberName { get; set; }
        public string householdID { get; set; }
        
        public VoteType voteType { get; set; }

        public string targetMemberID { get; set; }

        public int balanceChange { get; set; }

        public bool isAnonymous { get; set; }

        public string description { get; set; }

        public int votesFor { get; set; }

        public int votesAgainst { get; set;  }

        public string voteStatus { get; set; }

        public Vote()
        {
            this.membersVoted = new List<HouseholdMember>();
        }
    }

    public enum VoteType { Karma, Landlord, NewMember, EvictMember}
}