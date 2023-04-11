using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace CBIMS.LDP.Def
{
    public static class GraphExtensions
    {
        public static StringNode ToStringNode(this string value, IGraph graph)
        {
            return new StringNode(graph, value);
        }

        public static LongNode ToLongNode(this int value, IGraph graph)
        {
            return new LongNode(graph, value);
        }
        public static LongNode ToLongNode(this long value, IGraph graph)
        {
            return new LongNode(graph, value);
        }

        public static DoubleNode ToDoubleNode(this double value, IGraph graph)
        {
            return new DoubleNode(graph, value);
        }
        public static BooleanNode ToBooleanNode(this bool value, IGraph graph)
        {
            return new BooleanNode(graph, value);
        }

        public static INode ToNode(this object value, IGraph graph)
        {
            if (value == null)
                return null;
            if (value is INode node)
            {
                if (graph != null)
                {
                    return graph.AddNode(node);
                }
                else
                {
                    return node;
                }
            }
            if (value is Uri uri)
            {
                if (graph != null)
                {
                    return graph.CreateUriNode(uri);
                }
                else
                {
                    return uri.AbsoluteUri.ToStringNode(null);
                }
            }
            if (value is IRdfTerm term)
                return term.ToNode(graph);
            if (value is string strVal)
                return strVal.ToStringNode(graph);
            if (value is int intVal)
                return intVal.ToLongNode(graph);
            if (value is long longVal)
                return longVal.ToLongNode(graph);
            if (value is double doubleVal)
                return doubleVal.ToDoubleNode(graph);
            if (value is bool boolVal)
                return boolVal.ToBooleanNode(graph);

            throw new InvalidCastException("ToNode() not implemented: " + value.GetType().FullName);
        }

        public static INode AddNode(this IGraph graph, INode node)
        {

            if (node == null)
            {
                return null;
            }
            else if (graph == null || node.Graph == graph)
            {
                return node;
            }
            else if (node is IBlankNode)
            {
                return graph.CreateBlankNode();
            }
            else if (node is IUriNode UNode)
            {
                return graph.CreateUriNode(UNode.Uri);
            }
            else if (node is ILiteralNode LNode)
            {
                if (LNode.DataType != null)
                    return graph.CreateLiteralNode(LNode.Value, LNode.DataType);
                else if (LNode.Language != null)
                    return graph.CreateLiteralNode(LNode.Value, LNode.Language);
                else
                    return graph.CreateLiteralNode(LNode.Value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void AddPrefix(this IGraph graph, string prefixNC, Uri uri)
        {
            graph?.NamespaceMap.AddNamespace(prefixNC, uri);
        }
        public static void AddPrefix(this IGraph graph, RdfNSDef NS)
        {
            graph?.NamespaceMap.AddNamespace(NS.PrefixNC, new Uri(NS.FullPath));
        }
    
        //public static object FromNode(this INode node, IRdfModel)
        //{
        //    if(node is ILiteralNode LNode)
        //    {

        //    }
        //    else if(node is IUriNode UNode)
        //    {
        //        //TODO
        //        return node;
        //    }
        //    else
        //    {
        //        return node;
        //    }
        //}
    
    }
}
