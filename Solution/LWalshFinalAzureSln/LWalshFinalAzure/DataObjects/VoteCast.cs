using Microsoft.Azure.Mobile.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    /// <summary>
    /// A class that represents a vote cast
    /// </summary>
    public class VoteCast : EntityData
    {
        public string voteId { get; set; }

        public string vote { get; set; }
    }
}