using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace LWalshFinalAzure.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MessageBaseController : ApiController
    {

        /// <summary>
        /// The _document client
        /// </summary>
        protected DocumentClient DocumentClientInstance;

        /// <summary>
        /// The _document database database
        /// </summary>
        protected Database DocumentDbDatabaseInstance;

        /// <summary>
        /// The _document collection
        /// </summary>
        protected DocumentCollection DocumentCollectionInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonController"/> class.
        /// </summary>
        public MessageBaseController()
        {
            // 1. Create the document client used for interacting with the DocumentDB 
            DocumentClientInstance = new DocumentClient(new Uri(ConfigUtils.EndpointUrl), ConfigUtils.AuthorizationKey);

            // 2. If the database does not exist, create it
            CreateDatabaseIfNotExist();

            // 3. If the collection does not exist, create it
            CreateCollectionIfNotExist();
        }

        /// <summary>
        /// Creates the database if it does not exist.
        /// </summary>
        private void CreateDatabaseIfNotExist()
        {
            DocumentDbDatabaseInstance =
                DocumentClientInstance.CreateDatabaseQuery()
                    .Where(db => db.Id == ConfigUtils.DATABASE_NAME)
                    .AsEnumerable()
                    .FirstOrDefault();

            // If the database does not exist, create it.
            if (DocumentDbDatabaseInstance == null)
            {
                // 2a. Create the database
                DocumentDbDatabaseInstance = DocumentClientInstance.CreateDatabaseAsync(new Database { Id = ConfigUtils.DATABASE_NAME }).Result;

                System.Diagnostics.Trace.TraceInformation("Created the database: {0} because it was not found.",
                    ConfigUtils.DATABASE_NAME);
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation("Found the database: {0}, no database was needed to be created.",
                    ConfigUtils.DATABASE_NAME);
            }
        }

        /// <summary>
        /// Creates the collection if it does not exist.
        /// </summary>
        private void CreateCollectionIfNotExist()
        {
            DocumentCollectionInstance =
                DocumentClientInstance.CreateDocumentCollectionQuery(DocumentDbDatabaseInstance.CollectionsLink)
                    .Where(col => col.Id == ConfigUtils.COLLECTION_NAME)
                    .AsEnumerable()
                    .FirstOrDefault();

            // If the collection does not exist, create it
            if (DocumentCollectionInstance == null)
            {
                // 3a. Create the collection
                DocumentCollectionInstance =
                    DocumentClientInstance.CreateDocumentCollectionAsync(DocumentDbDatabaseInstance.CollectionsLink,
                        new DocumentCollection { Id = ConfigUtils.COLLECTION_NAME }).Result;

                System.Diagnostics.Trace.TraceInformation("Created the collection: {0} because it was not found.",
                    ConfigUtils.COLLECTION_NAME);
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation("Found the collection: {0}, no database was needed to be created.",
                    ConfigUtils.COLLECTION_NAME);
            }

            SetupIndexing();

        }

        /// <summary>
        /// Setups the indexing options.
        /// </summary>
        protected virtual void SetupIndexing()
        {
            System.Diagnostics.Trace.TraceInformation("Default: IndexingPolicy.IndexingMode: {0}", DocumentCollectionInstance.IndexingPolicy.IndexingMode);
            System.Diagnostics.Trace.TraceInformation("Default: IndexingPolicy.Automatic: {0}", DocumentCollectionInstance.IndexingPolicy.Automatic);

            // DEMO
            // Set the indexing mode to Lazy (indexing occurs asynchronously with respect to Create, Update, Delete
            //DocumentCollectionInstance.IndexingPolicy.IndexingMode = IndexingMode.Lazy;

            // DEMO
            // Set the indexing policy to manual (indexing does not happen for documents added to the collection automatically)
            //DocumentCollectionInstance.IndexingPolicy.Automatic = false;


            // DEMO Excluding a path from indexing
            //DocumentCollectionInstance.IndexingPolicy.ExcludedPaths.Add("/\"Metadata\"/*");

            // DEMO Specifying a Rang index on the timestamp property
            //DocumentCollectionInstance.IndexingPolicy.IncludedPaths.Add(new IndexingPath
            //{
            //    IndexType = IndexType.Range,
            //    Path = @"/""MemberSinceTimeStamp""/?",
            //    NumericPrecision = 7
            //});

            System.Diagnostics.Trace.TraceInformation("Current: IndexingPolicy.IndexingMode: {0}", DocumentCollectionInstance.IndexingPolicy.IndexingMode);
            System.Diagnostics.Trace.TraceInformation("Current: IndexingPolicy.Automatic: {0}", DocumentCollectionInstance.IndexingPolicy.Automatic);
        }

    }
}
