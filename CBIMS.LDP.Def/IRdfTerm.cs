using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace CBIMS.LDP.Def
{
    public enum LiteralBasic
    {
        NONE,
        STRING,
        INTEGER,
        DOUBLE,
        BOOLEAN,
        OTHER
    }

    public interface IRdfTerm
    {
        INode ToNode(IGraph graph);
    }

    public interface IRdfNode : IRdfTerm
    {
        RdfNSDef NS { get; }
        string Name { get; }
        string QName { get; }
        string FullPath { get; }
        IUriNode Node { get; }
    }

    public abstract class RdfNodePersist : IRdfNode
    {
        public RdfNSDef NS { get; protected set; }
        public virtual string Name { get; protected set; }
        public virtual string QName => NS?.PrefixNC + ":" + Name;
        public virtual string FullPath => NS?.FullPath + Name;
        public virtual IUriNode Node { get; protected set; }

        public virtual INode ToNode(IGraph graph) 
        {
            if (Node != null)
            {
                return Node.ToNode(graph);
            }
            else 
            {
                var uri = new Uri(NS.FullPath + Name);
                if (graph != null)
                {
                    return graph.CreateUriNode(uri);
                }
                else if(Graph != null)
                {
                    return Graph.CreateUriNode(uri);
                }
                else
                {
                    //throw new InvalidOperationException();
                    return null;
                }
            }
        }
            

        public IGraph Graph => NS.Graph;


        protected RdfNodePersist(RdfNSDef ns, string name, IUriNode node = null)
        {
            NS = ns;
            Name = name;
            Node = node;

            if (NS == null)
            {
                throw new InvalidOperationException("Null NameSpace");
            }

            if (Graph != null)
            {
                if (Node == null || Node.Graph != Graph)
                {
                    Node = ToNode(Graph) as IUriNode;
                }
            }
            else
            {
                //pass
            }

        }

    }



}
