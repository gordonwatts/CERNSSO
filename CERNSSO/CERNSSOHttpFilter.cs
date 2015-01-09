using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

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
        public CERNSSOHttpFilter(Certificate cernCert)
        {

        }

        public CERNSSOHttpFilter()
        {
            // TODO: Complete member initialization
        }

        /// <summary>
        /// Get the certificate being used to access the CERN websites.
        /// </summary>
        public Certificate CERNSSOCert { get; private set; }

        /// <summary>
        /// Hold onto the base http filter. We use it as plain as plain can be.
        /// </summary>
        private HttpBaseProtocolFilter _baseFilter = null;

        /// <summary>
        /// Internal method used by HttpClient to send a request.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public IAsyncOperationWithProgress<HttpResponseMessage, HttpProgress> SendRequestAsync(HttpRequestMessage request)
        {
            // First time through, make sure we are sending the proper header. Setup the internal filter.
            if (_baseFilter == null)
            {
                _baseFilter = new HttpBaseProtocolFilter();
                if (CERNSSOCert != null)
                {
                    var cookie = new HttpCookie("SSOAutologonCertificate", ".cern.ch", "");
                    cookie.Value = "true";
                    _baseFilter.CookieManager.SetCookie(cookie);
                    _baseFilter.ClientCertificate = CERNSSOCert;
                }
            }

            // Now do the work.
            return _baseFilter.SendRequestAsync(request);
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
