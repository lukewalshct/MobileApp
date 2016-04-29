using System;
using System.Net;
using System.Net.Http;
using Java.Lang;

namespace LWalshFinalClient
{
    public class HttpAutoProxyHandler : HttpClientHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpAutoProxyHandler"/> class.
        /// </summary>
        /// <param name="schemeName">Name of the scheme (http or https).</param>
        /// 
        public HttpAutoProxyHandler() { }
        public HttpAutoProxyHandler(string schemeName = "https")
        {

            // Eqv: System.getProperty(http.ProxyHost)
            // Eqv: System.getProperty(http.ProxyPort)
            string hostIpAddress = JavaSystem.GetProperty(string.Format("{0}.proxyHost", schemeName));
            string hostPort = JavaSystem.GetProperty(string.Format("{0}.proxyPort", schemeName));

            // Setup to use proxy if one is found
            if (!string.IsNullOrWhiteSpace(hostIpAddress))
            {
                // Instruct to use proxy
                UseProxy = true;

                string uriString;

                if (string.IsNullOrWhiteSpace(hostPort))
                {
                    uriString = string.Format("{0}://{1}", schemeName, hostIpAddress);
                }
                else
                {
                    uriString = string.Format("{0}://{1}:{2}", schemeName, hostIpAddress, hostPort);
                }

                Proxy = new WebProxy(new Uri(uriString));
            }
        }
    }

}