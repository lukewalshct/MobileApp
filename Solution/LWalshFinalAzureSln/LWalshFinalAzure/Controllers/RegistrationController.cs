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
    /// <summary>
    /// The primary role of the registration controller is to register a user, i.e. save their
    /// info in Azure SQL, once they've authenticated through Facebook. This allows their
    /// Facebook user ID and name to be stored in Azure SQL rather than having to make a 
    /// call out to Facebook's Graph every time that info is needed. The user does not have
    /// to initiate this action: once they authenticate through Facebook these methods
    /// are automatically called by the client app.
    /// </summary>
    [MobileAppController]
    public class RegistrationController : ApiController
    {
        //set context and conifg settings
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        /// <summary>
        /// HTTP GET method that returns the registration status of the current user. Authentication required.
        /// </summary>
        [Authorize]
        public async Task<Object> GetAuth()
        {
            //get user info from Facebook's graph
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
        /// POST method that allows any authenticated user to register. This method is called 
        /// automatically by the client app after the user logs in / authenticates with Facebook. 
        /// If the user already exists no action is taken, but if it’s a new Facebook user their 
        /// info is added to the Azure SQL database.
        /// </summary>
        [Authorize]
        public async Task<HttpResponseMessage> Post()
        {
            //get user info from Facebook graph
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
                    return Request.CreateResponse(System.Net.HttpStatusCode.OK,
                        new { Message = newUser.IDPUserID });
                }

                context.Users.Add(newUser);
                context.SaveChanges();

                return Request.CreateResponse(System.Net.HttpStatusCode.OK,
                new { Message = newUser.IDPUserID });
            }
            return Request.CreateResponse(System.Net.HttpStatusCode.BadRequest,
                new { Message = "Error with registration. Please try again." });
        }
    }
}
