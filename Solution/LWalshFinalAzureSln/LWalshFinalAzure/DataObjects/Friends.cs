using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure.DataObjects
{
    /// <summary>
    /// A class that represents all the user's friends on Facebook
    /// that are also using the app
    /// </summary>
    public class Friends
    {
        public List<FacebookFriend> friends { get; set; }
    }
}