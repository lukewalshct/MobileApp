using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.Azure.Mobile.Server;
using LWalshFinalAzure.DataObjects;
using LWalshFinalAzure.Models;
using System.Collections.Generic;
using System;

namespace LWalshFinalAzure.Controllers
{
    /// <summary>
    /// The primary role of the user controller is to retrieve data about users.
    /// </summary>
    public class UserController : TableController<User>
    {
        //set context and config settings
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        //initialize the controller
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            MobileServiceContext context = new MobileServiceContext();
            DomainManager = new EntityDomainManager<User>(context, Request);
        }

        /// <summary>
        /// GET method that returns all users.
        /// </summary>
        /// <returns>IQueryable of all users</returns>
        [HttpGet]
        [Route("user/all")]
        [ActionName("all")]
        public IQueryable<User> GetAllUser()
        {
            return Query();
        }

        /// <summary>
        /// GET method that returns a user by the id specified.
        /// </summary>
        /// <param name="id">The user id</param>
        /// <returns>The user</returns>
        [HttpGet]
        [Route("user/byid/{id}")]
        [ActionName("byid")]        
        public SingleResult<User> GetUser(string id)
        {
            if (id != null && id.Length > 0)
            {
                return Lookup(id);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// GET method that returns the current, authorized user.
        /// </summary>
        /// <returns>The authorized user</returns>
        [HttpGet]
        [Route("user/byauth")]
        [ActionName("byauth")]
        [Authorize]        
        public async Task<User> GetUserByAuth()
        {
            //get user info from the Facebook graph
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            //return user in the Azure SQL database that matches the Facebook user info
            if (userInfo != null)
            {
                return this.context.Users.Where(x => x.IDPUserID == 
                    userInfo.providerType + ":" + userInfo.IDPUserId).SingleOrDefault();
            }
            else
            {
                return null;
            }
        }        
    }
}
