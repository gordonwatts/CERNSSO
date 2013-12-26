using CERNSSO;
using System;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;
using System.Linq;

// Get the unit testing framework properly slotted in depending on what project
// we are working with.
#if CMPWindowsStore || WINDOWS_PHONE
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

            var title = await GetCDSPaperTitle(new Uri(@"https://cds.cern.ch/record/1636207?ln=en"));
            Assert.AreEqual("Measurement of dijet cross sections in pp collisions at 7 TeV centre−of−mass energy using the ATLAS detector", title, "Title of public paper");
        }

        /// <summary>
        /// Given a URI of a CDS entry, fetch back the HTML and extract the title
        /// </summary>
        /// <param name="uri">CERN URI we should be fetching</param>
        /// <returns></returns>
        private async Task<string> GetCDSPaperTitle(Uri uri)
        {
            var h = WebRequest.CreateHttp(uri);
            var response = await WebAccess.FetchWebResponse(h);
            return await ExtractHTMLTitleInfo(response);
        }

        /// <summary>
        /// Extract the title from the response.
        /// </summary>
        /// <param name="resp"></param>
        /// <returns></returns>
        private static async Task<string> ExtractHTMLTitleInfo(WebResponse resp)
        {
            using (var rdr = new StreamReader(resp.GetResponseStream()))
            {
                var text = await rdr.ReadToEndAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(text);
                var titleNode = doc.DocumentNode.Descendants("title").FirstOrDefault();
                if (titleNode == null)
                    throw new InvalidDataException("No title node found for the web page!");
                return titleNode.InnerHtml;
            }
        }
    }
}
