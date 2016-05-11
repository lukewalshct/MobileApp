using System.Configuration;

namespace LWalshFinalAzure
{
    /// <summary>
    /// A class that sets up configuration for Azure Document DB
    /// </summary>
    public static class ConfigUtils
    {
        /// <summary>
        /// Gets the document DB URI
        /// </summary>
        /// <value>The document DB URI.</value>
        static public string EndpointUrl
        {
            get { return ConfigurationManager.ConnectionStrings["DocumentDBEndpointUrl"].ConnectionString; }
        }

        /// <summary>
        /// Gets the authorization key.
        /// </summary>
        /// <value>The authorization key.</value>
        static public string AuthorizationKey
        {
            get { return ConfigurationManager.ConnectionStrings["DocumentDBAuthorizationKey"].ConnectionString; }
        }


        static public string DocumentDBConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["DocumentDBConnectionString"].ConnectionString; }
        }

        /// <summary>
        /// The name of the database
        /// </summary>
        public const string DATABASE_NAME = "lwalshfinaldb";

        /// <summary>
        /// The name of the collection
        /// </summary>
        public const string COLLECTION_NAME = "messages";

    }
}
