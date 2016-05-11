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
    /// <summary>
    /// A controller with the primary role to retrieve the households of Facebook friends of the current user who also use the app. 
    /// This is accomplished via an authenticated GET request. The friend controller is called on 
    /// the app’s main/welcome screen after the user logs in to allow the user to see the households of his or her 
    /// friends that also use the app.
    /// </summary>
    [MobileAppController]
    public class FriendController : ApiController
    {
        //set the context and configuration settings
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();


        /// <summary>
        /// GET request that returns a list of the households of the user's Facebook friends 
        /// that have also authenticated their Facebook account with the app, but that the 
        /// user is NOT yet a member of. If the user is in the same household as a Facebook 
        /// friend, that household will not appear in the result.
        /// </summary>
        /// <param string="id">The user's id.</param>
        /// <returns>List of households the user's Facebook friends belong to, but which
        /// the user does not.</returns>
        [HttpGet]
        [Route("user/byid/{id}/friends")]
        [ActionName("byid")]
        [Authorize]
        public async Task<HttpResponseMessage> GetFriends(string id)
        {
            //make a call to the Facebook Graph to confirm the user's identity and get user's Facebook credentials
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();
            //make another call to the Facebook Graph to get the user's friends
            Friends friends = await idpTransaction.getUserFriends();

            if (friends != null)
            {
                //find the current user in the database
                User user = this.context.Users.Where(x => x.IDPUserID == "Facebook:" + userInfo.IDPUserId).Single(); 
                //extract the Facebook user ids from the friends list returned from Faebook
                List<string> friendIDs = friends.friends.Select(x => ("Facebook:" + x.id)).ToList();
                //get a list of users from Azure SQL database that are user's Facebook friends
                List<User> friendsUsers = this.context.Users.Where(x => friendIDs.Contains(x.IDPUserID)).ToList();
                //get a list of households that those facebook friends belong to but the user does not
                List<Household> allHouseholdsLoaded = this.context.Households.Include("members").ToList();
                List<Household> friendsHouseholds = new List<Household>();
                foreach (User friendsUser in friendsUsers)
                {
                    //get households that the friend belongs to and only add it to results if the user is not a member
                    List<Household> friendsHousehold = allHouseholdsLoaded.Where(x => x.members.Select(y => y.userId).Contains(friendsUser.Id) &&
                        !x.members.Select(y => y.userId).Contains(user.Id)).ToList();
                    if (friendsHousehold != null)
                    {
                        friendsHouseholds.AddRange(friendsHousehold);
                    }                    
                }
                //return distinct list of households
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
