using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    public class Message
    {
        public string id { get; set; }

        public string memberName { get; set; }

        public string hhid { get; set; }

        public string userid { get; set; }
        
        public string timeStamp { get; set; }

        public string message { get; set; }
    }
}