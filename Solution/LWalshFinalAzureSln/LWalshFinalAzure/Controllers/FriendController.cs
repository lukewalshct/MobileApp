using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.DataObjects;
using System;
using LWalshFinalAzure.Models;
using Microsoft.Azure.Mobile.Server;
using System.Threading.Tasks;

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
        public async Task<Object> GetFriends(string id)
        {
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            Object o = await idpTransaction.getUserFriends();

            return userInfo;
        }
    }
}
