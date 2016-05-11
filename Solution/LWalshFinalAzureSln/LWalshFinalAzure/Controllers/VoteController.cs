using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.Models;
using LWalshFinalAzure.DataObjects;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System;
using Microsoft.Azure.Mobile.Server;
using System.Threading.Tasks;

namespace LWalshFinalAzure.Controllers
{
    /// <summary>
    /// The primary role of the vote controller is to allow users to view, vote, and create proposals
    /// in the household. A "vote"/"proposal" relates to changes in membership, user karma, and the landlord. 
    /// For example, if a user wanted to join a household, there's a "Join" button on the client app that will
    /// create a new vote (NewMember type) via this controller. The new vote will then appear in the list of 
    /// votes for the households, available for all household members to view. Votes must be approved by a majority
    /// of the existing members in order to pass. The current version of 
    /// this app allows for new member votes and karma change votes. Future releases will allow votes on 
    /// electing a landlord and evicting existing members.
    /// </summary>
    [MobileAppController]
    public class VoteController : ApiController
    {
        //set context and config settings
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();


        /// <summary>
        /// GET method that returns all the votes for a household. This is called by the client's "Voting"
        /// screen to disply all votes to the user.
        /// </summary>
        /// <param name="id">The household id</param>
        /// <returns>HttpResponseMessage containing votes or reason for failure</returns>
        [HttpGet]
        [Route("vote/byhhid/{id}")]
        [ActionName("byhhid")]
        public HttpResponseMessage GetVotes(string id)
        {
            //get the specified household
            Household hh = this.context.Households.Include("votes").Include("members").Include("votes.membersVoted").Where(x => x.Id == id).SingleOrDefault();

            if (hh != null)
            {
                //calculate the votes needed
                int votesNeeded = (int) Math.Round((((double)hh.members.Count) / 2), 0, MidpointRounding.AwayFromZero);

                if (votesNeeded != hh.members.Count)
                {
                    votesNeeded += 1;
                }
                

                //return the list of votes in the household
                return Request.CreateResponse(HttpStatusCode.OK,
                    hh.votes.Select(x => new
                    {
                        id = x.Id,
                        householdID = x.householdID,
                        voteType = x.voteType,
                        targetMemberID = x.targetMemberID,                        
                        balanceChange = x.balanceChange,
                        isAnonymous = x.isAnonymous,
                        description = x.description,
                        votesFor = x.votesFor,
                        votesAgainst = x.votesAgainst,
                        votesNeeded = votesNeeded,
                        voteStatus = x.voteStatus,
                        targetMemberName = x.targetMemberName,
                        membersVoted = x.membersVoted.ToList().Select(y => y.Id).ToList()                        
                    }));
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Cannot find household." });
            }            
        }

        /// <summary>
        /// POST action that creates a new vote for a household.
        /// </summary>
        /// <param name="v">The new vote</param>
        /// <returns>HttpResponseMessage indicating success or failure</returns>
        [HttpPost]
        [Route("vote/newvote")]
        [ActionName("newvote")]
        [Authorize]
        public async Task<HttpResponseMessage> NewVote ([FromBody] Vote v)
        {
            if (v != null)
            {
                //check to ensure that the household exists, else return bad request
                Household hh = this.context.Households.Include("votes").Where(x => x.Id == v.householdID).SingleOrDefault();
                if (hh != null)
                {
                    //check to ensure calling and target household members exist and is part of the household
                    IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
                    ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

                    //look up the user is in the household based on their Facebook credentials
                    User u = this.context.Users.Where(x => x.IDPUserID == "Facebook:" + userInfo.IDPUserId).SingleOrDefault(); 
                    HouseholdMember callMember = null;
                    HouseholdMember targetMember = null;
                    if (u != null)
                    {
                        callMember = this.context.HouseholdMembers.Where(x => x.userId == u.Id && x.householdId == hh.Id).SingleOrDefault();
                        targetMember = this.context.HouseholdMembers.Where(x => x.userId == v.targetMemberID && x.householdId == hh.Id).SingleOrDefault();
                    }
                    if ((v.voteType == VoteType.NewMember) || (u != null && callMember != null && targetMember != null))
                    {
                        //If landlord vote, ensure that the member isn't already the landlord or 
                        //has an active vote to make that member landlord (to avoid duplicate votes with the same purpose)
                        if (v.voteType == VoteType.Landlord)
                        {
                            if (targetMember.isLandlord || targetMember.isLandlordVote)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message =
                                    "Target member is already the landlord or has an active landlord vote" });
                            }
                            targetMember.isLandlordVote = true;                        
                        }
                        //if evict member vote, make sure the member doesn't already have an active vote for eviction
                        if (v.voteType == VoteType.EvictMember)
                        {
                            if (targetMember.isEvictVote)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new {
                                    Message = "Denied. Target member is already under vote for eviction."});
                            }
                            targetMember.isEvictVote = true;
                        }
                        //if new member vote, make sure the user isn't already a member or has a join vote in progress
                        if(v.voteType == VoteType.NewMember)
                        {
                            if (hh.members.Contains(targetMember))
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                                {
                                    Message = "Denied. Member is already in the household."
                                });
                            }

                            //retrieve a list of new member votes in progress for the household in which the 
                            //user is the target
                            List<Vote> newMemberVotes = hh.votes.Where(x => x.voteType == VoteType.NewMember &&
                                x.targetMemberID == v.targetMemberID && x.voteStatus == "In Progress").ToList();

                            //if the user already requested to join the household and has a vote in progress, return error
                            if (newMemberVotes.Count > 0)
                            {
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new
                                {
                                    Message = "You already requested to join the household and a vote is in progress." +
                                        " All members must approve your membership."
                                });
                            }
                        }

                        //else create a new vote
                        Vote newVote = new Vote();

                        newVote.balanceChange = v.balanceChange;
                        newVote.description = v.description;
                        newVote.householdID = v.householdID;
                        newVote.Id = Guid.NewGuid().ToString();
                        newVote.isAnonymous = v.isAnonymous;
                        newVote.membersVoted.Add(callMember);
                        newVote.targetMemberID = v.targetMemberID;
                        newVote.targetMemberName = v.targetMemberName;
                        newVote.voteType = v.voteType;
                        newVote.voteStatus = "In Progress";
                        if (v.voteType == VoteType.NewMember)
                        {
                            newVote.votesFor = 0;
                        }
                        else
                        {
                            newVote.votesFor = 1;
                        }
                        newVote.votesAgainst = 0;

                        //add the vote to the household and save changes
                        hh.votes.Add(newVote);
                        if (callMember != null)
                        {
                            callMember.votes.Add(newVote);
                        }
                        this.context.Votes.Add(newVote);

                        this.context.SaveChanges();

                        return Request.CreateResponse(HttpStatusCode.OK, v);                                                                   
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Could not verify household members status." });
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Cannot find household." });
                }

            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Cannot pass a null value" });
            }
        }

        /// <summary>
        /// POST action that applies a household member's vote to an existing proposal/vote. 
        /// Each member is only given one vote per proposal and may not take back their vote.
        /// </summary>
        /// <param name="vc">The member's vote</param>
        /// <returns>HttpResponseMessage indicating success or failure</returns>
        [HttpPost]
        [Route("vote/castvote")]
        [ActionName("castvote")]
        [Authorize]
        public async Task<HttpResponseMessage> CastVote([FromBody] VoteCast vc)
        {
            //check to ensure votecast is not null
            if (vc == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Cannot pass a null value" });
            }
            //get user's info from the Facebook graph
            IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
            ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

            //find the user and exisint vote in the Azure SQL database
            User u = this.context.Users.Where(x => x.IDPUserID == "Facebook:" + userInfo.IDPUserId).SingleOrDefault(); 
            Vote v = this.context.Votes.Include("membersVoted").Where(x => x.Id == vc.voteId).SingleOrDefault();

            //check to ensure the user exists in the database
            if (u == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Unable to find an existing user for the id" });
            }
            //check to ensure the vote exists
            if (v == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Unable to find an existing vote for the id" });
            }

            //get the household the vote is in
            Household hh = this.context.Households.Include("members").Where(x => x.votes.Select(y => y.Id).ToList().Contains(v.Id)).SingleOrDefault();

            //check the household exists
            if (hh == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Unable to find an existing household for the vote" });
            }

            //get the household member of the user in that household
            HouseholdMember member = hh.members.Where(x => x.userId == u.Id).SingleOrDefault();

            //check that the member exists
            if(member == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. The user is not a member of the household" });
            }

            string memberVote = vc.vote.ToLower();

            //ensure the member did not vote already
            if(v.membersVoted.Contains(member))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. The member already cast his or her vote" });
            }
            
            //ensure the vote is either for or against, and cast vote          
            if (memberVote == "for")
            {
                v.votesFor++;                
            }   
            else if (memberVote == "against")
            {
                v.votesAgainst++;
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. " + vc.vote + " is an invalid vote" });
            }

            //add the voting member to the list of members that voted for this vote
            v.membersVoted.Add(member);

            //check to see if the vote reached its threshold
            int votesNeeded = (int)Math.Round((((double)hh.members.Count) / 2), 0, MidpointRounding.AwayFromZero);

            if (votesNeeded != hh.members.Count)
            {
                votesNeeded += 1;
            }

            if(v.votesFor >= votesNeeded)
            {
                v.voteStatus = "Passed";
                //trigger push notification code (future release)
                
                //if karma vote, increment/decrement the target member's balance and adjust
                //other members' balances accordingly to maintain net 0 balance
                if (v.voteType == VoteType.Karma)
                {
                    if(!karmaVotePass(hh, v))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "The vote was cast successfully but the target member no longer exists." });
                    }
                }
                //if it's a new member vote call method to add the user to the household
                else if (v.voteType == VoteType.NewMember)
                {
                    if(!newMemberVotePass(hh, v))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "The vote was cast successfully but the target user no longer exists." });
                    }
                }

                //save changes and return success message
                this.context.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Vote cast! The proposal has passed!" });
            }
            else if (v.votesAgainst >= votesNeeded)
            {
                v.voteStatus = "Failed";
                //trigger push notification code (future release)

                //save changes and send successful vote cast message
                this.context.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Vote cast! The proposal has failed!" });
            }

            this.context.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.OK, new { Message = "Vote cast!"});
        }

        /// <summary>
        /// Private helper method to the vote cast method above that increases or decreases
        /// the target member's balance by the proposed amount, and adds/subtracts the difference
        /// from the remaining members' balances to maintain a roughly net 0 balance for the household.
        /// </summary>
        /// <param name="hh">The Household</param>
        /// <param name="v">The Vote</param>
        /// <returns>Bool indicating if the balance transactions were successful</returns>
        private bool karmaVotePass(Household hh, Vote v)
        {
            //get the target member
            HouseholdMember targetMember = hh.members.Where(x => x.userId == v.targetMemberID).SingleOrDefault();

            if (targetMember == null)
            {
                return false;
            }

            //adjust target member's balance
            targetMember.karma += v.balanceChange;

            //adjust the other household members' balances
            List<HouseholdMember> otherMembers = hh.members.Where(x => x.Id != targetMember.Id).ToList();
            int numOtherMembers = otherMembers.Count();
            double otherMembersKarmaChange = -1*((double)v.balanceChange) / numOtherMembers;

            foreach (HouseholdMember m in otherMembers)
            {
                m.karma += (otherMembersKarmaChange);
            }
            this.context.SaveChanges();
            return true;
        }

        /// <summary>
        /// Private helper method to the vote cast method above that adds the user to the 
        /// household as a new member.' balances to maintain a roughly net 0 balance for the household.
        /// </summary>
        /// <param name="hh">The Household</param>
        /// <param name="v">The Vote</param>
        /// <returns>Bool indicating if the new member was added successfully</returns>
        private bool newMemberVotePass(Household hh, Vote v)
        {
            //get the target user
            User targetUser = this.context.Users.Where(x => x.IDPUserID == v.targetMemberID).SingleOrDefault();

            if (targetUser == null)
            {
                return false;
            }

            //create new member 
            HouseholdMember newMember = new HouseholdMember();
            newMember.IDPUserId = "Facebook:" + targetUser.IDPUserID;
            newMember.Id = Guid.NewGuid().ToString();
            newMember.isLandlord = false;
            newMember.isApproveVote = false;
            newMember.isEvictVote = false;
            newMember.isLandlordVote = false;
            newMember.karma = 0;
            newMember.firstName = targetUser.firstName;
            newMember.lastName = targetUser.lastName;
            newMember.status = Status.Approved;
            newMember.userId = targetUser.Id;
            newMember.householdId = hh.Id;

            //add relationships and save changes
            targetUser.memberships.Add(newMember);
            hh.members.Add(newMember);            
            this.context.HouseholdMembers.Add(newMember);
            this.context.SaveChanges();
            return true;
        }
    }
}
