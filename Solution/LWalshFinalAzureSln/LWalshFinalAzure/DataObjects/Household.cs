using Microsoft.Azure.Mobile.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    /// <summary>
    /// A class that represents a household
    /// </summary>
    public class Household : EntityData
    {
        public string name { get; set; }

        public string description { get; set; }

        public string currencyName { get; set; }

        public string landlordIDP { get; set; }

        public string landlordName { get; set; }
        public ICollection<HouseholdMember> members { get; set; }

        public ICollection<Vote> votes { get; set; }

        public Household ()
        {
            this.members = new List<HouseholdMember>();
            this.votes = new List<Vote>();
        }
    }
}