using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using LWalshFinalAzure.DataObjects;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.Mobile.Server;
using LWalshFinalAzure.Models;

namespace LWalshFinalAzure.Controllers
{
    /// <summary>
    /// The primary role of the message controller is to store and retrieve messages for each
    /// household in Azure Document DB. Household members can create/post and view messages for
    /// a household, which are viewable to the entire household.
    /// </summary>
    [ApiExplorerSettings(IgnoreApi = false)]
    public class MessageController : MessageBaseController
    {
        //set up the context and configsettings
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        /// <summary>
        /// GET request that returns all of the messages in the collection
        /// </summary>
        /// <returns>ArrayList of messages</returns>
        [Authorize]
        public ArrayList Get()
        {
            ArrayList heterogeneousMessageList = new ArrayList();

            // Define a SQL query to return all of the messages
            var messages = DocumentClientInstance.CreateDocumentQuery(DocumentCollectionInstance.DocumentsLink, "SELECT * FROM MessageCollection M");

            // Add each message to the result array list
            foreach (var message in messages)
            {
                heterogeneousMessageList.Add(message);
            }

            // Return the result set of people
            return heterogeneousMessageList;

        }


        /// <summary>
        /// GET request that returns all messages for a specified household.
        /// </summary>
        /// <param name="id">The household id.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        [Authorize]
        public object Get(string id)
        {
            // Workaround Note as of May 2015: FirstOrDefault is not directly supported by the LINQ to DocumentDB provider so we have to use AsEnumerable().FirstOrDefault()
            // Define a query to get a person by their ID using Linq
            ArrayList heterogeneousMessageList = new ArrayList();
                        
            var messages = DocumentClientInstance.CreateDocumentQuery(DocumentCollectionInstance.DocumentsLink, 
                "SELECT * FROM MessageCollection M WHERE M.hhid = '" + id + "'");

            // Add each message to the result array list
            foreach (var message in messages)
            {
                heterogeneousMessageList.Add(message);
            }

            return heterogeneousMessageList;

            // Let the caller know the resource for the ID could not be found.
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Household {id} not found."));

        }


        /// <summary>
        /// POST action that creates a new message in Azure Document DB
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task&lt;IHttpActionResult&gt;.</returns>
        /// <exception cref="HttpResponseException"></exception>
        [ResponseType(typeof(Message))]
        [Authorize]
        public async Task<IHttpActionResult> Post([FromBody]Message message)
        {
            Document result = null;
                        
            try
            {
                //get the user info from Facebook's graph
                IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
                ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

                LWalshFinalAzure.DataObjects.User user = null;

                if (userInfo != null)
                {
                    //find the user in Azure SQL database
                    user = this.context.Users.Where(x => x.IDPUserID == (userInfo.providerType + ":" + userInfo.IDPUserId)).SingleOrDefault();
                }
                if (message != null && user != null)
                {
                    //create a new message and store it in Azure Document DB
                    Message newMessage = new Message();
                    newMessage.hhid = message.hhid;
                    newMessage.memberName = user.firstName;
                    newMessage.message = message.message;
                    newMessage.userid = message.userid;
                    newMessage.timeStamp = message.timeStamp;                    

                    // Let DocumentDB assign the Id, this is RESTfull because we are using POST
                    result = await DocumentClientInstance.CreateDocumentAsync(DocumentCollectionInstance.SelfLink, newMessage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceInformation("Failed to create the message {0}", ex.Message);

                throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.InternalServerError, $"Exception {ex.Message}"));
            }

            return CreatedAtRoute("DefaultApi", new { id = result.Id }, result);
        }        
    }
}
