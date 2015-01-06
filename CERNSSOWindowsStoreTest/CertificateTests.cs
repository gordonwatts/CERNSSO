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
            Assert.IsTrue(true);
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
            var str = await LoadUrl("https://indico.cern.ch/event/359262/", true);
        }

        [TestMethod]
        public async Task LoadProtectedAgendaWithCERT()
        {

        }

        // Access the agenda server
        private static async Task<string> LoadUrl(string url, bool expectingFailure = false)
        {
            var hc = new HttpClient();
            var data = await hc.GetStringAsync(new Uri(url));
            Assert.AreNotEqual(0, data.Length);
            Debug.WriteLine(data);
            Assert.AreEqual(expectingFailure, data.Contains("Need password help"));
            return data;
        }
    }
}
