using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.DataObjects;
using System;

namespace LWalshFinalAzure.Controllers
{
    [MobileAppController]
    public class FriendController : ApiController
    {
        [HttpGet]
        [Route("user/byid/{id}/friends")]
        [ActionName("byid")]
        public Object GetFriends(string id)
        {
            return id;
        }
    }
}
