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
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();
            Friends friends = await idpTransaction.getUserFriends();

            if (friends != null)
            {
                User user = this.context.Users.Where(x => x.IDPUserID == "Facebook:" + userInfo.IDPUserId).Single(); 
                List<string> friendIDs = friends.friends.Select(x => ("Facebook:" + x.id)).ToList();
                List<User> friendsUsers = this.context.Users.Where(x => friendIDs.Contains(x.IDPUserID)).ToList();
                List<Household> allHouseholdsLoaded = this.context.Households.Include("members").ToList();
                List<Household> friendsHouseholds = new List<Household>();
                foreach (User friendsUser in friendsUsers)
                {
                    //get households that the friends 
                    List<Household> friendsHousehold = allHouseholdsLoaded.Where(x => x.members.Select(y => y.userId).Contains(friendsUser.Id) &&
                        !x.members.Select(y => y.userId).Contains(user.Id)).ToList();
                    if (friendsHousehold != null)
                    {
                        friendsHouseholds.AddRange(friendsHousehold);
                    }                    
                }
                friendsHouseholds = friendsHouseholds.Distinct<Household>().ToList();                
                return Request.CreateResponse(System.Net.HttpStatusCode.OK, friendsHouseholds);
            }
            else
            {
                return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest, new { Message = "Error retrieving friend list" });
            }                    
        }        
    }
}
