using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.DataObjects;
using System;
using LWalshFinalAzure.Models;
using Microsoft.Azure.Mobile.Server;
using System.Threading.Tasks;
using System.Net.Http;

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
        public async Task<Object> GetFriends(string id)
        {
            HttpRequestMessage testRequest = new HttpRequestMessage();
            //testRequest.
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            string testID = "811503865646931";
            return id;
        }
    }
}
