using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.Models;
using Microsoft.Azure.Mobile.Server;
using System.Threading.Tasks;
using System;
using LWalshFinalAzure.DataObjects;
using System.Linq;
using System.Net.Http;

namespace LWalshFinalAzure.Controllers
{
    [MobileAppController]
    public class RegistrationController : ApiController
    {

        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        /// <summary>
        /// HTTP GET method that returns the registration status of the current user. Authentication required.
        /// </summary>
        [Authorize]
        public async Task<Object> GetAuth()
        {
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();
            User u = null;
            if (userInfo != null)
            {
                u = this.context.Users.Where(x => x.IDPUserID == userInfo.providerType + ":" + userInfo.IDPUserId).SingleOrDefault();
            }

            Object o = null;
            if (u != null)
            {
                o = new
                {
                    firstName = u.firstName,
                    lastName = u.lastName,
                };
            }
            return o;
        }        

        /// <summary>
        /// HTTP Post method that allows any authenticated user to register. Requires authentication.
        /// </summary>
        //[Authorize]
        public async Task<HttpResponseMessage> Post()
        {
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            if (userInfo != null)
            {
                User newUser = new User();
                newUser.Id = Guid.NewGuid().ToString();
                newUser.IDPUserID = userInfo.providerType + ":" + userInfo.IDPUserId;
                newUser.firstName = userInfo.Name;

                User checkExistUser = this.context.Users.Where(x => x.IDPUserID == newUser.IDPUserID).SingleOrDefault();
                if (checkExistUser != null)
                {
                    return Request.CreateResponse(System.Net.HttpStatusCode.Conflict,
                        new { Message = "User already exists." });
                }

                context.Users.Add(newUser);
                context.SaveChanges();

                return Request.CreateResponse(System.Net.HttpStatusCode.OK,
                new { Message = "User " + newUser.IDPUserID + " is now registered!" });
            }
            return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest,
                new { Message = "Error with registration. Please try again." });
        }
    }
}
