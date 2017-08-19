using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace PageScore
{
    public static class DocParseHelper
    {
        public static Dictionary<string, int> GetWordCountDictFromXMLString(this string xml)
        {
            return xml.GetXMLContentRaw().GetTextContentRaw().CountTextWords();
        }

        public static string GetXMLContentRaw(this string xml)
        {
            XmlDocument xmdoc = new XmlDocument();
            xmdoc.LoadXml(xml);

            return GetXMLContentRaw(xmdoc);
        }
        
        /// <summary>
        ///     Conform a Cypher create text for a type
        ///     Code from: https://stackoverflow.com/questions/37254403/c-sharp-neo4jclient-adding-two-new-nodes-with-a-new-relationship-inside-a-transa
        /// </summary>
        /// <typeparam name="TObject">The type to handle</typeparam>
        /// <returns>A string like "{o:TObject {tobject})</returns>
        static public string KeyFor<TObject>(string quearyName)
        {
            var name = typeof(TObject).Name;
            return $"({quearyName}:{name} {{{name.ToLower()}}})";
        }

        //public static string GetXMLContentRaw(this XmlDocument xml)
        //{
        //    foreach (XmlNode childrenNode in xml)
        //    {
        //        ret += childrenNode.InnerText + " ";
        //        Console.WriteLine(ret);
        //    }

        //    Console.WriteLine(ret + "\n\n");
        //}
        public static string GetXMLContentRaw(this XmlNode xml)
        {
            string ret = "";
            foreach (XmlNode node in xml)
            {
                if (node.HasChildNodes)
                {
                    foreach (XmlNode child in node.ChildNodes)
                    {
                        ret += child.GetXMLContentRaw();
                    }
                }
                else
                {
                    ret += node.InnerText + " ";
                }
            }
            return ret;
        }

        

public static string GetTextContentRaw(this string text)
        {
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            return rgx.Replace(text, "").ToLower();
        }

        public static Dictionary<string, int> CountTextWords(this string normalized)
        {
            Dictionary<string, int> ret = new Dictionary<string, int>();
            string[] strs = normalized.Split(' ');
            foreach(string st in strs)
            {
                if (string.IsNullOrEmpty(st)) continue;
                if (!ret.ContainsKey(st))
                {
                    ret.Add(st, 1);
                }
                else
                {
                    ret[st]++;
                }
            }
            return ret;
        }
    }
}