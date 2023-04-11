using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;

namespace CBIMS.LDP.Def
{
    public class RdfInstPersist : RdfNodePersist, IRdfInstEditable
    {
        //TODO: what if different objects have the same INode?

        public IEnumerable<IRdfClassDef> Types => _Types.Values;


        ConcurrentDictionary<string, IRdfClassDef> _Types = new ConcurrentDictionary<string, IRdfClassDef>();
        ConcurrentDictionary<string, ISet<object>> _Props = new ConcurrentDictionary<string, ISet<object>>();

        public string Label
        {
            get
            {
                return GetPropSingle<string>(RDFSCommonDef.label.QName);
            }
            set
            {
                SetProp(RDFSCommonDef.label.QName, value);
            }
        }
        
        public IEnumerable<string> Comment => GetProp<string>(RDFSCommonDef.comment.QName);
        public void AddComment(string comment) => AddProp(RDFSCommonDef.comment.QName, comment);
        public void SetComments(IEnumerable<string> comments) => SetProps<string>(RDFSCommonDef.comment.QName, comments);
        



        public RdfInstPersist(RdfNSDef ns, string name, IRdfClassDef type, IUriNode node = null) : base(ns, name, node)
        {
            if (type != null)
                AddType(type);
        }

        // IRdfInst

        public IEnumerable<T> GetProp<T>(string qname)
        {
            var vals = GetProp(qname);
            if (vals.Any())
            {
                return vals.Cast<T>();
            }
            return Enumerable.Empty<T>();
        }
        public IEnumerable<object> GetProp(string qname)
        {
            if (_Props.ContainsKey(qname))
            {
                return _Props[qname];
            }
            return Enumerable.Empty<object>();
        }
        public T GetPropSingle<T>(string qname)
        {
            var val = GetPropSingle(qname);
            if (val != null)
            {
                return (T)val;
            }
            return default(T);
        }
        public object GetPropSingle(string qname)
        {
            if (_Props.ContainsKey(qname) && _Props.Count > 0)
            {
                return _Props[qname].FirstOrDefault();
            }
            return null;
        }
        public bool HasProp(string qname) => _Props.ContainsKey(qname) && _Props.Count > 0;

        public bool HasPropVal(string qname, object v)
        {
            if (_Props.ContainsKey(qname))
            {
                return _Props[qname].Contains(v);
            }
            return false;
        }


        
        // IRdfInstEditable

        public IRdfInstEditable AddType(IRdfClassDef type)
        {
            if (!_Types.ContainsKey(type.QName))
            {
                _Types.TryAdd(type.QName, type);
                if (Graph != null)
                    AssertProp(RDFSCommonDef.a.QName, type.ToNode(Graph));
            }
            return this;
        }

        public IRdfInstEditable AddType(RdfNSDef typeNS, string typeName)
        {
            string typeQName = typeNS.PrefixNC + ":" + typeName;
            if (!_Types.ContainsKey(typeQName))
            {
                RdfURIClassDef def = new RdfURIClassDef(typeNS, typeName, null, false, null, null);

                _Types.TryAdd(typeQName, def);
                if (Graph != null)
                    AssertProp(RDFSCommonDef.a.QName, def.ToNode(Graph));
            }
            return this;
        }

        public IRdfInstEditable AddProp(string key, object val)
        {
            if (val != null)
            {
                var node = val.ToNode(Graph);
                if (node != null)
                {
                    if (!_Props.ContainsKey(key))
                    {
                        _Props[key] = new HashSet<object>();
                    }
                    _Props[key].Add(val);
                    AssertProp(key, node);
                }
            }
            return this;
        }
        public IRdfInstEditable SetProp(string key, object val)
        {
            RemovePropAll(key);
            if (val != null)
            {
                AddProp(key, val);
            }
            return this;
        }
        public IRdfInstEditable SetProps(string key, IEnumerable<object> vals)
        {
            RemovePropAll(key);
            if (vals != null)
            {
                foreach (var val in vals)
                {
                    AddProp(key, val);
                }
            }
            return this;
        }

        public IRdfInstEditable SetProps<T>(string key, IEnumerable<T> vals)
        {
            RemovePropAll(key);
            if (vals != null)
            {
                foreach (var val in vals)
                {
                    AddProp(key, val);
                }
            }
            return this;
        }

        public IRdfInstEditable RemovePropAll(string key)
        {
            if (_Props.ContainsKey(key))
            {
                _Props.TryRemove(key, out var val);
                RetractPropAll(key);
            }
            return this;
        }
        public IRdfInstEditable RemoveProp(string key, object val)
        {
            if (_Props.ContainsKey(key))
            {
                _Props[key].Remove(val);
                RetractProp(key, val.ToNode(Graph));
            }
            return this;
        }


        // Graph handlers

        protected void AssertProp(INode pred, INode obj)
        {
            if(Graph != null)
            {
                try
                {
                    Graph.Assert(Node, pred, obj);
                }
                catch(IndexOutOfRangeException)
                {
                    // try again, something might be wrong in concurrent system
                    Graph.Assert(Node, pred, obj);
                }
                
            }
                
        }
        protected void AssertProp(string pred_qname, INode obj)
        {
            if (Graph != null)
            {
                var pred = Graph.CreateUriNode(pred_qname);
                AssertProp(pred, obj);
            } 
        }

        protected void RetractProp(INode pred, INode obj)
        {
            if (Graph != null)
            {
                try
                {
                    Graph.Retract(Node, pred, obj);
                }
                catch (IndexOutOfRangeException)
                {
                    // try again, something might be wrong in concurrent system
                    Graph.Retract(Node, pred, obj);
                }
            }
                
        }
        protected void RetractProp(string pred_qname, INode obj)
        {
            if (Graph != null)
            {
                var pred = Graph.CreateUriNode(pred_qname);
                RetractProp(pred, obj);
            }
        }
        protected void RetractPropAll(INode pred)
        {
            if (Graph != null)
            {
                foreach (var trip in Graph.GetTriplesWithSubjectPredicate(Node, pred).ToArray())
                {
                    try
                    {
                        Graph.Retract(trip);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // try again, something might be wrong in concurrent system
                        Graph.Retract(trip);
                    }
                }
            }
        }
        protected void RetractPropAll(string pred_qname)
        {
            if (Graph != null)
            {
                var pred = Graph.CreateUriNode(pred_qname);
                RetractPropAll(pred);
            }
        }


    }
}
