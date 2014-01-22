
using System;
using System.Net.Http;
using System.Threading.Tasks;
namespace CERNSSOPCL
{
    /// <summary>
    /// Uniform Portable Class Library interface to CERN's Single Sign On.
    /// Username/Password is cross-platform, but use of a certificate isn't. You'll
    /// have to use a specific version of the platform library to effect that.
    /// </summary>
    public class CERNWebAccess
    {
        /// <summary>
        /// We are given a prepared HTTP web request. Fetch a working response.
        /// </summary>
        /// <param name="requestUri">The URI of the CERN SSO resource you want to fetch.</param>
        /// <returns>A response object that should have the data requested in it. Or if authentication fails, it will throw an UnauthorizedAccess exception.</returns>
        /// <remarks>If this is called against a URI that doesn't require authentication, this should work just fine.</remarks>
        public static Task<HttpResponseMessage> GetWebResponse(Uri requestUri)
        {
#if WINDOWS_DESKTOP || WINDOWS_PHONE || CMPWindowsStore
            return CERNSSO.WebAccess.GetWebResponse(requestUri);
#else
            throw new NotImplementedException("The CERNSSO Platform Specific Library has not been included in this project! This is a coding and deployment error.");
#endif
        }

        /// <summary>
        /// Clears out all information we've cached.
        /// </summary>
        /// <remarks>This was added to aid in testing, mostly.</remarks>
        public static void ResetCredentials()
        {
#if WINDOWS_DESKTOP || WINDOWS_PHONE || CMPWindowsStore
            CERNSSO.WebAccess.ResetCredentials();
#else
            throw new NotImplementedException("The CERNSSO Platform Specific Library has not been included in this project! This is a coding and deployment error.");
#endif
        }
    }
}
