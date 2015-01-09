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

namespace CERNSSOWindowsStoreTest
{
	[TestClass]
    public class CERNSSOHttpFilterTest
    {
		[TestMethod]
		public void CreateEmpty()
        {
            var a = new CERNSSOHttpFilter();
        }

		[TestMethod]
		public async Task CreateWithCert()
        {
            var a = new CERNSSOHttpFilter(await CertUtils.GetCert());
        }

		[TestMethod]
		public async Task GetUnprotectedIndicoURL()
        {
            var a = new CERNSSOHttpFilter();
            await LoadUrl(a, "https://indico.cern.ch/event/336571/");
        }

		[TestMethod]
		public async Task GetProtectedIndicoURLNoCert()
        {
            var a = new CERNSSOHttpFilter();
            await LoadUrl(a, "https://indico.cern.ch/event/359262/", true);
        }

		[TestMethod]
		public async Task GetProtectedIndicoURL()
        {
            var a = new CERNSSOHttpFilter(await CertUtils.GetCert());
            await LoadUrl(a, "https://indico.cern.ch/event/359262/", true);
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
