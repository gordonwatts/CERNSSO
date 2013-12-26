
namespace CERNSSO
{
    /// <summary>
    /// Static methods to enable web requests to CERN protected sites.
    /// </summary>
    public static class WebAccess
    {
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
        }

        /// <summary>
        /// Clears out all information we've cached. This is useful mainly for testing
        /// to make sure the state of the library is reset.
        /// </summary>
        public static void ResetCredentials()
        { }
    }
}
