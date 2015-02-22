using CERNSSO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CERNSSOWindowsDesktopTest
{
    /// <summary>
    /// Test all login via certificates
    /// </summary>
    [TestClass]
    public class CERNSSOCertificate
    {
        [TestInitialize]
        public void TestInit()
        {
            WebAccess.ResetCredentials();
            WebAccess.LoadCertificate(FindCert());
        }

        /// <summary>
        /// Test accessing a public document even though we
        /// have logged in.
        /// </summary>
        [TestMethod]
        public async Task AccessCertPublicDocument()
        {
            // Access the HTML on the CDS record at
            // https://cds.cern.ch/record/1636207?ln=en which is a public
            // ATLAS paper.

            var title = await TestUtil.GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1636207?ln=en"));
            Assert.AreEqual("Measurement of dijet cross sections in pp collisions at 7 TeV centre−of−mass energy using the ATLAS detector - CERN Document Server", title, "Title of public paper");
        }

        /// <summary>
        /// Get after a private ATLAS document using a username and password.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task AccessCertATLASPrivateDocument()
        {
            // Access the HTML on the CDS record at
            // https://cds.cern.ch/record/1512932/? which is a internal note at ATLAS

            var title = await TestUtil.GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1512932/?"));
            Assert.AreEqual("Searches for long-lived neutral particles decaying into Heavy Flavors In the Hadronic Calorimeter of ATLAS at sqrt{s} = 8 TeV - CERN Document Server", title, "Title of public paper");
        }

        /// <summary>
        /// Find a certificate that can be used to log in.
        /// </summary>
        /// <returns></returns>
        private static X509Certificate2 FindCert()
        {
            var st = new X509Store(StoreName.My);
            st.Open(OpenFlags.ReadOnly);
            try
            {
                var allcerts = st.Certificates.Cast<X509Certificate2>().Where(s => s.SubjectName.Name.Contains("DC=cern") && s.SubjectName.Name.Contains("OU=Users")).ToArray();
                Assert.AreEqual(2, allcerts.Length, "Should have exactly one good CERT in the store!");
                return allcerts[0];
            }
            finally
            {
                st.Close();
            }
        }
    }
}
