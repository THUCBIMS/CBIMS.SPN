using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Nodes;

namespace CBIMS.SPN
{
    public class Place : LDPContainer<INode>
    {
        public SPNModel Model { get; }
        public AbstractRule ColorRule => GetPropSingle<AbstractRule>("spn:colorRule");
        public AbstractRule InitRule => GetPropSingle<AbstractRule>("spn:initRule");

        // MultiSet support
        private ConcurrentDictionary<INode, int> _ContentCount = new ConcurrentDictionary<INode, int>();


        internal List<ArcT2P> _Input_Arcs = new List<ArcT2P>();
        internal List<ArcP2T> _Output_Arcs = new List<ArcP2T>();

        internal bool KEEP_UNUSED_MULTI_COUNT = true;

        internal Place(SPNModel model, string name, AbstractRule colorRule, AbstractRule initRule, IUriNode node = null) : base(model.NS, name, SPNDefs.Place, node)
        {
            Model = model;
            SetProp("spn:colorRule", colorRule);
            SetProp("spn:initRule", initRule);
        }

        public void AddToken(INode token, int count = 1)
        {
            if (token == null)
                return;
            if (count <= 0)
                return;

            if (!_ContentCount.ContainsKey(token))
            {
                _ContentCount[token] = count;
                AddContent(token);

                _SetMultiCount(0, count, token);
            }
            else
            {
                int old_count = _ContentCount[token];
                _ContentCount[token] += count;
                int new_count = _ContentCount[token];

                _SetMultiCount(old_count, new_count, token);
            }
        }

        public bool RemoveToken(INode token, int count = 1)
        {
            if (token == null)
                return false;
            if (count <= 0)
                return false;
            if (!_ContentCount.ContainsKey(token))
                return false;
            if (count > _ContentCount[token])
                return false;

            if (count == _ContentCount[token])
            {
                _ContentCount.TryRemove(token, out int val);
                RemoveContent(token);
                _SetMultiCount(count, 0, token);
            }
            else
            {
                int old_count = _ContentCount[token];
                _ContentCount[token] -= count;
                int new_count = _ContentCount[token];

                _SetMultiCount(old_count, new_count, token);
            }
            return true;
        }

        public void AddToken(object token, int count = 1)
        {
            AddToken(token.ToNode(Graph), count);
        }
        public void RemoveToken(object token, int count = 1)
        {
            RemoveToken(token.ToNode(Graph), count);
        }

        public int HasToken(INode node)
        {
            if (_ContentCount.ContainsKey(node))
            {
                return _ContentCount[node];
            }
            return 0;
        }

        private void _assertTriple(INode subj, INode pred, INode obj)
        {
            try
            {
                Graph.Assert(subj, pred, obj);
            }
            catch (IndexOutOfRangeException)
            {
                // try again, something might be wrong in concurrent system
                Graph.Assert(subj, pred, obj);
            }
        }

        private void _SetMultiCount(int old_count, int new_count, INode token)
        {
            

            var a = Graph.CreateUriNode("rdf:type");
            var multi_num = Graph.CreateUriNode("spn:multi_num");
            var multi_usage = Graph.CreateUriNode("spn:multi_usage");


            var token_node = token.ToNode(Graph);



            if (old_count > 1)
            {
                var old_multi = Graph.CreateUriNode($"spn:multi_{old_count}");

                Graph.Retract(Node, old_multi, token_node);

                if (!KEEP_UNUSED_MULTI_COUNT)
                {
                    var old_usage_triple = Graph.GetTriplesWithSubjectPredicate(old_multi, multi_usage).First();

                    long old_usage_num = (old_usage_triple.Object as LongNode).AsInteger();

                    Graph.Retract(old_usage_triple);


                    if (old_usage_num <= 1)
                    {
                        Graph.Retract(Graph.GetTriplesWithSubjectPredicate(old_multi, multi_num).First());
                        Graph.Retract(Graph.GetTriplesWithSubjectPredicate(old_multi, a).First());
                    }
                    else
                    {
                        _assertTriple(old_multi, multi_usage, (old_usage_num - 1).ToNode(Graph));
                    }

                }

            }


            

            if (new_count > 1)
            {
                var new_multi = Graph.CreateUriNode($"spn:multi_{new_count}");
                _assertTriple(new_multi, a, SPNDefs.MultiNumProperty.Node);

                var new_usage_triple = Graph.GetTriplesWithSubjectPredicate(new_multi, multi_usage).FirstOrDefault();
                if (new_usage_triple == null)
                {
                    _assertTriple(new_multi, multi_num, new_count.ToNode(Graph));
                    if (!KEEP_UNUSED_MULTI_COUNT)
                    {
                        _assertTriple(new_multi, multi_usage, 1.ToNode(Graph));
                    }
                }
                else
                {
                    if (!KEEP_UNUSED_MULTI_COUNT)
                    {
                        long new_usage_num = (new_usage_triple.Object as LongNode).AsInteger();
                        Graph.Retract(new_usage_triple);
                        _assertTriple(new_multi, multi_usage, (new_usage_num + 1).ToNode(Graph));
                    }
                }
                _assertTriple(Node, new_multi, token_node);
            }

        }




        internal void _BindingConsume(Dictionary<INode, int> tokens)
        {
            foreach (var token in tokens.Keys)
            {
                RemoveToken(token, tokens[token]);
            }
        }

        internal void _BindingGenerate(Dictionary<INode, int> tokens)
        {
            foreach (var token in tokens.Keys)
            {
                AddToken(token, tokens[token]);
            }
        }


        public void ClearToken()
        {
            Dictionary<INode, int> tokens = new Dictionary<INode, int>(_ContentCount);

            _BindingConsume(tokens);
        }


    }
}
