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
   
    public class UserController : TableController<User>
    {
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            MobileServiceContext context = new MobileServiceContext();
            DomainManager = new EntityDomainManager<User>(context, Request);
        }

        // GET tables/User
        [HttpGet]
        [Route("user/all")]
        [ActionName("all")]
        public IQueryable<User> GetAllUser()
        {
            //need to find the user first, get the id, then attach since if we use the actual user
            //and try to add the user to the HH's list it will try to insert the user again
            //which will cause an error

            //try
            //{
            //    User newUser = new User() { Id = Query().First().Id };
            //    context.Users.Attach(newUser);

            //    Household testHH = new Household();
            //    testHH.Id = Guid.NewGuid().ToString();
            //    testHH.name = "Test Household";
            //    testHH.users.Add(newUser);
            //    context.Households.Add(testHH);
            //    context.SaveChanges();
            //}
            //catch (Exception e)
            //{

            //}
            return Query();
        }

        [HttpGet]
        [Route("user/byid/{id}")]
        [ActionName("byid")]
        // GET tables/User/48D68C86-6EA6-4C25-AA33-223FC9A27959
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

        [HttpGet]
        [Route("user/byauth")]
        [ActionName("byauth")]
        [Authorize]
        // GET tables/User/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public async Task<User> GetUserByAuth()
        {
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

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
        //// PATCH tables/User/48D68C86-6EA6-4C25-AA33-223FC9A27959
        //public Task<User> PatchUser(string id, Delta<User> patch)
        //{
        //     return UpdateAsync(id, patch);
        //}

        //// POST tables/User
        //public async Task<IHttpActionResult> PostUser(User item)
        //{
        //    User current = await InsertAsync(item);
        //    return CreatedAtRoute("Tables", new { id = current.Id }, current);
        //}

        //// DELETE tables/User/48D68C86-6EA6-4C25-AA33-223FC9A27959
        //public Task DeleteUser(string id)
        //{
        //     return DeleteAsync(id);
        //}
    }
}
