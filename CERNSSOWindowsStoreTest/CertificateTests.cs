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
            await ClearAssertStore();
            Assert.IsNull(await FindMyCert());

            await LoadCert();

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
            Assert.IsNotNull(FindMyCert());
        }

        /// <summary>
        /// Load a cert up from the application file system
        /// </summary>
        /// <returns></returns>
        private static async Task LoadCert()
        {
            // Load up the password.
            var packageLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var passwordFile = await packageLocation.GetFileAsync("password.txt");
            var bufferme = await Windows.Storage.FileIO.ReadLinesAsync(passwordFile);
            var password = bufferme.First();
            Assert.IsNotNull(password);

            // Load the cert that should have been packaged with us here.
            var certificate = await packageLocation.GetFileAsync("cert.pfx");
            var buffer = await Windows.Storage.FileIO.ReadBufferAsync(certificate);
            var cert = Windows.Security.Cryptography.CryptographicBuffer.EncodeToBase64String(buffer);
            Assert.IsNotNull(cert);

            // Try loading it into the local store.
            await CertificateEnrollmentManager.ImportPfxDataAsync(cert, password, ExportOption.NotExportable, KeyProtectionLevel.NoConsent, InstallOptions.DeleteExpired, "mytestcert");
        }

        /// <summary>
        /// Clear things out - in case we need to do this for a test.
        /// </summary>
        /// <returns></returns>
        private async Task ClearAssertStore()
        {
            var c = await FindMyCert();
            if (c != null)
            {
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
                
            }
        }

        /// <summary>
        /// Normally this would be a test class initializer, but we want to do it a little differently here.
        /// </summary>
        /// <returns></returns>
        async Task InitForTest()
        {
            if (await FindMyCert() == null)
            {
                await LoadCert();
            }
        }

        /// <summary>
        /// Find a cert in the store.
        /// </summary>
        /// <returns></returns>
        async Task<Certificate> FindMyCert()
        {
            var query = new CertificateQuery();
            query.FriendlyName = "mytestcert";
            var certificates = await CertificateStores.FindAllAsync(query);

            if (certificates.Count != 1)
            {
                return null;
            }
            return certificates[0];
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
            await ClearAssertStore();
            var str = await LoadUrl("https://indico.cern.ch/event/359262/", true);
        }

        [TestMethod]
        public async Task LoadProtectedAgendaWithCERT()
        {
            // Go for a protected agenda, but setup the cert
            var filter = new HttpBaseProtocolFilter();
            await InitForTest();
            filter.ClientCertificate = await FindMyCert();
            //var cookie = new HttpCookie("indico_session", ".cern.ch", "");
            var cookie = new HttpCookie("SSOAutologonCertificate", ".cern.ch", "");
            cookie.Value = "true";
            filter.CookieManager.SetCookie(cookie);

            var str = await LoadUrl("https://indico.cern.ch/event/359262/", filter: filter);

        }

        // Access the agenda server
        private static async Task<string> LoadUrl(string url, bool expectingFailure = false, IHttpFilter filter = null)
        {
            var hc = filter == null ? new HttpClient() : new HttpClient(filter);
            var data = await hc.GetStringAsync(new Uri(url));
            Assert.AreNotEqual(0, data.Length);
            Debug.WriteLine(data);
            Assert.AreEqual(expectingFailure, data.Contains("Need password help"));
            return data;
        }
    }
}
