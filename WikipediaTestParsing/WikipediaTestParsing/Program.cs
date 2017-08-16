using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WikipediaTestParsing {

    public class Program {
        
        private const string FILE_LOCATION
            = @"C:\Users\Ramon Caballero IV\Documents\Persistance Applications\Wikipedia\" + 
            "enwiki-latest-pages-articles-multistream.xml";
        private const string TITLE_REGEX = @"'''(.*)'''";
        private const string LINKS_REGEX = @"\[\[(.+?)(\|.+?)?\]\]";
        private const string REF_REGEX = @"<ref.+?<\/ref>";
        private const string DESCRIPTION_REGEX = @"(;.*\n?(:\s?.*\n?)+)";
        private const string ARTICLE_REGEX = @"(?:{{(?:.+?\s)?article[s]?\|(.*)}}=?)";
        private const string REDIRECT2_REGEX = @"{{Redirect2.+?}}";

        public static void Main(string[] args) {
            Test();
        }

        private static void Test() {
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

                if (i == 0) continue;

                XmlDocument page = new XmlDocument();
                page.LoadXml(rawXmlPage);
                string json = JsonConvert.SerializeXmlNode(page, Newtonsoft.Json.Formatting.None, true);
                JObject pageJson = JObject.Parse(json);

                //Console.WriteLine(pageJson);

                string pageTitle = (string) pageJson.GetValue("title");

                string redirect = GetRedirectLink(pageJson);

                JToken revision = pageJson.GetValue("revision");
                JToken textRevisionAttribute = revision["text"];
                JToken actualPageTextToken = textRevisionAttribute["#text"];
                string actualPageText = actualPageTextToken.ToString();

                FormatToHTML(ref actualPageText, pageTitle);

                //Console.WriteLine($"{pageTitle} done.");

                Console.WriteLine(actualPageText + "\n");
            }

            Console.WriteLine($"\n--------------------------------------------\n" +
                $"Total time elapsed: {DateTime.Now - start}\n--------------------------------------------\n");
        }

        private static void FormatToHTML(ref string pageText, string pageTitle) {
            MatchCollection pageLinks = GetRegexMatches(pageText, LINKS_REGEX);
            MatchCollection articleLinks = GetRegexMatches(pageText, ARTICLE_REGEX);
            foreach (Match m in articleLinks) {
                Console.WriteLine(m.Value);
                Console.WriteLine("\n---------------------------------------------------\n");
            }
            ReplaceLinksToPlain(pageLinks, ref pageText, pageTitle);

            MatchCollection references = GetRegexMatches(pageText, REF_REGEX);
            RemoveMatches(references, ref pageText);

            FindAndReplaceRedirects(ref pageText);
            ReplaceWeirdTitleFormatting(ref pageText);
            FindAndReplaceHeaders(ref pageText);
            FindAndReplaceNewlines(ref pageText);
            FindAndReplaceLists(ref pageText);
            FindAndReplaceDescriptions(ref pageText);
            FindReferencedArticles(ref pageText);
        }

        private static void FindAndReplaceRedirects(ref string pageText) {
            MatchCollection redirects = GetRegexMatches(pageText, REDIRECT2_REGEX);
            
            foreach (Match redirect in redirects) {

            }
        }

        private static void FindAndReplaceDescriptions(ref string pageText) {
            MatchCollection descriptions = GetRegexMatches(pageText, DESCRIPTION_REGEX);
            string definitionRegex = @":\s?(.*)";

            foreach (Match description in descriptions) {
                foreach (Match definition in GetRegexMatches(description.Value, definitionRegex)) {
                    Console.WriteLine(definition.Value);
                    Console.WriteLine("\n--------------------------------------\n");
                }
            }
        }

        private static void FindAndReplaceLists(ref string pageText) {
            // DO IN TUTORING
        }

        private static void FindAndReplaceNewlines(ref string pageText) {
            foreach (Match newLine in GetRegexMatches(pageText, @"\n")) {
                pageText = pageText.Replace(newLine.Value, "<br/>");
            }
        }

        // TODO fix for universal use
        private static void FindReferencedArticles(ref string pageText) {
            MatchCollection references = GetRegexMatches(pageText, @"{{Related articles\|(.+?)}}");

            Console.WriteLine("\n-----------------------------------\n");
            foreach (Match reference in references) {
                Console.WriteLine(reference);
            }
            Console.WriteLine("\n-----------------------------------\n");
        }

        private static void ReplaceWeirdTitleFormatting(ref string pageText) {
            foreach (Match title in GetRegexMatches(pageText, TITLE_REGEX)) {
                pageText = pageText.Replace(title.Groups[0].Value, $"<b>{title.Groups[1].Value}</b>");
            }
        }

        private static void FindAndReplaceHeaders(ref string pageText) {
            string headerPattern = "======(.+?)======";

            for (int i = 6; i > 0; i--) {
                MatchCollection headersFound = GetRegexMatches(pageText, headerPattern);

                foreach (Match header in headersFound) {
                    pageText = pageText.Replace(header.Value, $"<h{i}>{header.Groups[1].Value}</h{i}>");
                }

                headerPattern = headerPattern.Remove(0, 1);
                headerPattern = headerPattern.Remove(headerPattern.Length - 1);
            }
        }

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