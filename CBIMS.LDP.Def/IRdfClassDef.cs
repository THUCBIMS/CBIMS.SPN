using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace CBIMS.LDP.Def
{
    public interface IRdfClassDef : IRdfNode
    {
        
    }

    public interface IRdfURIClassDef : IRdfClassDef
    {
        IEnumerable<IRdfURIClassDef> SuperClasses { get; }
        bool IsAbstract { get; }
        IEnumerable<IRdfPropDef> PropDefs { get; }
        IRdfPropDef GetPropDef(string qname);
        Type CLI_Type { get; }
    }

    public interface IRdfLiteralClassDef : IRdfClassDef
    {
        LiteralBasic BasicType { get; }
        IRdfLiteralClassDef SuperType { get; }
    }

    public class RdfURIClassDef : RdfInstPersist, IRdfURIClassDef
    {
        public IEnumerable<IRdfURIClassDef> SuperClasses => GetProp<IRdfURIClassDef>(RDFSCommonDef.subClassOf.QName);
        public bool IsAbstract { get; }
        public IEnumerable<IRdfPropDef> PropDefs { get; }
        public Type CLI_Type { get; }


        protected Dictionary<string, IRdfPropDef> _PropDefs = new Dictionary<string, IRdfPropDef>();
        public RdfURIClassDef(RdfNSDef ns, string name, 
            IRdfURIClassDef superClass, bool isAbstract, Type cli_Type, IUriNode node = null,
            Action<RdfURIClassDef> actionOnInitialize = null) 
            : base(ns, name, null, node)
        {
            SetClass();
            SetProp(RDFSCommonDef.subClassOf.QName, superClass);
            IsAbstract = isAbstract;
            CLI_Type = cli_Type;

            if (actionOnInitialize != null)
            {
                actionOnInitialize(this);
            }
                
        }

        protected virtual void SetClass()
        {
            SetProp(RDFSCommonDef.a.QName, RDFSCommonDef.Class);
        }

        public IRdfPropDef GetPropDef(string qname)
        {
            if (_PropDefs.ContainsKey(qname))
                return _PropDefs[qname];
            if (SuperClasses != null)
            {
                foreach (IRdfURIClassDef cls in SuperClasses)
                {
                    var item = cls.GetPropDef(qname);
                    if(item != null)
                        return item;
                }
            }
            return null;
        }


        public IRdfPropDef AddPropDef(IRdfPropDef def)
        {
            if (!_PropDefs.ContainsKey(def.QName))
            {
                _PropDefs[def.QName] = def;
            }

            if(def is RdfPropDef _def)
            {
                _def.AddDomain(this);
            }

            return def;
        }
    }

    public class RdfLiteralClassDef : RdfInstPersist, IRdfLiteralClassDef
    {
        public LiteralBasic BasicType { get; }
        public IRdfLiteralClassDef SuperType => GetPropSingle<IRdfLiteralClassDef>(RDFSCommonDef.subClassOf.QName);

        public RdfLiteralClassDef(LiteralBasic basicType, RdfNSDef ns, string name, IUriNode node = null) : base(ns, name, RDFSCommonDef.DataType, node)
        {
            BasicType = basicType;
        }

        public RdfLiteralClassDef(RdfNSDef ns, string name, IRdfLiteralClassDef superType, IUriNode node = null) : base(ns, name, RDFSCommonDef.DataType, node)
        {
            SetProp(RDFSCommonDef.subClassOf.QName, superType);
            BasicType = superType.BasicType;
        }
    }

    public class OwlClassDef : RdfURIClassDef
    {
        public OwlClassDef(RdfNSDef ns, string name, IRdfURIClassDef superClass, bool isAbstract, Type cli_Type, 
            IUriNode node = null, Action<RdfURIClassDef> actionOnInitialize = null) : base(ns, name, superClass, isAbstract, cli_Type, node, actionOnInitialize)
        {
        }

        protected override void SetClass()
        {
            SetProp(RDFSCommonDef.a.QName, OWLCommonDef.Class);
        }

        public void AddPropDef(IRdfPropDef def)
        {
            if (!_PropDefs.ContainsKey(def.QName))
            {
                _PropDefs[def.QName] = def;
            }
        }
    }



    public class OwlRestrictionDef : RdfURIClassDef
    {
        public OwlRestrictionDef(RdfNSDef ns, string name, IUriNode node = null) : base(ns, name, null, true, null, node)
        {
        }

        protected override void SetClass()
        {
            SetProp(RDFSCommonDef.a.QName, OWLCommonDef.Restriction);
        }

    }



    public class OwlDataRangeDef : RdfURIClassDef
    {
        public OwlDataRangeDef(IList<object> contents, RdfNSDef ns, string name, IUriNode node = null) : base(ns, name, null, true, null, node)
        {
            RdfReadOnlyList list = new RdfReadOnlyList(contents, ns, name + "_0");
            this.AddProp(OWLCommonDef.oneOf.QName, list);
        }
        protected override void SetClass()
        {
            SetProp(RDFSCommonDef.a.QName, OWLCommonDef.DataRange);
        }

    }
}
