using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace WikipediaTestParsing {

    public class CondensedRegex {

        private const string FILE_LOCATION
            = @"C:\Users\Ramon Caballero IV\Documents\Persistance Applications\Wikipedia\" +
            "enwiki-latest-pages-articles-multistream.xml";
        private const string LINKS_REGEX = @"(?<links>\[\[(?<l1>.+?)(?<l2>\|.+?)?\]\])";
        private const string REF_REGEX = @"(?:<ref.+?(?:(?:<\/ref>)|(?:\/>)))";
        //private const string DESCRIPTION_REGEX = @"(;.*\n?(:\s?.*\n?)+)";
        private const string ARTICLE_REGEX = @"(?<articles>={{(?:.+?\s)?article[s]?\|(.+?)}})";
        private const string REDIRECT2_REGEX = @"{{Redirect2.+?}}";
        private const string FILE_REGEX = @"(?>\[\[File.+?\]\])"; // check
        private const string BOLD_TEXT_REGEX = @"'''(?<bold>(?:.+?)?'?'?)'''";
        private const string COMMENT_REGEX = @"<!--[.+?]-->"; // check
        private const string ITALICS_REGEX = @"''(?<italics>(?:.+?)?)''";
        private const string NBSP_REGEX = @"&nbsp;";
        private const string FOREIGN_LANG_REGEX = @"{{lang\|.+?\|(?<foreignLang>.+?)}}";
        private const string PICTURE_DESCRIPTION_REGEX = @"(?<pictureDescription>thumb.+?\]\])"; //incomplete 
        private const string H6_REGEX = "(?>======(?<header6>.+?)======)";
        private const string H5_REGEX = "(?>=====(?<header5>.+?)=====)";
        private const string H4_REGEX = "(?>====(?<header4>.+?)====)";
        private const string H3_REGEX = "(?>===(?<header3>.+?)===)";
        private const string H2_REGEX = "(?>==(?<header2>.+?)==)";
        private const string H1_REGEX = "(?>=(?<header1>.+?)=)";

        public static void Run() {
            Console.OutputEncoding = Encoding.Unicode;
            ReadWikiPages();
        }

        private static void ReadWikiPages() {
            List<Thread> taskList = new List<Thread>();
            StreamReader xmlReader = new StreamReader(FILE_LOCATION);

            string line = String.Empty;

            // Starts reading up until it finds the first page
            while ((line = xmlReader.ReadLine()).Trim() != "</siteinfo>") { }

            DateTime start = DateTime.Now;
            for (int i = 0; i < 2; i++) {
                string rawXmlPage = String.Empty;
                while (true) {
                    line = xmlReader.ReadLine();
                    if (line == String.Empty) line = "\n";
                    rawXmlPage += line;
                    if (line.Trim() == "</page>") break;
                }

                //if (i == 0) continue;

                XmlDocument page = new XmlDocument();
                page.LoadXml(rawXmlPage);

                Thread pageProcess = new Thread(new ParameterizedThreadStart(ParseXML));
                pageProcess.Start(page);
                taskList.Add(pageProcess);
            }
            Console.Write("Waiting...");
            foreach (Thread t in taskList) {
                while (t.IsAlive) { }
            }
            Console.WriteLine("Done");

            Console.WriteLine($"\n--------------------------------------------\n" +
                $"Total time elapsed: {DateTime.Now - start}\n--------------------------------------------\n");
        }

        private static void ParseXML(object doc) {
            XmlDocument page = (XmlDocument) doc;
            string json = JsonConvert.SerializeXmlNode(page, Newtonsoft.Json.Formatting.None, true);
            JObject pageJson = JObject.Parse(json);

            //Console.WriteLine(pageJson);

            string pageTitle = (string) pageJson.GetValue("title");

            string redirect = GetRedirectLink(pageJson);
            if (redirect != null) {
                return;
            }

            JToken revision = pageJson.GetValue("revision");
            JToken textRevisionAttribute = revision["text"];
            JToken actualPageTextToken = textRevisionAttribute["#text"];
            string pageText = actualPageTextToken.ToString();

            //Console.WriteLine(pageText);

            //if (pageText.Length < 50 || pageText.Substring(0, 50).Contains("#REDIRECT")) continue;

            // PERSIST
            //MatchCollection pageLinks = GetRegexMatches(pageText, LINKS_REGEX);
            //ReplaceLinksToPlain(pageLinks, ref pageText, pageTitle);

            // PERSIST
            MatchCollection articleLinks = GetRegexMatches(pageText, ARTICLE_REGEX);
            foreach (Match article in articleLinks) {
                pageText = pageText.Replace(article.Value, "=");
            }

            FormatToHTML(ref pageText, pageTitle);

            //Console.WriteLine($"{pageTitle} done.");

            Console.WriteLine(pageText + "\n--------------------------------------\n");
        }

        private static void FormatToHTML(ref string pageText, string pageTitle) {
            List<string> referencedWikiPages = RunRegex(ref pageText);

            //FindAndReplaceLists(ref pageText);
            //FindAndReplaceDescriptions(ref pageText);
        }

        private static List<string> RunRegex(ref string pageText) {
            List<string> referencedWikiPages = new List<string>();

            string uglyRegexForWholePageText =
                $@"{LINKS_REGEX}|{ARTICLE_REGEX}|(?<remove>{COMMENT_REGEX}|{REF_REGEX}|{REDIRECT2_REGEX}|<.*?See also.*|" +
                $@"{FILE_REGEX})|{BOLD_TEXT_REGEX}|{ITALICS_REGEX}|(?<nbsp>{NBSP_REGEX})|{FOREIGN_LANG_REGEX}|" +
                $@"{H6_REGEX}|{H5_REGEX}|{H4_REGEX}|{H3_REGEX}|{H2_REGEX}|{H1_REGEX}";

            foreach (Match m in GetRegexMatches(pageText, uglyRegexForWholePageText)) {
                string matchValue = m.Value;
                if (!m.Success) continue;
                
                if (m.Groups["links"].Success) {
                    referencedWikiPages.Add(m.Groups["l1"].Value);
                    string actualLinkText = m.Groups["l2"].Value;

                    if (actualLinkText == String.Empty) actualLinkText = m.Groups["l1"].Value;
                    else actualLinkText = actualLinkText.Replace("|", String.Empty);

                    pageText = pageText.Replace(matchValue, actualLinkText);
                }
                if (m.Groups["remove"].Success) pageText = pageText.Replace(matchValue, String.Empty);
                if (m.Groups["nbsp"].Success) pageText = pageText.Replace(matchValue, " ");
                if (m.Groups["bold"].Success) pageText = pageText.Replace(matchValue, $"<b>{m.Groups["bold"].Value}</b>");
                if (m.Groups["italics"].Success) pageText = pageText.Replace(matchValue, $"<i>{m.Groups["italics"].Value}</i>");
                if (m.Groups["foreignLang"].Success) pageText = pageText.Replace(matchValue, m.Groups["foreignLang"].Value);

                if (m.Groups["header6"].Success) pageText = pageText.Replace(matchValue, $"<h6>{m.Groups["header6"].Value}</h6>");
                if (m.Groups["header5"].Success) pageText = pageText.Replace(matchValue, $"<h5>{m.Groups["header5"].Value}</h5>");
                if (m.Groups["header4"].Success) pageText = pageText.Replace(matchValue, $"<h4>{m.Groups["header4"].Value}</h4>");
                if (m.Groups["header3"].Success) pageText = pageText.Replace(matchValue, $"<h3>{m.Groups["header3"].Value}</h3>");
                if (m.Groups["header2"].Success) pageText = pageText.Replace(matchValue, $"<h2>{m.Groups["header2"].Value}</h2>");
                if (m.Groups["header1"].Success) pageText = pageText.Replace(matchValue, $"<h1>{m.Groups["header1"].Value}</h1>");
            }

            return referencedWikiPages;
        }

        #region Unused
        //private static void FindAndReplaceDescriptions(ref string pageText) {
        //    MatchCollection descriptions = GetRegexMatches(pageText, DESCRIPTION_REGEX);
        //    string definitionRegex = @":\s?;(.*)";

        //    foreach (Match description in descriptions) {
        //        foreach (Match definition in GetRegexMatches(description.Value, definitionRegex)) {

        //        }
        //    }
        //}

        private static void FindAndReplaceLists(ref string pageText) {
            // DO IN TUTORING
        }
        #endregion

        private static string GetRedirectLink(JObject pageJson) {
            string redirectPage = null;
            JToken redirect = pageJson.GetValue("redirect");
            if (redirect != null) {
                redirectPage = (string) redirect["@title"];
            }

            return redirectPage;
        }

        private static void ReplaceLinksToPlain(MatchCollection pageLinks, ref string pageText, string pageTitle) {
            foreach (Match link in pageLinks) {
                string actualLinkText = link.Groups[2].Value;

                if (actualLinkText == String.Empty) actualLinkText = link.Groups[1].Value;
                else actualLinkText = actualLinkText.Replace("|", String.Empty);

                pageText = pageText.Replace(link.Value, actualLinkText);
            }
        }

        private static void RemoveMatches(MatchCollection matches, ref string pageText) {
            foreach (Match match in matches) {
                pageText = pageText.Replace(match.Value, String.Empty);
            }
        }

        private static MatchCollection GetRegexMatches(string text, string regex) {
            Regex reg = new Regex(regex);
            return reg.Matches(text);
        }
    }
}