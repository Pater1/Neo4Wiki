using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PageScore;

namespace PageScorePOC
{
    class Program
    {
        static string xml = @"
            <body>
                <h1>Hello World</h1>
                <p>This is a test!</p>
                <p>oUEfnoue <br/>OEifE9f23jrr2 24840 4810 ---</p>
            </body>";
        static void Main(string[] args)
        {
            Dictionary<string, int> cnt = xml.GetWordCountDictFromXMLString();
            foreach(KeyValuePair<string, int> p in cnt)
            {
                Console.WriteLine($"{p.Key}: {p.Value}");
            }
        }
    }
}
