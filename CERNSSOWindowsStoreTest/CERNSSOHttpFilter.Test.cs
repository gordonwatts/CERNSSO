using CERNSSO;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace CERNSSOWindowsStoreTest
{
	[TestClass]
    public class CERNSSOHttpFilterTest
    {
		[TestMethod]
		public void CreateEmpty()
        {
            var a = new CERNSSOHttpFilter(null, new HttpBaseProtocolFilter());
        }

		[TestMethod]
		public async Task CreateWithCert()
        {
            var a = new CERNSSOHttpFilter(await CertUtils.GetCert(), new HttpBaseProtocolFilter());
        }

		[TestMethod]
		public async Task GetUnprotectedIndicoURL()
        {
            var a = new CERNSSOHttpFilter(null, new HttpBaseProtocolFilter());
            await LoadUrl(a, "https://indico.cern.ch/event/336571/");
        }

		[TestMethod]
		public async Task GetProtectedIndicoURLNoCert()
        {
            var a = new CERNSSOHttpFilter(null, new HttpBaseProtocolFilter());
            await LoadUrl(a, "https://indico.cern.ch/event/359262/", true);
        }

		[TestMethod]
		public async Task GetProtectedIndicoURL()
        {
            var a = new CERNSSOHttpFilter(await CertUtils.GetCert(), new HttpBaseProtocolFilter());
            await LoadUrl(a, "https://indico.cern.ch/event/359262/");
        }

        [TestMethod]
        public async Task Get2ProtectedIndicoURLs()
        {
            var a = new CERNSSOHttpFilter(await CertUtils.GetCert(), new HttpBaseProtocolFilter());
            await LoadUrl(a, "https://indico.cern.ch/event/359262/");
            await LoadUrl(a, "https://indico.cern.ch/event/286493/");
        }

        // Access the agenda server
        private static async Task<string> LoadUrl(CERNSSOHttpFilter filter, string url, bool expectingFailure = false)
        {
            var hc = new HttpClient(filter);
            var u = new Uri(url);
            var data = await hc.GetStringAsync(u);
            Assert.AreNotEqual(0, data.Length);

            Debug.WriteLine("Return data:");
            Debug.WriteLine(data);
            Assert.AreEqual(expectingFailure, data.Contains("Need password help"));
            return data;
        }
    }
}
