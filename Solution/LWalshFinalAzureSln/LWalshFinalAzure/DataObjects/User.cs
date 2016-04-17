using Microsoft.Azure.Mobile.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    public class User : EntityData  
    {
        public string firstName { get; set; }

        public string lastName { get; set; }

        public string IDPUserID { get; set; }

        public string household { get; set; }

        public ICollection<HouseholdMember> memberships { get; set; }

        public User ()
        {
            this.memberships = new List<HouseholdMember>();
        }
    }
}