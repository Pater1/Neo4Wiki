using Neo4jClient;
using Neo4jClient.Cypher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using client = Neo4jClient;

namespace NeoContainers
{
    public class LinksTo : client.Relationship,
            client.IRelationshipAllowingSourceNode<PageNode>,
            client.IRelationshipAllowingTargetNode<PageNode>
    {
        public LinksTo(client.NodeReference<PageNode> targetNode)
                : base(targetNode)
        {
        }

        public override string RelationshipTypeKey => GetType().Name.ToUpper();

        public static void Build(client.GraphClient graphClient, string sourceTitle, string linkedTitle)
        {
            Node<PageNode> source =
                graphClient.Cypher
                .Match("(pagenode:PageNode)")
                .Where((PageNode pagenode) => pagenode.Title == sourceTitle)
                .Return(pagenode => pagenode.Node<PageNode>())
                .Results.Single();

            Node<PageNode> linked =
                graphClient.Cypher
                .Match("(pagenode:PageNode)")
                .Where((PageNode pagenode) => pagenode.Title == linkedTitle)
                .Return(pagenode => pagenode.Node<PageNode>())
                .Results.Single();

            graphClient.CreateRelationship(source.Reference, new LinksTo(linked.Reference));
        }
    }
}
