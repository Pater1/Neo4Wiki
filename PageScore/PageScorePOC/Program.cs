using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PageScore;
using NeoContainers;
using driver = Neo4j.Driver.V1;
using client = Neo4jClient;
using extention = Neo4jClient.Extension;
using cypher = Neo4jClient.Extension.Cypher;

namespace PageScorePOC
{
    class Program
    {
        static string xml1 = @"
            <body>
                <h1>Hello World</h1>
                <p>This is a test!</p>
                <p>oUEfnoue <br/>OEifE9f23jrr2 24840 4810 ---</p>
            </body>";
        static string xml2 = @"
            <body>
                <h1>How Goes?</h1>
            </body>";
        static string xml3 = @"
            <body>
                <p>This is a test!</p>
                <p>Or... Is it?!?</p>
            </body>";
        static void Main(string[] args)
        {
            client.GraphClient graphClient = new client.GraphClient(new Uri("http://localhost:7474/db/data"), "neo4j", "password");
            graphClient.Connect();

            //PageNode a = new PageNode(1, "Page1", xml1);
            //PageNode b = new PageNode(2, "Page2", xml2);
            //PageNode c = new PageNode(3, "Page3", xml3);

            //a.WriteToDatabase(graphClient);
            //b.WriteToDatabase(graphClient);
            //c.WriteToDatabase(graphClient);

            LinksTo.Build(graphClient, "Page1", "Page2");
        }
    }
}
