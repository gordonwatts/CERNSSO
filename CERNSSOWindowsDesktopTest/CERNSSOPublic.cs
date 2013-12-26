using CERNSSO;

// Get the unit testing framework properly slotted in.
#if CMPWindowsStore || WINDOWS_PHONE
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace CERNSSOWindowsDesktopTest
{
    /// <summary>
    /// Test access to public CERN web documents and items
    /// </summary>
    [TestClass]
    public class CERNSSOPublic
    {
        /// <summary>
        /// Reset the library before we test it each time.
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            WebAccess.ResetCredentials();
        }

        /// <summary>
        /// Access a CDS record that is publicly available, without
        /// having any sort of credentials present.
        /// </summary>
        [TestMethod]
        public void TestPublicCDSRecord()
        {
        }
    }
}
