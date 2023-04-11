using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace CBIMS.LDP.Repo
{
    public interface IRdfModel : IRdfNode
    {
        IGraph Graph { get; }
        IRepository Host { get; }

        IRdfNode GetInstByUri(string uri);
        bool HasInstUri(string uri);
        void AddInst(IRdfNode inst);
    }



    public class RdfModel : RdfInstPersist, IRdfModel
    {
        public IRepository Host { get; }

        public override string FullPath => base.FullPath.EndsWith("#") ?
            base.FullPath.Substring(0, base.FullPath.Length - 1) :
            base.FullPath;

        public override string QName => NS.PrefixNC;

        private IUriNode _Node = null;
        public override IUriNode Node
        {
            get {
                if (_Node == null && Graph != null)
                {
                    _Node = ToNode(Graph) as IUriNode;
                }
                return _Node; 
            }
        }
            
            
        public string GraphUri => Graph.BaseUri.AbsoluteUri;


        private Dictionary<string, IRdfNode> _contentNodeUriMap 
            = new Dictionary<string, IRdfNode>(); //<node, inst>

        public IRdfNode GetInstByUri(string uri)
        {
            if (_contentNodeUriMap.ContainsKey(uri))
                return _contentNodeUriMap[uri];
            return null;
        }
        public IRdfNode GetInstByUri(IUriNode uri)
        {
            return GetInstByUri(uri.Uri.AbsoluteUri);
        }
        
        public bool HasInstUri(string uri)
        {
            if (_contentNodeUriMap.ContainsKey(uri))
                return true;
            return false;
        }
        public bool HasInstUri(IUriNode uri)
        {
            return HasInstUri(uri.Uri.AbsoluteUri);
        }
        
        public void AddInst(IRdfNode inst)
        {
            if (inst.Node != null)
                _contentNodeUriMap[inst.Node.Uri.AbsoluteUri] = inst;
            else
                _contentNodeUriMap[inst.FullPath] = inst;
        }

        public RdfModel(RdfNSDef ns, IRepository host) : base(ns, ns.PrefixNC, null)
        {
            Host = host;
            host.AddModel(this);
        }

        public override INode ToNode(IGraph graph)
        {
            var uri = new Uri(FullPath);
            if (graph != null)
            {
                return graph.CreateUriNode(uri);
            }
            else if (Graph != null)
            {
                return Graph.CreateUriNode(uri);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
