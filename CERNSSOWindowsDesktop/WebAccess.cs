using System;
using System.Net;
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
        /// </summary>
        public static Action<HttpWebRequest> gPrepWebRequest = null;

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
            gPrepWebRequest = req =>
            {
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/31.0.1650.63 Safari/537.36";
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
        public static async Task<WebResponse> FetchWebResponse(HttpWebRequest request)
        {
            // Run the web request. Perhaps we will get lucky. We know we are lucky if one
            // of the authentication guys doesn't come back, or the resource that responds
            // is the resource we asked for information from.

            PrepWebRequest(request);
            var response = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            if (response.ResponseUri.OriginalString == request.RequestUri.OriginalString)
                return response;
            if (!response.ResponseUri.IsCERNSSOAuthUri())
                return response;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Call the prep web request delegate.
        /// </summary>
        /// <param name="request"></param>
        private static void PrepWebRequest(HttpWebRequest request)
        {
            if (gPrepWebRequest != null)
                gPrepWebRequest(request);
        }

    }
}
