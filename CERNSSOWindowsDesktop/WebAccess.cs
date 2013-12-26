using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CERNSSO
{
    /// <summary>
    /// Static methods to enable web requests to CERN protected sites.
    /// </summary>
    public static class WebAccess
    {
        /// <summary>
        /// Called to prepare the web request before it is sent out the first time.
        /// This is called for every single web request that we ask for in this
        /// library.
        /// </summary>
        public static Action<HttpRequestMessage> gPrepWebRequest = null;

        /// <summary>
        /// After calling this future web accesses will use this username and password
        /// to access web resources. All previous login information will be erased, along
        /// with pre-authenticated sites.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <remarks>
        /// The current version of the library can't access two different web sites with different
        /// login credentials.
        /// </remarks>
        public static void LoadUsernamePassword(string username, string password)
        {
            // We need to assure full interaction with the ASP.NET web service back-end. So simulate Chrome,
            // which happens to be the browser we used to debug this interaction.
            gPrepWebRequest = reqMsg =>
            {
                reqMsg.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.63 Safari/537.36");
            };
        }

        /// <summary>
        /// Clears out all information we've cached. This is useful mainly for testing
        /// to make sure the state of the library is reset.
        /// </summary>
        public static void ResetCredentials()
        { }

        /// <summary>
        /// We are given a prepared HTTP web request. Fetch a working response.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> GetWebResponse(Uri requestUri)
        {
            // Run the web request. Perhaps we will get lucky. We know we are lucky if one
            // of the authentication guys doesn't come back, or the resource that responds
            // is the resource we asked for information from.

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            PrepWebRequest(request);
            var hc = new HttpClient();
            var response = await hc.SendAsync(request);

            // First level detection - if this request went well, then we can pass back
            // everything here. Otherwise, see if we can detect something that requires us
            // to log in.

            var responseUri = response.RequestMessage.RequestUri;
            if (responseUri.OriginalString == requestUri.OriginalString)
                return response;
            if (!responseUri.IsCERNSSOAuthUri())
                return response;

            return response;
#if false
            var response = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            if (response.ResponseUri.OriginalString == request.RequestUri.OriginalString)
                return response;
            if (!response.ResponseUri.IsCERNSSOAuthUri())
                return response;
#endif
            throw new NotImplementedException();
        }

        /// <summary>
        /// Call the prep web request delegate.
        /// </summary>
        /// <param name="request"></param>
        private static void PrepWebRequest(HttpRequestMessage request)
        {
            if (gPrepWebRequest != null)
                gPrepWebRequest(request);
        }

    }
}
