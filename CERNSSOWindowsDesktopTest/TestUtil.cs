using CERNSSO;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CERNSSOWindowsDesktopTest
{
    /// <summary>
    /// Some helper functions
    /// </summary>
    static class TestUtil
    {
        /// <summary>
        /// Given a URI of a CDS entry, fetch back the HTML and extract the title
        /// </summary>
        /// <param name="uri">CERN URI we should be fetching</param>
        /// <returns></returns>
        public static async Task<string> GetCDSPaperTitle(Uri uri)
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
        public static async Task<string> ExtractHTMLTitleInfo(WebResponse resp)
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
