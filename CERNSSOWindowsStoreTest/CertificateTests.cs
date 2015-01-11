using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace CERNSSOWindowsStoreTest
{
    [TestClass]
    public class CertificateTests
    {
        [TestMethod]
        public async Task LoadExistingAllCerts()
        {
            IReadOnlyList<Certificate> certificates = await CertificateStores.FindAllAsync();
            Debug.WriteLine("Number of cert is {0}", certificates.Count);
            foreach (var c in certificates)
            {
                Debug.WriteLine("Name: {0}", c.FriendlyName);
            }
        }

        [TestMethod]
        public async Task LoadCertFromLocalFS()
        {
            // Make sure everything is empty here so we can test).
            if (!await ClearAssertStore())
            {
                Assert.IsTrue(true, "Unable to unload an already loaded cert, so can't run test!");
            }
            else
            {
                await CertUtils.GetCert();

                // Make sure some are there.
                var certificates = await CertificateStores.FindAllAsync();
                Debug.WriteLine("Number of cert is {0}", certificates.Count);
                foreach (var c in certificates)
                {
                    Debug.WriteLine("  -> {0}", c.Issuer);
                }
                Assert.IsTrue(certificates.Count > 0); // 3 because there is a liniage.

                var query = new CertificateQuery();
                query.FriendlyName = "mytestcert";
                certificates = await CertificateStores.FindAllAsync(query);
                Assert.AreEqual(1, certificates.Count);

                // Make sure things we are going to use for everything are working!
                Assert.IsNotNull(await CertUtils.GetCert());
            }
        }

        /// <summary>
        /// Clear things out - in case we need to do this for a test.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> ClearAssertStore()
        {
            var c = await CertUtils.FindMyCert();
            if (c != null)
            {
                return false;
#if false
                // Turns out there is currently no way to properly remove a cert - as far as I can tell. I asked, waiting for an answer:
                // http://stackoverflow.com/questions/27809503/how-can-i-remove-a-certificate-from-my-apps-certificate-store
                var certs = await CertificateStores.FindAllAsync();
                Debug.WriteLine("Found a cert to clear in ClearAssertStore:");
                foreach (var ct in certs)
                {
                    Debug.WriteLine("  -> we see {0} by {1}", ct.FriendlyName, ct.Issuer);
                }

                var s = CertificateStores.GetStoreByName("MY");
                if (s != null)
                    s.Delete(c);
                Assert.IsNull(await FindMyCert());
#endif
            }
            return true;
        }

        [TestMethod]
        public async Task LoadUnprotectedAgenda()
        {
            // Load an indico agenda w/out any protection.
            await LoadUrl("https://indico.cern.ch/event/336571/");
        }

        [TestMethod]
        public async Task LoadProtectedAgendaNoCERT()
        {
            // Go for a protected agenda, but no cert loaded.
            if (!await ClearAssertStore())
            {
                Assert.IsTrue(true, "Run in wrong order - a cert was already loaded and we couldn't unload it");
            }
            else
            {
                var str = await LoadUrl("https://indico.cern.ch/event/359262/", true);
            }
        }

        [TestMethod]
        public async Task LoadProtectedAgendaWithCERT()
        {
            // Go for a protected agenda, but setup the cert
            var filter = new HttpBaseProtocolFilter();
            filter.ClientCertificate = await CertUtils.GetCert();
            //var cookie = new HttpCookie("indico_session", ".cern.ch", "");
            var cookie = new HttpCookie("SSOAutologonCertificate", ".cern.ch", "");
            cookie.Value = "true";
            filter.CookieManager.SetCookie(cookie);

            var str = await LoadUrl("https://indico.cern.ch/event/359262/", filter: filter);

        }

        [TestMethod]
        public async Task LoadProtectedCDSWithCERT()
        {
            // Go for a protected agenda, but setup the cert
            var filter = new HttpBaseProtocolFilter();
            filter.ClientCertificate = await CertUtils.GetCert();
            //var cookie = new HttpCookie("indico_session", ".cern.ch", "");
            var cookie = new HttpCookie("SSOAutologonCertificate", ".cern.ch", "");
            cookie.Value = "true";
            filter.CookieManager.SetCookie(cookie);

            var str = await LoadUrl("https://cds.cern.ch/record/1712676?", filter: filter);

        }

        // Access the agenda server
        private static async Task<string> LoadUrl(string url, bool expectingFailure = false, HttpBaseProtocolFilter filter = null)
        {
            var hc = filter == null ? new HttpClient() : new HttpClient(filter);
            var u = new Uri(url);
            var data = await hc.GetStringAsync(u);
            Assert.AreNotEqual(0, data.Length);
            if (filter != null)
            {
                Debug.WriteLine("Cookies that are in there now:");
                foreach (var c in filter.CookieManager.GetCookies(u))
                {
                    Debug.WriteLine("  {0} = {1} (expires {2})", c.Name, c.Value, c.Expires.HasValue ? c.Expires.Value.ToString() : "<none>");
                }
            }
            Debug.WriteLine("Return data:");
            Debug.WriteLine(data);
            Assert.AreEqual(expectingFailure, data.Contains("Need password help"));
            return data;
        }
    }
}
