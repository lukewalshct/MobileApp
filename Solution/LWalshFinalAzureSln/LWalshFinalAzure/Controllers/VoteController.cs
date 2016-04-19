using System.Web.Http;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.Models;
using LWalshFinalAzure.DataObjects;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System;

namespace LWalshFinalAzure.Controllers
{
    [MobileAppController]
    public class VoteController : ApiController
    {
        MobileServiceContext context = new MobileServiceContext();

        [HttpGet]
        [Route("vote/byhhid/{id}")]
        [ActionName("byhhid")]
        public HttpResponseMessage GetVotes(string id)
        {
            Household hh = this.context.Households.Include("votes").Include("members").Where(x => x.Id == id).SingleOrDefault();

            if (hh != null)
            {
                int votesNeeded = (int) Math.Round((((double)hh.members.Count) / 2), 0, MidpointRounding.AwayFromZero);
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
                        voteStatus = x.voteStatus
                    }));
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Cannot find household." });
            }            
        }

        [HttpPost]
        [Route("vote/newvote")]
        [ActionName("newvote")]
        public HttpResponseMessage NewVote ([FromBody] Vote v)
        {
            if (v != null)
            {
                //check to ensure that the household exists, else return bad request
                Household hh = this.context.Households.Where(x => x.Id == v.householdID).SingleOrDefault();
                if (hh != null)
                {
                    //check to ensure calling and target household members exist and is part of the household
                    User u = this.context.Users.Where(x => x.IDPUserID == "FB1").SingleOrDefault(); //need to replace with FB auth
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
                        //has an active vote to make that member landlord
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
                                    Message = "Target member is already under vote for eviction"});
                            }
                            targetMember.isEvictVote = true;
                        }
                        //if new member vote, make sure the user isn't already a member
                        if(v.voteType == VoteType.NewMember && hh.members.Contains(targetMember))
                        {
                            return Request.CreateResponse(HttpStatusCode.BadRequest, new {
                                Message = "Target member is already in the household"});
                        }

                        Vote newVote = new Vote();

                        newVote.balanceChange = v.balanceChange;
                        newVote.description = v.description;
                        newVote.householdID = v.householdID;
                        newVote.Id = Guid.NewGuid().ToString();
                        newVote.isAnonymous = v.isAnonymous;
                        newVote.membersVoted.Add(callMember);
                        newVote.targetMemberID = v.targetMemberID;
                        newVote.voteType = v.voteType;
                        newVote.votesFor = 1;
                        newVote.votesAgainst = 0;

                        hh.votes.Add(newVote);
                        callMember.votes.Add(newVote);
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

        [HttpPost]
        [Route("vote/castvote")]
        [ActionName("castvote")]
        public HttpResponseMessage CastVote([FromBody] VoteCast vc)
        {
            //check to ensure votecast is not null
            if (vc == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Denied. Cannot pass a null value" });
            }

            User u = this.context.Users.Where(x => x.IDPUserID == "FB3").SingleOrDefault(); //need to replace with FB auth
            Vote v = this.context.Votes.Include("membersVoted").Where(x => x.Id == vc.voteId).SingleOrDefault();

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

            v.membersVoted.Add(member);

            //check to see if the vote reached its threshold
            int votesNeeded = (int)Math.Round((((double)hh.members.Count) / 2), 0, MidpointRounding.AwayFromZero);

            if(v.votesFor >= votesNeeded)
            {
                v.voteStatus = "Passed";
                //trigger push notification code

                if (v.voteType == VoteType.Karma)
                {
                    if(!karmaVotePass(hh, v))
                    {
                        return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "The vote was cast successfully but the target member no longer exists." });
                    }
                }
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Vote cast! The proposal has passed!" });
            }
            else if (v.votesAgainst >= votesNeeded)
            {
                v.voteStatus = "Failed";
                //trigger push notification code
                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Vote cast! The proposal has failed!" });
            }

            this.context.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.BadRequest, new { Message = "Vote cast!"});
        }

        private bool karmaVotePass(Household hh, Vote v)
        {
            HouseholdMember targetMember = hh.members.Where(x => x.Id == v.targetMemberID).SingleOrDefault();

            if (targetMember == null)
            {
                return false;
            }

            targetMember.karma += v.balanceChange;

            List<HouseholdMember> otherMembers = hh.members.Where(x => x.Id != v.targetMemberID).ToList();
            int numOtherMembers = hh.members.Count() - 1;
            double otherMembersKarmaChange = -1*((double)v.balanceChange) / numOtherMembers;

            foreach (HouseholdMember m in otherMembers)
            {
                m.karma += (otherMembersKarmaChange);
            }
            this.context.SaveChanges();
            return true;
        }
    }
}
