using System;

namespace CERNSSO
{
    public static class Utils
    {
        /// <summary>
        /// See if this uri matches with one of the authorization URI's that we
        /// might expect back from CERN's SSO.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static bool IsCERNSSOAuthUri(this Uri u)
        {
            return u.Fragment.StartsWith("/adfs/ls/");
        }
    }
}
