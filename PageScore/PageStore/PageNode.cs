using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using driver = Neo4j.Driver.V1;
using client = Neo4jClient;
using PageScore;
using Neo4jClient.Cypher;
using Neo4jClient;
using System.Xml;

namespace NeoContainers
{
    public class PageNode//: driver.INode
    {
        public static PageNode BuildFromINode(driver.INode node)
        {
            return new PageNode(
                (long)node.Properties["ID"],
                (string)node.Properties["Title"],
                (string)node.Properties["Text"],
                (string[])node.Properties["WordList"],
                (int[])node.Properties["WordCount"]
            );
        }
        public static PageNode BuildFromINode(ICypherResultItem node)
        {
            client.Node<PageNode> pg = node.Node<PageNode>();
            return pg.Data;
        }
        public static client.Node<PageNode> WriteToDatabase(client.GraphClient graphClient, PageNode pagenode)
        {
            client.Node<PageNode> npn = null;
            using (client.Transactions.ITransaction trans = graphClient.BeginTransaction())
            {
                npn = graphClient.Cypher
                    .Create(DocParseHelper.KeyFor<PageNode>("page"))
                    .WithParams(new { pagenode })
                    .Return(page => page.Node<PageNode>())
                    .Results
                    .Single();

                trans.Commit();
            }
            return npn;
        }
        public client.Node<PageNode> WriteToDatabase(client.GraphClient graphClient)
        {
            return WriteToDatabase(graphClient, this);
        }

        public long ID { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string[] WordList { get; set; }
        public int[] WordCount { get; set; }

        private string[] LinkedToList { get; set; }
        public PageNode[] GetLinkedPages(client.GraphClient graphClient) {
            PageNode[] ret = new PageNode[LinkedToList.Length];

            for(int i = 0; i < LinkedToList.Length; i++) {
                ret[i] = PullFromDatabaseByTitle(graphClient, LinkedToList[i]);
            }

            return ret;
        }

        public static implicit operator XmlDocument(PageNode a) {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(a.Text);
            return doc;
        }

        public int this[string key]
        {
            get
            {
                int i = Array.IndexOf(WordList, key);
                return WordCount[i];
            }
        }

        public static PageNode PullFromDatabaseByTitle(client.GraphClient graphClient, string title)
        {
            return graphClient.Cypher
                .Match("(pagenode:PageNode)")
                .Where((PageNode pagenode) => pagenode.Title == title)
                .Return(pagenode => pagenode.Node<PageNode>())
                .Results.Single().Data;
        }
        public void LinkUp(client.GraphClient graphClient)
        {
            Node<PageNode> source =
                graphClient.Cypher
                .Match("(pagenode:PageNode)")
                .Where((PageNode pagenode) => pagenode.Title == Title)
                .Return(pagenode => pagenode.Node<PageNode>())
                .Results.Single();

            foreach (string st in LinkedToList)
            {
                Node<PageNode> linked =
                    graphClient.Cypher
                    .Match("(pagenode:PageNode)")
                    .Where((PageNode pagenode) => pagenode.Title == st)
                    .Return(pagenode => pagenode.Node<PageNode>())
                    .Results.Single();

                graphClient.CreateRelationship(source.Reference, new LinksTo(linked.Reference));
            }
        }

        public PageNode(){}
        public PageNode(long iD, string title, string text, string[] links = null) : this()
        {
            ID = iD;
            Title = title;
            Text = text;

            string[] list;
            int[] count;
            UnzipArrays(out list, out count, text.GetWordCountDictFromXMLString());

            WordCount = count;
            WordList = list;

            LinkedToList = links;
        }
        public PageNode(long iD, string title, string text, string[] words, int[] count, string[] links = null) : this()
        {
            ID = iD;
            Title = title;
            Text = text;
            WordCount = count;
            WordList = words;

            LinkedToList = links;
        }

        public static Dictionary<string, int> ZipTogetherArrays(string[] words, int[] count)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>();
            for (int i = 0; i < words.Length; i++)
            {
                ret.Add(words[i], count[i]);
            }
            return ret;
        }
        public static void UnzipArrays(out string[] words, out int[] count, Dictionary<string, int> source)
        {
            KeyValuePair<string, int>[] l = source.ToArray();

            words = new string[l.Length];
            count = new int[l.Length];

            for(int i = 0; i < l.Length; i++)
            {
                words[i] = l[i].Key;
                count[i] = l[i].Value;
            }
        }
    }
}