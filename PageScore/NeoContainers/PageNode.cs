using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using driver = Neo4j.Driver.V1;
using client = Neo4jClient;
using PageScore;

namespace NeoContainers
{
    public struct PageNode: driver.INode
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
        public void WriteToDatabase(client.GraphClient graphClient)
        {
            graphClient.Connect();

            using (client.Transactions.ITransaction trans = graphClient.BeginTransaction())
            {
                graphClient.Cypher.Create($"(n:Page {LabelsAsString})").WithParams(this).ExecuteWithoutResults();

                trans.Commit();
            }
        }

        public long ID { get; }
        public string Title { get; }
        public string Text { get; }
        public Dictionary<string, int> WordCountDict { get; }
        private string[] WordList => WordCountDict.Select(x => x.Key).ToArray();
        private int[] WordCount => WordCountDict.Select(x => x.Value).ToArray();

        public PageNode(long iD, string title, string text) : this()
        {
            ID = iD;
            Title = title;
            Text = text;
            WordCountDict = text.GetWordCountDictFromXMLString();
        }
        public PageNode(long iD, string title, string text, string[] words, int[] count) : this()
        {
            ID = iD;
            Title = title;
            Text = text;
            WordCountDict = ZipTogetherArrays(words, count);
        }

        private static Dictionary<string, int> ZipTogetherArrays(string[] words, int[] count)
        {
            Dictionary <string, int> ret = new Dictionary<string, int>();
            for(int i = 0; i < words.Length; i++)
            {
                ret.Add(words[i], count[i]);
            }
            return ret;
        }

        #region INode
        public object this[string key] => Properties[key];

        public IReadOnlyList<string> Labels => Properties.ToList().Select(x => x.Key).ToList();
        public string LabelsAsString
        {
            get
            {
                string ret = "{";
                IReadOnlyList<string> lbls = Labels;
                bool first = true;
                foreach(string s in lbls)
                {
                    ret += (first? "": ", ") + s + ": {" + s + "}";
                    first = false;
                }
                ret += "}";
                Console.WriteLine(ret);
                return ret;
            }
        }

        private Dictionary<string, object> properties;
        public IReadOnlyDictionary<string, object> Properties
        {
            get
            {
                if(properties == null)
                {
                    properties = new Dictionary<string, object>();
                    properties.Add("ID", ID);
                    properties.Add("Title", Title);
                    properties.Add("Text", Text);
                    properties.Add("WordList", WordList);
                    properties.Add("WordCount", WordCount);
                }
                else
                {
                    properties["ID"] = ID;
                    properties["Title"] = Title;
                    properties["Text"] = Text;
                    properties["WordList"] = WordCountDict.Select(x => x.Key).ToArray();
                    properties["WordCount"] = WordCountDict.Select(x => x.Value).ToArray();
                }
                return properties;
            }
        }

        public long Id => ID;
        
        public bool Equals(driver.INode other)
        {
            if (other.GetType() != typeof(PageNode)) return false;

            PageNode pn = (PageNode)other;
            if (pn.ID != ID) return false;
            if (pn.Title != Title) return false;

            return true;
        }
        #endregion
    }
}
