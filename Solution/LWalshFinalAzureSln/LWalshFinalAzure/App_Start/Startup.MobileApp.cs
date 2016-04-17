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
            HttpConfiguration config = new HttpConfiguration();
            config.Routes.MapHttpRoute(
                name: "GetMemberFromHH",
                routeTemplate: "api/{controller}/byid/{HHID}/{action}/{userID}",
                defaults: new { HHID = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "HouseholdGetUsersApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { HHID = RouteParameter.Optional }
            );
            config.Routes.MapHttpRoute(
                name: "DefaultPost",
                routeTemplate: "api/{controller}/{action}"
            );
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}"                
            );

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}",
            //    defaults: new { id = RouteParameter.Optional }
            //);
            new MobileAppConfiguration()
                .UseDefaultConfiguration()
                .ApplyTo(config);

            // Use Entity Framework Code First to create database tables based on your DbContext
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

    //public class MobileServiceInitializer : CreateDatabaseIfNotExists<MobileServiceContext>
    public class MobileServiceInitializer : DropCreateDatabaseAlways<MobileServiceContext>
    {
        protected override void Seed(MobileServiceContext context)
        {
            User me = new User
            {
                Id = Guid.NewGuid().ToString(),
                firstName = "Luke",
                lastName = "Walsh",
                IDPUserID = "FB1",
                household = "HH1"
            };
            User tim = new User
            {
                Id = Guid.NewGuid().ToString(),
                firstName = "Tim",
                lastName = "Burke",
                IDPUserID = "FB2",
                household = "HH1"
            };

            Household hh1 = new Household { Id = Guid.NewGuid().ToString(), name = "Test Household1" };
            Household hh2 = new Household { Id = Guid.NewGuid().ToString(), name = "Test Household2" };

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

            me.memberships.Add(member1);
            tim.memberships.Add(member2);            

            hh1.members.Add(member1);
            hh2.members.Add(member2);

            //hh1.users.Add(me);
            //hh2.users.Add(me);

            List<User> users = new List<User>
            {
                me,  
                tim
            };
            
            foreach (User u in users)
            {
                context.Set<User>().Add(u);
            }

            //context.Set<HouseholdMember>().Add(member1);
            //context.Set<HouseholdMember>().Add(member2);

            context.Set<Household>().Add(hh1);
            context.Set<Household>().Add(hh2);
            //context.SaveChanges();
            base.Seed(context);
        }
    }
}

