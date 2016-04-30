using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.DataObjects;
using System;
using LWalshFinalAzure.Models;
using Microsoft.Azure.Mobile.Server;
using System.Threading.Tasks;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;

namespace LWalshFinalAzure.Controllers
{
    [MobileAppController]
    public class FriendController : ApiController
    {
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        [HttpGet]
        [Route("user/byid/{id}/friends")]
        [ActionName("byid")]
        [Authorize]
        public async Task<HttpResponseMessage> GetFriends(string id)
        {
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
           
            Friends friends = await idpTransaction.getUserFriends();

            if (friends != null)
            {
                List<string> friendIDs = friends.friends.Select(x => x.id).ToList();
                List<Household> friendsHouseholds = this.context.Households.Where(x => x.members.Select(y => y.Id)
                    .Contains("Facbook:" + x.Id)).ToList();
                return Request.CreateResponse(System.Net.HttpStatusCode.OK, friendsHouseholds);
            }
            else
            {
                return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest, new { Message = "Error retrieving friend list" });
            }                    
        }        
    }
}
