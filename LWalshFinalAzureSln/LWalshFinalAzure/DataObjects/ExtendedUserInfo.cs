using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LWalshFinalAzure
{
    /// <summary>
    /// A class that represents user info obtained from an IDP.
    /// </summary>
    public class ExtendedUserInfo
    {
        [JsonProperty(PropertyName = "Id")]
        public string IDPUserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Gender { get; set; }
        public string providerType { get; set; }
        public string pictureURL { get; set; }
    }
}