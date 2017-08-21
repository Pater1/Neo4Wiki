using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using NeoContainers;

using Neo4jClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace WikipediaTestParsing {

    public class SpreadOutRegex {

        private const string FILE_LOCATION
            = @"C:\Users\Ramon Caballero IV\Documents\Persistance Applications\Wikipedia\" +
            "enwiki-latest-pages-articles-multistream.xml";

        private const string LINKS_REGEX = @"\[\[(?!File:)(?<l1>.+?)(?<l2>\|.+?)?\]\]";
        private const string REF_REGEX = @"(?:<ref.+?(?:<\/ref>|\/>))";
        private const string DESCRIPTION_REGEX = @"(;.*\n?(:\s?.*\n?)+)";
        private const string WHOLE_ARTICLE_REGEX = @"(?:{{(?:.+?\s)?article[s]?\|(.+?)}})";
        private const string REDIRECT2_REGEX = @"{{Redirect2.+?}}";
        private const string FILE_REGEX = @"\[\[File:.*?\]\]";
        private const string BOLD_TEXT_REGEX = @"'''((?:.+?)?'?'?)'''";
        private const string COMMENT_REGEX = @"<!--.+?-->";

        //private static Dictionary<string, List<string>> pagelinks = new Dictionary<string, List<string>>();
        private static List<string> allTitles = new List<string>();

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
            using (GraphClient database = new GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "password")) {
                database.Connect();
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
                    pageProcess.Start(new Wrapper() { doc = page, db = database });
                    taskList.Add(pageProcess);
                }
                Console.Write("Waiting...");
                foreach (Thread t in taskList) {
                    while (t.IsAlive) { }
                }
                taskList.Clear();

                foreach (string s in allTitles) {
                    Thread pageLinkup = new Thread(new ParameterizedThreadStart(CreateRelationship));
                    pageLinkup.Start(new TitleDatabaseWrapper() { title = s, db = database });
                    taskList.Add(pageLinkup);
                }

                foreach (Thread t in taskList) {
                    while (t.IsAlive) { }
                }

                Console.WriteLine("Done");

                Console.WriteLine($"\n--------------------------------------------\n" +
                    $"Total time elapsed: {DateTime.Now - start}\n--------------------------------------------\n");
            }
        }

        private class TitleDatabaseWrapper {
            public string title;
            public GraphClient db;
        }

        private static void CreateRelationship(object obj) {
            TitleDatabaseWrapper w = (TitleDatabaseWrapper) obj;
            PageNode.PullFromDatabaseByTitle(w.db, w.title).LinkUp(w.db);
        }

        private class Wrapper {
            public XmlDocument doc;
            public GraphClient db;
        }

        private static void ParseXML(object wr) {
            Wrapper w = (Wrapper) wr;
            XmlDocument page = w.doc;
            GraphClient db = w.db;
            
            string json = JsonConvert.SerializeXmlNode(page, Newtonsoft.Json.Formatting.None, true);
            JObject pageJson = JObject.Parse(json);

            //Console.WriteLine(pageJson);

            string pageTitle = (string) pageJson.GetValue("title");

            string redirect = GetRedirectLink(pageJson);
            if (redirect != null) return;

            long pageId = long.Parse(pageJson.GetValue("id").ToString());

            JToken revision = pageJson.GetValue("revision");
            JToken textRevisionAttribute = revision["text"];
            JToken actualPageTextToken = textRevisionAttribute["#text"];
            string pageText = actualPageTextToken.ToString();
            //Console.WriteLine(pageText);

            //if (pageText.Length < 50 || pageText.Substring(0, 50).Contains("#REDIRECT")) continue;

            // PERSIST
            List<string> links = new List<string>();

            FormatToHTML(ref pageText, pageTitle, ref links);
            allTitles.Add(pageTitle);

            PageNode pageNode = new PageNode(pageId, pageTitle, pageText, links.ToArray());
            pageNode.WriteToDatabase(db);
        }

        private static void FormatToHTML(ref string pageText, string pageTitle, ref List<string> links) {
            RemoveUnecessaryInfo(ref pageText);

            foreach (Match articles in GetRegexMatches(pageText, WHOLE_ARTICLE_REGEX)) {
                pageText = pageText.Replace(articles.Value, String.Empty);
                links.AddRange(articles.Groups[1].Value.Split('|'));
            }

            ReplaceLinksToPlain(GetRegexMatches(pageText, $"{LINKS_REGEX}"), ref pageText, ref links);
            RemoveMatches(GetRegexMatches(pageText, FILE_REGEX), ref pageText);

            FindAndReplaceHeaders(ref pageText);
            FindAndReplaceLists(ref pageText);
            FindAndReplaceForeignLanguage(ref pageText);
            ApplyBold(ref pageText);
            ApplyItalics(ref pageText);
        }

        private static void RemoveUnecessaryInfo(ref string pageText) {
            string removeRegexSingleLine = $@"{REDIRECT2_REGEX}|&nbsp;|(?:{{{{.+?}}}})";
            RemoveMatches(GetRegexMatches(pageText, removeRegexSingleLine), ref pageText);
            RemoveMatches(GetRegexMatches(pageText, $"{COMMENT_REGEX}|==See also.*|{REF_REGEX}", RegexOptions.Singleline), ref pageText);
        }

        private static void ApplyBold(ref string pageText) {
            foreach (Match boldText in GetRegexMatches(pageText, BOLD_TEXT_REGEX)) {
                pageText = pageText.Replace(boldText.Value, $"<b>{boldText.Groups[1].Value}</b>");
            }
        }

        private static void ApplyItalics(ref string pageText) {
            string italicsRegex = @"''((?:.+?)?)''";
            foreach (Match italicText in GetRegexMatches(pageText, italicsRegex)) {
                pageText = pageText.Replace(italicText.Value, $"<i>{italicText.Groups[1].Value}</i>");
            }
        }

        private static void FindAndReplaceForeignLanguage(ref string pageText) {
            string languageDetectRegex = @"{{lang\|.+?\|(.+?)}}";
            foreach (Match languageText in GetRegexMatches(pageText, languageDetectRegex)) {
                pageText = pageText.Replace(languageText.Value, languageText.Groups[1].Value);
            }
        }

        private static void FindAndReplaceLists(ref string pageText) {
            // DO IN TUTORING
        }

        private static void FindAndReplaceHeaders(ref string pageText) {
            string headerPattern = "======(.+?)======";

            for (int i = 6; i > 1; i--) {
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

        private static void ReplaceLinksToPlain(MatchCollection pageLinks, ref string pageText, ref List<string> linkNames) {

            foreach (Match link in pageLinks) {
                linkNames.Add(link.Groups[1].Value);
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

        private static MatchCollection GetRegexMatches(string text, string regex, RegexOptions options = RegexOptions.None) {
            Regex reg = new Regex(regex, options);
            return reg.Matches(text);
        }
    }
}