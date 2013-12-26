﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
        private static Action<HttpRequestMessage> gPrepWebRequest = null;

        /// <summary>
        /// The initial web request has come back with no authorization. So, the next step
        /// we need to do is to do the authorization. This delegate will fill that in.
        /// The argument is the response from the resource that triggered the login.
        /// </summary>
        private static Func<HttpResponseMessage, Task<HttpRequestMessage>> gAuthorize = null;

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

            // Run the login. Code is reasonable complex, so we move it below.
            gAuthorize = resp =>
                {
                    return AuthorizeWithUsernameAndPassword(resp, username, password);
                };
        }

        /// <summary>
        /// Clears out all information we've cached. This is useful mainly for testing
        /// to make sure the state of the library is reset.
        /// </summary>
        public static void ResetCredentials()
        {
            // Release all the delegates
            gAuthorize = null;
            gPrepWebRequest = null;

            // Release the web access client - this contains cookies that may match
            // with the above.
            gClient = null;
        }

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

            var request = CreateRequest(requestUri);
            var hc = GetHttpClient();
            var response = await hc.SendAsync(request);

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
            // to code that properly deals with that.

            var authReq = await gAuthorize(response);
            response = await hc.SendAsync(authReq);

            // At this point, what we should have back is the standard form that redirects us
            // to the place where we can fetch our cookies.

            var loginFormRedirectData = ExtractFormInfo(await response.Content.ReadAsStringAsync());

            var dataRequest = CreateRequest(loginFormRedirectData.Action, loginFormRedirectData.RepostFields);
            response = await hc.SendAsync(dataRequest);

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

            var cookies = new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookies };
            gClient = new HttpClient(handler);

            return gClient;
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

        /// <summary>
        /// Perform a log-in using a username and password.
        /// </summary>
        /// <param name="response">Response message from the resource load that triggered this</param>
        private static async Task<HttpRequestMessage> AuthorizeWithUsernameAndPassword(HttpResponseMessage response, string username, string password)
        {
            var signinFormData = ExtractFormInfo(await response.Content.ReadAsStringAsync());

            // Set the user name and password, and repost.

            int oldNumberKeys = signinFormData.RepostFields.Count;
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$txtFormsLogin"] = username;
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$txtFormsPassword"] = password;
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$btnFormsLogin"] = "Sign in";

            // Next task is to alter the repost fields a little bit. If we don't do this, we fail authentication. Yes... We do! Yikes! :-)
            signinFormData.RepostFields.Remove("ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$btnSelectFederation");
            signinFormData.RepostFields["ctl00$ctl00$NICEMasterPageBodyContent$SiteContentPlaceholder$drpFederation"] = "";

            // If we are doing relative URI's, fix it up.
            var loginUri = signinFormData.Action.MakeAbsolute(response.RequestMessage.RequestUri);

            return CreateRequest(loginUri, signinFormData.RepostFields);
        }

        /// <summary>
        /// Create and normalize a request to the various CERN end-points.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        private static HttpRequestMessage CreateRequest(Uri u, IDictionary<string, string> formData = null)
        {
            // Create request and do basic prep stuff.
            var request = new HttpRequestMessage(HttpMethod.Get, u);

            // If there is form data we will switch to POST and write it in.
            if (formData != null)
            {
                request.Method = HttpMethod.Post;
                var content = new FormUrlEncodedContent(formData);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                request.Content = content;
            }

            // Now prep anything else. We have to do this afterwards in case the content,
            // which also has the headers, is modified.
            PrepWebRequest(request);

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
        /// <param name="homeSiteLoginRedirect"></param>
        /// <param name="repostFields"></param>
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
