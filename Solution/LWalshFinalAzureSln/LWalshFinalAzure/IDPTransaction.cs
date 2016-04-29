using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Facebook;
using LWalshFinalAzure.DataObjects;
using Newtonsoft.Json.Linq;

namespace LWalshFinalAzure
{
    /// <summary>
    /// A class that contains helper methods for requesting user info from an IDP. This
    /// class was created since it's the same methods being used in all controllers that 
    /// need user info from an IDP.
    /// </summary>
    public class IDPTransaction : ApiController
    {
        private MobileAppSettingsDictionary configSettings;
        private HttpRequestMessage request;
        private HttpConfiguration configuration;

        public IDPTransaction(HttpRequestMessage r, MobileAppSettingsDictionary configSettings, HttpConfiguration config)
        {
            this.request = r;
            this.configSettings = configSettings;
            this.configuration = config;
        }

        /// <summary>
        /// Retrieves the user info from the IDP.
        /// </summary>
        [Authorize]
        public async Task<ExtendedUserInfo> GetIDPInfo()
        { 
            ExtendedUserInfo extendedUserInfo = null;           
            FacebookCredentials facebookCredentials = null;
            try
            {
                facebookCredentials = await this.User.GetAppServiceIdentityAsync<FacebookCredentials>(this.request);
            }
            catch (Exception ex)
            {

            }
            if (facebookCredentials != null)
            {
                extendedUserInfo = await GetUserNameAndDescription(facebookCredentials);
                extendedUserInfo.providerType = "Facebook";
            }

            if (extendedUserInfo == null)
            {
                return null;
            }
            else
            {
                return extendedUserInfo;
            }
        }

        
        /// <summary>
        /// Gets the Facebook user name, gender and description.
        /// </summary>
        /// <param name="facebookCredentials">The facebook credentials.</param>
        /// <returns>Task&lt;ExtendedUserInfo&gt;.</returns>
        private async Task<ExtendedUserInfo> GetUserNameAndDescription(FacebookCredentials facebookCredentials)
        {
            ExtendedUserInfo externExtendedUserInfo = new ExtendedUserInfo();

            if (facebookCredentials == null)
            {
                return externExtendedUserInfo;
            }

            string userID = (string)facebookCredentials.Claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];
            Uri facebookUri = new Uri("https://graph.facebook.com/v2.5/" + userID);

            FacebookClient fb = new FacebookClient(facebookCredentials.AccessToken);
            try
            {
                JsonObject user = await fb.GetTaskAsync("me?fields=name,id,gender") as JsonObject;
                externExtendedUserInfo.Name = (string)user["name"];
                externExtendedUserInfo.Gender = (string)user["gender"];
                externExtendedUserInfo.IDPUserId = (string)user["id"];
                externExtendedUserInfo.pictureURL = "graph.facebook.com/" + facebookCredentials.AccessToken + "/picture";
            }
            catch (Exception ex)
            {

            }

            return externExtendedUserInfo;
        }        
        [Authorize]
        public async Task<Friends> getUserFriends()
        {
            FacebookCredentials facebookCredentials = null;
            try
            {
                facebookCredentials = await this.User.GetAppServiceIdentityAsync<FacebookCredentials>(this.request);
            }
            catch (Exception ex)
            {

            }
            if (facebookCredentials != null)
            {
                FacebookClient fb = new FacebookClient(facebookCredentials.AccessToken);
                try
                {
                    JsonObject result = await fb.GetTaskAsync("me/friends") as JsonObject;
                    JArray resultFriends = (JArray) result["data"];
                    List<FacebookFriend> friends = new List<FacebookFriend>();

                    foreach(JToken rFriend in resultFriends)
                    {
                        FacebookFriend f = new FacebookFriend();
                        f.id = (string) rFriend["id"];
                        f.name = (string) rFriend["name"];
                        friends.Add(f);
                    }
                                       
                    Friends userFriends = new Friends();
                    userFriends.friends = friends;

                    return userFriends;
                }
                catch (Exception ex)
                {

                }
            }
            return null;
        }
    }
}