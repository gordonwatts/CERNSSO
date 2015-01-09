using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Certificates;

namespace CERNSSOWindowsStoreTest
{
    /// <summary>
    /// Some easy routines for accessing certs
    /// </summary>
    class CertUtils
    {
        /// <summary>
        /// Get a cert. Load the cert if it hasn't already been loaded into the app cache.
        /// </summary>
        /// <returns></returns>
        public static async Task<Certificate> GetCert()
        {
            var c = await FindMyCert();
            if (c != null)
            {
                return c;
            }
            await LoadCert();
            return await FindMyCert();
        }

        /// <summary>
        /// Do the work of loading it in from our manifest.
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
        /// Search for the cert.
        /// </summary>
        /// <returns>Returns null if it isn't already loaded, otherwise the cert</returns>
        public static async Task<Certificate> FindMyCert()
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
    }
}
