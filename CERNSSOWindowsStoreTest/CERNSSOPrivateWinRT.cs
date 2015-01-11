using CERNSSO;
using CERNSSOWindowsDesktopTest;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CERNSSOWindowsStoreTest
{
    /// <summary>
    /// Test private access
    /// </summary>
    [TestClass]
    public class CERNSSOPrivateWinRT
    {
        [TestInitialize]
        public async Task TestInit()
        {
            WebAccess.ResetCredentials();
            WebAccess.LoadCertificate(await CertUtils.GetCert());
        }

        [TestMethod]
        public async Task GetPrivateTitle()
        {
            var title = await TestUtil.GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1712676"));
            Assert.AreEqual(@"Search for pair produced long-lived neutral particles decaying in the ATLAS hadronic calorimeter in $pp$ collisions at $\sqrt{s}= 8~\rm{TeV}$ - CERN Document Server", title, "Title of private paper");
        }       
    }
}
