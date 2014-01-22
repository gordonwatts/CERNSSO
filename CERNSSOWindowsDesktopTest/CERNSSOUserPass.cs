using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CERNSSOWindowsDesktopTest
{
    /// <summary>
    /// Test access when we set a username and password.
    /// </summary>
    [TestClass]
    public class CERNSSOUserPass
    {
        /// <summary>
        /// Test accessing a public document even though we
        /// have logged in.
        /// </summary>
        [TestMethod]
        public async Task AccessPublicDocument()
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
        public async Task AccessATLASPrivateDocument()
        {
            // Access the HTML on the CDS record at
            // https://cds.cern.ch/record/1512932/? which is a internal note at ATLAS

            var title = await TestUtil.GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1512932/?"));
            Assert.AreEqual("Searches for long-lived neutral particles decaying into Heavy Flavors In the Hadronic Calorimeter of ATLAS at sqrt{s} = 8 TeV - CERN Document Server", title, "Title of public paper");
        }

        /// <summary>
        /// Get the username and password that we can use to access things here.
        /// </summary>
        /// <returns></returns>
        private static Tuple<string, string> LookupUserPass()
        {
            Credential cred;
            if (!NativeMethods.CredRead("cern.ch", CRED_TYPE.GENERIC, 0, out cred))
            {
                Console.WriteLine("Error getting credentials");
                Console.WriteLine("Use the credential control panel, create a generic credential for windows domains for cern.ch with username and password");
                throw new InvalidOperationException();
            }

            string password;
            using (var m = new MemoryStream(cred.CredentialBlob, false))
            using (var sr = new StreamReader(m, System.Text.Encoding.Unicode))
            {
                password = sr.ReadToEnd();
            }

            return Tuple.Create(cred.UserName, password);
        }
    }
}
