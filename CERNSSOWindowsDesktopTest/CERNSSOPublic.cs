using CERNSSO;
using System;
using System.Threading.Tasks;

// Get the unit testing framework properly slotted in depending on what project
// we are working with.
#if CMPWindowsStore || WINDOWS_PHONE_APP
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
        public async Task TestPublicCDSRecord()
        {
            // Access the HTML on the CDS record at
            // https://cds.cern.ch/record/1636207?ln=en which is a public
            // ATLAS paper.

            var title = await TestUtil.GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1636207?ln=en"));
            Assert.AreEqual("Measurement of dijet cross sections in pp collisions at 7 TeV centre−of−mass energy using the ATLAS detector - CERN Document Server", title, "Title of public paper");
        }

        /// <summary>
        /// Attempt to access a private document for which we have no allowed access.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AccessATLASPrivateDocumentNoLogin()
        {
            try
            {
                // Access the HTML on the CDS record at
                // https://cds.cern.ch/record/1512932/? which is a internal note at ATLAS

                var title = await TestUtil.GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1512932/?"));
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            Assert.Fail("Did not get the UnauthorizedAccessExceptoin.");
        }
    }
}
