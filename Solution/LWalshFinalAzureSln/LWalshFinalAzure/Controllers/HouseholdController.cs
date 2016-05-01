using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.Models;
using System.Collections.Generic;
using System.Linq;
using LWalshFinalAzure.DataObjects;
using System;
using System.Net.Http;
using System.Net;
using Microsoft.Azure.Mobile.Server;
using System.Threading.Tasks;

namespace LWalshFinalAzure.Controllers
{
    [MobileAppController]
    //[RoutePrefix("api")]
    public class HouseholdController : ApiController
    {
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        [HttpGet]
        [Route("household/byuser/{id}")]
        [ActionName("byuser")]
        public List<Household> GetUserHouseholds(string id)
        {
            //find the user based on the id
            User u = this.context.Users.Where(x => x.IDPUserID == ("Facebook:" + id)).SingleOrDefault();

            if (u != null)
            {
                return this.context.Households.Include("members").Where(x => x.members.Select(y => y.userId)
                    .Contains(u.Id)).ToList();
            }
            else
            {
                return null;
            }
        }

        [HttpGet]
        [Route("household")]
        public IEnumerable<Household> GetAllHouseholds()
        {
            return this.context.Households;
        }

        [HttpGet]
        [Route("household/byid/{id}")]
        [ActionName("byid")]
        public Object GetHouseholdById(string id)
        {
            Household hh = this.context.Households.Where(x => x.Id == id).FirstOrDefault();

            if (hh != null)
            {
                return new
                {
                    name = hh.name,
                    description = hh.description,
                    currencyName = hh.currencyName,
                    landlordName = hh.landlordName,
                    members = this.context.HouseholdMembers.Where(x => x.householdId == id)
                };
            }
            else
            {
                return null;
            }
        }

        [HttpGet]
        [Route("household/byid/{HHID}/getmember/{userID}")]
        [ActionName("getmember")]
        public HouseholdMember GetMember (string HHID, string userID)
        {
            return this.context.HouseholdMembers.Where(x => x.householdId == HHID && x.userId == userID).SingleOrDefault();
        }

        [HttpGet]
        [ActionName("getmembers")]
        [Route("household/getmembers/{id}")]
        public IEnumerable<HouseholdMember> GetMembers(string id)
        {
            return this.context.HouseholdMembers.Where(x => x.householdId == id);
        }

        [HttpPost]
        [ActionName("create")]
        [Route("household/create")]
        [Authorize]
        public async Task<HttpResponseMessage> CreateHousehold()
        {
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            if (userInfo != null)
            {
                User creator = this.context.Users.Where(x => x.IDPUserID == (userInfo.providerType + ":" + userInfo.IDPUserId)).SingleOrDefault();

                if (creator == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        new { Message = "No matching user registered. Please register and try again." });
                }

                Household newHH = new Household();

                //create household info
                newHH.Id = Guid.NewGuid().ToString();
                newHH.name = "New Household";
                newHH.landlordIDP = creator.IDPUserID;
                newHH.landlordName = creator.firstName + " " + creator.lastName;
                newHH.currencyName = "Karma Points";
                newHH.description = "Welcome to the household!";

                //create new member (caller)
                HouseholdMember newMember = new HouseholdMember();
                newMember.Id = Guid.NewGuid().ToString();
                newMember.isLandlord = true;
                newMember.isApproveVote = false;
                newMember.isEvictVote = false;
                newMember.isLandlordVote = false;
                newMember.karma = 0;
                newMember.firstName = creator.firstName;
                newMember.lastName = creator.lastName;
                newMember.status = Status.Approved;
                newMember.userId = creator.Id;
                newMember.householdId = newHH.Id;

                //add relationships
                creator.memberships.Add(newMember);
                newHH.members.Add(newMember);

                this.context.Households.Add(newHH);
                this.context.HouseholdMembers.Add(newMember);
                this.context.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new {message = "Congratulations! You created a new household " +
                    "and you are the new landlord! This new household will appear in Your Households list."});
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    { Message = "Household creation failed. Could not contact authentication IDP" });
            }
        }

        [HttpPost]
        [ActionName("editinfo")]
        [Route("household/editinfo")]
        public HttpResponseMessage EditHouseholdInfo([FromBody] Household updateHH)
        {
            Household existHH = this.context.Households.Where(x => x.Id == updateHH.Id).FirstOrDefault();

            string userIDP = "FB1";//need to change to caller's IDP

            if (updateHH != null)
            {
                if (existHH != null)
                {
                    if (updateHH.landlordIDP == userIDP)
                    {
                        existHH.name = updateHH.name;
                        existHH.description = updateHH.description;
                        existHH.currencyName = updateHH.currencyName;
                        this.context.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Successfully changed household info!" });
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Caller must be the current landlord to make changes." });
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "No matching households found" });
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Cannot pass a null value" });
            }
        }

    }
}
