﻿using System;

namespace CERNSSO
{
    static class Utils
    {
        /// <summary>
        /// See if this uri matches with one of the authorization URI's that we
        /// might expect back from CERN's SSO.
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static bool IsCERNSSOAuthUri(this Uri u)
        {
            if (u.IsAbsoluteUri)
                return u.AbsolutePath.StartsWith("/adfs/ls/");
            return u.OriginalString.StartsWith("/adfs/ls/");
        }

        /// <summary>
        /// If the URI is relative, turn it into an absolute one.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="abosluteUri"></param>
        /// <returns></returns>
        public static Uri MakeAbsolute(this Uri orig, Uri abosluteUri)
        {
            if (!orig.IsAbsoluteUri)
            {
                orig = new Uri(abosluteUri, orig);
            }
            return orig;
        }
    }
}
