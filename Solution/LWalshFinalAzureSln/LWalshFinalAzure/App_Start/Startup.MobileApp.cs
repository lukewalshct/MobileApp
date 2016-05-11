using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;
using LWalshFinalAzure.DataObjects;
using LWalshFinalAzure.Models;
using Owin;
using System.Linq;
using System.Web.Http.Routing;
using System.Net.Http;

namespace LWalshFinalAzure
{
    public partial class Startup
    {
        public static void ConfigureMobileApp(IAppBuilder app)
        {
            //configure routing
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "GetMemberFromHH",
                routeTemplate: "api/{controller}/byid/{HHID}/{action}/{userID}",
                defaults: new { HHID = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "GetUserFriends",
                routeTemplate: "api/user/byid/{id}/friends",
                defaults: new { controller = "friend", action = "byid", id = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "GetUser",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "DefaultPost",
                routeTemplate: "api/{controller}/{action}"
            );
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}"                
            );

            new MobileAppConfiguration()
                .UseDefaultConfiguration()
                .ApplyTo(config);

            // Use Entity Framework Code First to create database tables based on DbContext
            Database.SetInitializer(new MobileServiceInitializer());

            MobileAppSettingsDictionary settings = config.GetMobileAppSettingsProvider().GetMobileAppSettings();
                
            if (string.IsNullOrEmpty(settings.HostName))
            {
                app.UseAppServiceAuthentication(new AppServiceAuthenticationOptions
                {
                    // This middleware is intended to be used locally for debugging. By default, HostName will
                    // only have a value when running in an App Service application.
                    SigningKey = ConfigurationManager.AppSettings["SigningKey"],
                    ValidAudiences = new[] { ConfigurationManager.AppSettings["ValidAudience"] },
                    ValidIssuers = new[] { ConfigurationManager.AppSettings["ValidIssuer"] },
                    TokenHandler = config.GetAppServiceTokenHandler()
                });
            }

            app.UseWebApi(config);
        }
    }

    public class MobileServiceInitializer : CreateDatabaseIfNotExists<MobileServiceContext>
    //public class MobileServiceInitializer : DropCreateDatabaseAlways<MobileServiceContext>
    {
        protected override void Seed(MobileServiceContext context)
        {
            //create list of sample users
            User me = new User
            {
                Id = Guid.NewGuid().ToString(),
                firstName = "Luke",
                lastName = "Walsh",
                IDPUserID = "FB1"
            };
            User tim = new User
            {
                Id = Guid.NewGuid().ToString(),
                firstName = "Tim",
                lastName = "Burke",
                IDPUserID = "FB2"
            };
            User eric = new User
            {
                Id = Guid.NewGuid().ToString(),
                firstName = "Eric",
                lastName = "Puffer",
                IDPUserID = "FB3"                
            };
            User matt = new User
            {
                Id = Guid.NewGuid().ToString(),
                firstName = "Matt",
                lastName = "Kuruc",
                IDPUserID = "FB4"
            };

            //create sample households
            Household hh1 = new Household { Id = Guid.NewGuid().ToString(), name = "Test Household1" };
            Household hh2 = new Household { Id = Guid.NewGuid().ToString(), name = "Test Household2" };

            //create sample household members (many to many relationship between user and household)
            HouseholdMember member1 = new HouseholdMember()
            {
                userId = me.Id,
                householdId = hh1.Id,
                Id = Guid.NewGuid().ToString(),
                firstName = me.firstName,
                lastName = me.lastName,
                status = Status.Approved,
                karma = 0,
                isLandlord = true,
                isApproveVote = false,
                isEvictVote = false,
                isLandlordVote = false
            };

            HouseholdMember member2 = new HouseholdMember()
            {
                userId = tim.Id,
                householdId = hh2.Id,
                Id = Guid.NewGuid().ToString(),
                firstName = tim.firstName,
                lastName = tim.lastName,
                status = Status.Approved,
                karma = 0,
                isLandlord = false,
                isApproveVote = false,
                isEvictVote = false,
                isLandlordVote = false
            };

            HouseholdMember member3 = new HouseholdMember()
            {
                userId = eric.Id,
                householdId = hh1.Id,
                Id = Guid.NewGuid().ToString(),
                firstName = eric.firstName,
                lastName = eric.lastName,
                status = Status.Approved,
                karma = 0,
                isLandlord = false,
                isApproveVote = false,
                isEvictVote = false,
                isLandlordVote = false
            };

            HouseholdMember member4 = new HouseholdMember()
            {
                userId = matt.Id,
                householdId = hh1.Id,
                Id = Guid.NewGuid().ToString(),
                firstName = matt.firstName,
                lastName = matt.lastName,
                status = Status.Approved,
                karma = 0,
                isLandlord = false,
                isApproveVote = false,
                isEvictVote = false,
                isLandlordVote = false
            };

            //add new users, households, and household members to the context
            me.memberships.Add(member1);
            tim.memberships.Add(member2);
            eric.memberships.Add(member3);
            matt.memberships.Add(member4);            

            hh1.members.Add(member1);
            hh2.members.Add(member2);
            hh1.members.Add(member3);
            hh1.members.Add(member4);

            List<User> users = new List<User>
            {
                me,  
                tim,
                eric,
                matt
            };
            
            foreach (User u in users)
            {
                context.Set<User>().Add(u);
            }

            context.Set<Household>().Add(hh1);
            context.Set<Household>().Add(hh2);

            //add a test vote
            Vote v = new Vote();
            v.Id = Guid.NewGuid().ToString();
            v.householdID = hh1.Id;
            v.membersVoted.Add(member1);
            v.isAnonymous = false;
            v.targetMemberID = member1.Id;
            v.voteType = VoteType.Karma;
            v.votesFor = 1;
            v.voteStatus = "In Progress";
            v.balanceChange = -100;

            hh1.votes.Add(v);
            member1.votes.Add(v);
            context.Set<Vote>().Add(v);
    
            base.Seed(context);
        }
    }
}

