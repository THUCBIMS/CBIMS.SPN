using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace CBIMS.LDP.Def
{
    public class RdfNSDef
    {
        public RdfNSDef ParentNS { get; protected set; }
        public string RelativePath { get; protected set; }
        public string PrefixNC { get; protected set; } // NC - No Colon ':' 
        public IGraph Graph { get; protected set; }
        public string FullPath { get 
            { 
                if (ParentNS == null) 
                    return RelativePath;

                var parent = ParentNS.FullPath;
                if(parent.EndsWith("#"))
                    parent = parent.Substring(0, parent.Length - 1);
                return parent + RelativePath; 
            } 
        }

        public RdfNSDef(string prefix, IGraph graph)
        {
            ParentNS = null;
            RelativePath = graph.BaseUri.AbsoluteUri;
            PrefixNC = prefix;
            Graph = graph;
        }

        public RdfNSDef(RdfNSDef parentNS, string relativePath, string prefixNC, bool createGraph, bool useOWL = true)
        {
            ParentNS = parentNS;
            RelativePath = relativePath;
            PrefixNC = prefixNC;

            if (createGraph)
            {
                Graph = new Graph();
                Graph.BaseUri = new Uri(FullPath);
                Graph.NamespaceMap.AddNamespace(prefixNC, Graph.BaseUri);

                if (useOWL)
                {
                    Graph.NamespaceMap.AddNamespace(RdfCommonNS.OWL.PrefixNC, new Uri(RdfCommonNS.OWL.FullPath));
                }

            }
        }
    }

}
