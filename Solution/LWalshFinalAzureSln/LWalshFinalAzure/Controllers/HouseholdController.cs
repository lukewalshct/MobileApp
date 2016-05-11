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
    /// <summary>
    /// The primary role of the household controller is to get, create and update data related to 
    /// households. A household is the basic communal space that users can be a part of. The 
    /// relationship is a user can have many household memberships (HouseholdMember), and a 
    /// household can have many memberships. Two other functinalities are also associated with
    /// households- voting and messages. Household members can participate in voting and messaging
    /// which is associated with a particular household. All household members can view and participate
    /// in the voting and messaging.
    /// </summary>
    [MobileAppController]
    public class HouseholdController : ApiController
    {
        //se the context and config settings
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        /// <summary>
        /// GET request that returns a list of households that a user is a member of.
        /// </summary>
        /// <param name="id">The user's id</param>
        /// <returns>List of Households</returns>
        [HttpGet]
        [Route("household/byuser/{id}")]
        [ActionName("byuser")]
        public List<Household> GetUserHouseholds(string id)
        {
            //find the user in Azure's SQL database based on the id
            User u = this.context.Users.Where(x => x.IDPUserID == ("Facebook:" + id)).SingleOrDefault();

            if (u != null)
            {
                //return a list of households which the user is a member of
                return this.context.Households.Include("members").Where(x => x.members.Select(y => y.userId)
                    .Contains(u.Id)).ToList();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// GET request that returns a list of all households.
        /// </summary>
        /// <returns>List of Households</returns>
        [HttpGet]
        [Route("household")]
        public IEnumerable<Household> GetAllHouseholds()
        {
            return this.context.Households;
        }

        /// <summary>
        /// GET request that returns a specific household by its id.
        /// </summary>
        /// <param name="id">The household id</param>
        /// <returns>Object with data from the household</returns>
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

        /// <summary>
        /// GET request that returns a household membership based on a household id and user id.
        /// </summary>        
        /// <param name="HHID">The Household id</param>
        /// <param name="userID">The User id</param>
        /// <returns>The HouseholdMember if it exists, else null</returns>
        [HttpGet]
        [Route("household/byid/{HHID}/getmember/{userID}")]
        [ActionName("getmember")]
        public HouseholdMember GetMember (string HHID, string userID)
        {
            User u = this.context.Users.Where(x => x.IDPUserID == "Facebook:" + userID).SingleOrDefault();
            if (u != null)
            {
                return this.context.HouseholdMembers.Where(x => x.householdId == HHID && x.userId == u.Id).SingleOrDefault();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// GET request that returns list of household members for a specified household id.
        /// </summary>        
        /// <param name="id">The Household id</param>        
        /// <returns>List of the Household's members.</returns>
        [HttpGet]
        [ActionName("getmembers")]
        [Route("household/getmembers/{id}")]
        public IEnumerable<HouseholdMember> GetMembers(string id)
        {
            return this.context.HouseholdMembers.Where(x => x.householdId == id);
        }

        /// <summary>
        /// POST action that creates a new household. This is called from the client app's home screen when
        /// the user would like to create a new household.
        /// </summary>        
        /// <returns>HttpResponseMessage indicating success or failure</returns>
        [HttpPost]
        [ActionName("create")]
        [Route("household/create")]
        [Authorize]
        public async Task<HttpResponseMessage> CreateHousehold()
        {
            //get the user's info from the Facebook graph to ensure the user exists
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            if (userInfo != null)
            {
                //get the user from Azure's SQL database
                User creator = this.context.Users.Where(x => x.IDPUserID == (userInfo.providerType + ":" + userInfo.IDPUserId)).SingleOrDefault();

                //Deny the household creation if the user isn't in the database
                if (creator == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest,
                        new { Message = "No matching user registered. Please register and try again." });
                }


                //create a new household with default values. By default the current user is 
                //set as the landlord of the household (landlord can edit household info)

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
                newMember.IDPUserId = "Facebook:" + userInfo.IDPUserId;
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

                //save changes in the database
                this.context.Households.Add(newHH);
                this.context.HouseholdMembers.Add(newMember);
                this.context.SaveChanges();

                //return success message
                return Request.CreateResponse(HttpStatusCode.OK, new {message = "Congratulations! You created a new household " +
                    "and you are the new landlord! This new household will appear in Your Households list."});
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                    { Message = "Household creation failed. Could not contact authentication IDP" });
            }
        }

        /// <summary>
        /// POST action that updates the basic info about a household such as the household's name,
        /// a description, and the name of the currency. This method is called from a household's 
        /// home screen to allow the landlord to edit these fields. Only the landlord may edit these
        /// fields.
        /// </summary>        
        /// <param name="updateHH">The Household to be updated</param>        
        /// <returns>HttpResponseMessage indicating success or failure</returns>
        [HttpPost]
        [ActionName("editinfo")]
        [Route("household/editinfo")]
        [Authorize]
        public async Task<HttpResponseMessage> EditHouseholdInfo([FromBody] Household updateHH)
        {
            //get the existing household from the Azure SQL database
            Household existHH = this.context.Households.Where(x => x.Id == updateHH.Id).FirstOrDefault();
            
            //get the user's info from Facebook's graph
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            if (updateHH != null)
            {
                if (existHH != null)
                {
                    //if the user is the landlord, update the household info and return success message
                    if (updateHH.landlordIDP == "Facebook:" + userInfo.IDPUserId)
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
