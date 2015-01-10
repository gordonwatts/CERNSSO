using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace CERNSSO
{
    /// <summary>
    /// HttpFilter to, given the proper certificate, access a document behind CERN's SSO layer.
    /// </summary>
    public class CERNSSOHttpFilter : IHttpFilter
    {
        /// <summary>
        /// Initialize the http filter to provide access to protected CERN websites.
        /// </summary>
        /// <param name="cernCert">The certificate to be used to access the CERN websites. null if no certificate should be used.</param>
        /// <param name="innerFilter">The inner filter in the filter chain. Cannot be null.</param>
        public CERNSSOHttpFilter(Certificate cernCert, IHttpFilter innerFilter)
        {
            if (innerFilter == null)
            {
                throw new ArgumentNullException("innerFilter");
            }
            InnerFilter = innerFilter;
            CERNSSOCert = cernCert;
        }

        /// <summary>
        /// Get the certificate being used to access the CERN websites.
        /// </summary>
        public Certificate CERNSSOCert { get; private set; }

        /// <summary>
        /// Get the inner filter used as the next in the chain.
        /// </summary>
        public IHttpFilter InnerFilter { get; private set; }

        /// <summary>
        /// Marks the first request, where we know we have to complete the log-in.
        /// </summary>
        bool _firstRequest = true;

        /// <summary>
        /// Internal method used by HttpClient to send a request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> SendRequestAsync(HttpRequestMessage request)
        {
            // First time through, we need to tell the system to auto-login.
            try
            {
                if (_firstRequest)
                {
#if true
                    //
                    // This is the proper way to do this. However, the request will generate auto-redirects. And, unfortunately,
                    // those auto redirects will not have the cookie, and so the login fails to happen.
                    // The only way to make the log-in work, it seems is to either run the re-directs our selves, or
                    // to have the code that sits in the #else statement.
                    //
                    var c = new HttpCookiePairHeaderValue("SSOAutologonCertificate", "true");
                    request.Headers.Cookie.Add(c);
#else
                    var cookie = new HttpCookie("SSOAutologonCertificate", ".cern.ch", "");
                    cookie.Value = "true";
                    (InnerFilter as HttpBaseProtocolFilter).CookieManager.SetCookie(cookie);
#endif
                    (InnerFilter as HttpBaseProtocolFilter).ClientCertificate = CERNSSOCert;
                }
                _firstRequest = false;

                // Now do the work.
                return InnerFilter.SendRequestAsync(request);
            }
            finally
            {
                (InnerFilter as HttpBaseProtocolFilter).ClientCertificate = null;
            }
        }

        /// <summary>
        /// Dispose of what resources we are holding on to.
        /// </summary>
        public void Dispose()
        {
            // Nothing.
        }
    }
}
