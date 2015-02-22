using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using Windows.Web.Http.Headers;

namespace CERNSSO
{
    /// <summary>
    /// Static methods to enable web requests to CERN protected sites (behind their SSO wall).
    /// </summary>
    public static class WebAccess
    {
        /// <summary>
        /// Called to create the HTTP handler. If null, then the default is created.
        /// </summary>
        /// <remarks>Don't set the cookie container. They will be over-written.</remarks>
        private static Func<HttpClient> gCreateHttpHandler = null;

        /// <summary>
        /// Set to true so we know when we have credential information.
        /// </summary>
        private static bool gCredentialInformationValid = false;

        /// <summary>
        /// We will be loading a windows store certificate on our own.
        /// </summary>
        /// <param name="cert">The certificate to use for log-in at CERN</param>
        public static void LoadCertificate(Certificate cert)
        {
            if (cert == null)
            {
                throw new ArgumentNullException("cert");
            }

            // When we create a new http handler, make sure the cert and correct cookies are in there

            gCreateHttpHandler = () =>
            {
                var h = new HttpBaseProtocolFilter();
                h.ClientCertificate = cert;
                var cookie = new HttpCookie("SSOAutologonCertificate", ".cern.ch", "");
                cookie.Value = "true";
                h.CookieManager.SetCookie(cookie);

                return new HttpClient(h);
            };

            // And the rest of the system should know...
            gCredentialInformationValid = true;
        }

        /// <summary>
        /// Clears out all information we've cached.
        /// </summary>
        /// <remarks>This was added to aid in testing, mostly.</remarks>
        public static void ResetCredentials()
        {
            // Release the web access client - this contains cookies that may match
            // with the above.
            gClient = null;

            // And everything is now invalid...
            gCredentialInformationValid = false;
        }

        /// <summary>
        /// We are given a prepared HTTP web request. Fetch a working response.
        /// </summary>
        /// <param name="requestUri">The URI of the CERN SSO resource you want to fetch.</param>
        /// <returns>A response object that should have the data requested in it. Or if authentication fails, it will throw an UnauthorizedAccess exception.</returns>
        /// <remarks>If this is called against a URI that doesn't require authentication, this should work just fine.</remarks>
        public static async Task<HttpResponseMessage> GetWebResponse(Uri requestUri, HttpMethod method = null)
        {
            // Run the web request. Perhaps we will get lucky. We know we are lucky if one
            // of the authentication guys doesn't come back, or the resource that responds
            // is the resource we asked for information from.

            var request = CreateRequest(requestUri);
            if (method != null)
            {
                request.Method = method;
            }
            var hc = GetHttpClient();
            var response = await hc.SendRequestAsync(request);

            // First level detection - if this request went well, then we can pass back
            // everything here. Otherwise, see if we can detect something that requires us
            // to log in.

            var responseUri = response.RequestMessage.RequestUri;
            if (responseUri.OriginalString == requestUri.OriginalString)
                return response;
            if (!responseUri.IsCERNSSOAuthUri())
                return response;

            // At this point the resource is going to require authorization. The next step is
            // going to depend a bit on the authorization method we are using, so we delegate it
            // to code that properly deals with that. If no authorize step is required, then
            // we just assume that the last query has the data we need.

            if (!gCredentialInformationValid)
                throw new UnauthorizedAccessException(string.Format("URI {0} requires CERN authentication. None given!", requestUri.OriginalString));

            // At this point, what we should have back is the standard form that redirects us
            // to the place where we can fetch our cookies.

            var loginFormRedirectData = ExtractFormInfo(await response.Content.ReadAsStringAsync());
            if (loginFormRedirectData.Action.IsCERNSSOAuthUri())
                throw new UnauthorizedAccessException(string.Format("Credentials given didn't allow access to {0}.", requestUri.OriginalString));

            var dataRequest = CreateRequest(loginFormRedirectData.Action, loginFormRedirectData.RepostFields);
            response = await hc.SendRequestAsync(dataRequest);

            // And the request for our cookies should end up giving us back the content
            // that we really want!

            return response;
        }

        private static HttpClient gClient = null;

        /// <summary>
        /// Return a client we can use to access the various things around the web.
        /// </summary>
        /// <returns></returns>
        private static HttpClient GetHttpClient()
        {
            if (gClient != null)
                return gClient;

            // Create the handler
            gClient = gCreateHttpHandler != null ? gCreateHttpHandler() : new HttpClient();

            return gClient;
        }

        /// <summary>
        /// Create and normalize a request to the various CERN end-points.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="formData"></param>
        /// <returns></returns>
        private static HttpRequestMessage CreateRequest(Uri u, IDictionary<string, string> formData = null)
        {
            // Create request and do basic prep stuff.
            var request = new HttpRequestMessage(HttpMethod.Get, u);

            // If there is form data we will switch to POST and write it in.
            if (formData != null)
            {
                request.Method = HttpMethod.Post;
                var content = new HttpFormUrlEncodedContent(formData);
                content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Content = content;
            }

            return request;
        }

        /// <summary>
        /// Helper class for parsing and extracting info from a form.
        /// </summary>
        private class FormInfo
        {
            public Uri Action { get; set; }
            public IDictionary<string, string> RepostFields;
        }

        /// <summary>
        /// Extract form data
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static FormInfo ExtractFormInfo(string text)
        {
            var result = new FormInfo();

            // This should contain a redirect that is deep in a HTTP form. We need to parse that out.
            var doc = new HtmlDocument();
            doc.LoadHtml(text);
            var form = doc.DocumentNode.Descendants("form").FirstOrDefault();
            if (form == null)
                throw new InvalidOperationException("Response for authorization didn't contain the expected form");

            // The action is something that came a long in the stream, so it will have been escaped. So,
            // decode that. it could also be relative, so we have to deal with that too.
            var actionUriText = WebUtility.HtmlDecode(form.Attributes["action"].Value);
            result.Action = new Uri(actionUriText, actionUriText.StartsWith("http") ? UriKind.Absolute : UriKind.Relative);

            // Now more through all the form names.
            result.RepostFields = new Dictionary<string, string>();
            foreach (var inputs in doc.DocumentNode.Descendants("input"))
            {
                var ftype = inputs.Attributes["type"].Value;
                if (ftype == "hidden")
                {
                    var name = WebUtility.HtmlDecode(inputs.Attributes["name"].Value);
                    var value = WebUtility.HtmlDecode(inputs.Attributes["value"].Value);
                    result.RepostFields[name] = value;
                }
                else if (ftype == "password" || ftype == "text" || ftype == "submit")
                {
                    if (inputs.Attributes.Contains("name"))
                    {
                        var name = inputs.Attributes["name"].Value;
                        result.RepostFields[name] = "";
                    }
                }
            }
            return result;
        }
    }
}
