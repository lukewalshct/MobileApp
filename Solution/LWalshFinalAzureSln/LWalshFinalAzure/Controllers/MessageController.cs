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

namespace LWalshFinalAzure.Controllers
{
    [ApiExplorerSettings(IgnoreApi = false)]
    public class MessageController : MessageBaseController
    {

        /// <summary>
        /// Gets all of the people in the collection
        /// </summary>
        /// <returns>ArrayList.</returns>
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
        public async Task<IHttpActionResult> Post([FromBody]Message message)
        {
            Document result = null;

            try
            {
                // Let DocumentDB assign the Id, this is RESTfull because we are using POST
                result = await DocumentClientInstance.CreateDocumentAsync(DocumentCollectionInstance.SelfLink, message);
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
