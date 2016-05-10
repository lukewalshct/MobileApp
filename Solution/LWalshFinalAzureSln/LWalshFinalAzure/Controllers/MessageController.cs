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
    [ApiExplorerSettings(IgnoreApi = false)]
    public class MessageController : MessageBaseController
    {
        MobileServiceContext context = new MobileServiceContext();
        public MobileAppSettingsDictionary ConfigSettings => Configuration.GetMobileAppSettingsProvider().GetMobileAppSettings();

        /// <summary>
        /// Gets all of the people in the collection
        /// </summary>
        /// <returns>ArrayList.</returns>
        [Authorize]
        public ArrayList Get()
        {
            ArrayList heterogeneousMessageList = new ArrayList();

            // Define a SQL query to return all of the people
            var messages = DocumentClientInstance.CreateDocumentQuery(DocumentCollectionInstance.DocumentsLink, "SELECT * FROM MessageCollection M");

            // Add each person to the result array list
            foreach (var message in messages)
            {
                heterogeneousMessageList.Add(message);
            }

            // Return the result set of people
            return heterogeneousMessageList;

        }


        /// <summary>
        /// Gets person specified by the id
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.Web.Http.HttpResponseException"></exception>
        [Authorize]
        public object Get(string id)
        {
            // Workaround Note as of May 2015: FirstOrDefault is not directly supported by the LINQ to DocumentDB provider so we have to use AsEnumerable().FirstOrDefault()
            // Define a query to get a person by their ID using Linq
            //var message = (from m in DocumentClientInstance.CreateDocumentQuery(DocumentCollectionInstance.DocumentsLink)
            //              where m.hhid == id
            //              select m).AsEnumerable().FirstOrDefault();
            ArrayList heterogeneousMessageList = new ArrayList();

            var messages = DocumentClientInstance.CreateDocumentQuery(DocumentCollectionInstance.DocumentsLink, 
                "SELECT * FROM MessageCollection M WHERE M.hhid = '" + id + "'");


            // If we found a person return it
            // Add each person to the result array list
            foreach (var message in messages)
            {
                heterogeneousMessageList.Add(message);
            }

            return heterogeneousMessageList;

            // Let the caller know the resource for the ID could not be found.
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.NotFound, $"Person {id} not found."));

        }


        /// <summary>
        /// Creates the specified person
        /// </summary>
        /// <param name="person">The person.</param>
        /// <returns>Task&lt;IHttpActionResult&gt;.</returns>
        /// <exception cref="HttpResponseException"></exception>
        [ResponseType(typeof(Message))]
        [Authorize]
        public async Task<IHttpActionResult> Post([FromBody]Message message)
        {
            Document result = null;
                        
            try
            {
                IDPTransaction idpTransaction = new IDPTransaction(this.Request, this.ConfigSettings, this.Configuration);
                ExtendedUserInfo userInfo = await idpTransaction.GetIDPInfo();

                LWalshFinalAzure.DataObjects.User user = null;

                if (userInfo != null)
                {
                    user = this.context.Users.Where(x => x.IDPUserID == (userInfo.providerType + ":" + userInfo.IDPUserId)).SingleOrDefault();
                }
                if (message != null && user != null)
                {
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

        // PUT api/values/5
        /// <summary>
        /// Updates the person
        /// </summary>
        /// <param name="id"></param>
        /// <param name="person"></param>
        /// <returns></returns>
        [Authorize]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> Put(string id, [FromBody]Message message)
        {
            await DocumentClientInstance.ReplaceDocumentAsync(DocumentCollectionInstance.DocumentsLink, message);

            return StatusCode(HttpStatusCode.NoContent);
        }
        

        /// <summary>
        /// Deletes the specified person.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>IHttpActionResult.</returns>
        //public IHttpActionResult Delete(string id)
        //{
        //    var message = (from m in DocumentClientInstance.CreateDocumentQuery(DocumentCollectionInstance.DocumentsLink)
        //                  where m.Id == id
        //                  select m).AsEnumerable().FirstOrDefault();


        //    // If we found a person delete it
        //    if (message != null)
        //    {
        //        DocumentClientInstance.DeleteDocumentAsync(message.SelfLink);
        //    }

        //    return Ok(message);
        //}
    }
}
