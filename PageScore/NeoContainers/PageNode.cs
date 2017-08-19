using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using driver = Neo4j.Driver.V1;
using client = Neo4jClient;
using PageScore;
using Neo4jClient.Cypher;

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
        private string[] WordList { get; set; }
        private int[] WordCount { get; set; }

        private string[] LinkedToList { get; set; }

        public int this[string key]
        {
            get
            {
                int i = Array.IndexOf(WordList, key);
                return WordCount[i];
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

        private static Dictionary<string, int> ZipTogetherArrays(string[] words, int[] count)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>();
            for (int i = 0; i < words.Length; i++)
            {
                ret.Add(words[i], count[i]);
            }
            return ret;
        }
        private static void UnzipArrays(out string[] words, out int[] count, Dictionary<string, int> source)
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

        //#region INode
        //public object this[string key] => Properties[key];

        //public IReadOnlyList<string> Labels => Properties.ToList().Select(x => x.Key).ToList();
        //public string LabelsAsString
        //{
        //    get
        //    {
        //        string ret = "{";
        //        IReadOnlyList<string> lbls = Labels;
        //        bool first = true;
        //        foreach(string s in lbls)
        //        {
        //            ret += (first? "": ", ") + s + ": {" + s + "}";
        //            first = false;
        //        }
        //        ret += "}";
        //        Console.WriteLine(ret);
        //        return ret;
        //    }
        //}

        //private Dictionary<string, object> properties;
        //public IReadOnlyDictionary<string, object> Properties
        //{
        //    get
        //    {
        //        if(properties == null)
        //        {
        //            properties = new Dictionary<string, object>();
        //            properties.Add("ID", ID);
        //            properties.Add("Title", Title);
        //            properties.Add("Text", Text);
        //            properties.Add("WordList", WordList);
        //            properties.Add("WordCount", WordCount);
        //        }
        //        else
        //        {
        //            properties["ID"] = ID;
        //            properties["Title"] = Title;
        //            properties["Text"] = Text;
        //            properties["WordList"] = WordCountDict.Select(x => x.Key).ToArray();
        //            properties["WordCount"] = WordCountDict.Select(x => x.Value).ToArray();
        //        }
        //        return properties;
        //    }
        //}

        //public long Id => ID;

        //public bool Equals(driver.INode other)
        //{
        //    if (other.GetType() != typeof(PageNode)) return false;

        //    PageNode pn = (PageNode)other;
        //    if (pn.ID != ID) return false;
        //    if (pn.Title != Title) return false;

        //    return true;
        //}
        //#endregion
    }
}