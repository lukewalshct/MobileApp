﻿using Microsoft.Azure.Mobile.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    public class HouseholdMember : EntityData
    {
        public string firstName { get; set; }
        public string lastName { get; set; }

        public  Status status { get; set; }

        public int karma { get; set; }

        public bool isLandlord { get; set; }

        public bool isLandlordVote { get; set; }

        public bool isEvictVote { get; set; }
        public bool isApproveVote { get; set; }
        public string userId { get; set; }
        public string householdId { get; set; }
    }

    public enum Status { Approved, Declined, Pending }

}