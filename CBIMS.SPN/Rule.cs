using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;
using VDS.RDF;

namespace CBIMS.SPN
{
    public class ArgDef : RdfInstPersist
    {
        public string argName => GetPropSingle<string>("spn:argName");
        public IEnumerable<INode> argType => GetProp<INode>("spn:argType");

        internal ArgDef(RdfNSDef ns, string name, string argName, IUriNode node = null) : base(ns, name, SPNDefs.ArgDef, node)
        {
            SetProp("spn:argName", argName);
        }

    }

    public abstract class AbstractRule : RdfInstPersist
    {
        protected AbstractRule(RdfNSDef ns, string name, IRdfClassDef type, IUriNode node = null) : base(ns, name, type, node)
        {
        }
    }

    public class SPARQLRule : AbstractRule
    {
        public string hasSPARQL => GetPropSingle<string>("spn:hasSPARQL");
        internal SPARQLRule(RdfNSDef ns, string name, string SPARQL, IUriNode node = null) : base(ns, name, SPNDefs.SPARQLRule, node)
        {
            SetProp("spn:hasSPARQL", SPARQL);
        }
    }

    public class ConstantRule : AbstractRule
    {
        public object hasValue => GetPropSingle("spn:hasValue");

        internal ConstantRule(RdfNSDef ns, string name, object value, IUriNode node = null) : base(ns, name, SPNDefs.ConstantRule, node)
        {
            SetProp("spn:hasValue", value);
        }
    }

    public class ArgRule : AbstractRule
    {
        public ArgDef hasArg => GetPropSingle<ArgDef>("spn:hasArg");

        internal ArgRule(RdfNSDef ns, string name, ArgDef arg, IUriNode node = null) : base(ns, name, SPNDefs.ArgRule, node)
        {
            SetProp("spn:hasArg", arg);
        }
    }

    public class CompoundRule : AbstractRule
    {
        public string Operator => GetPropSingle<string>("spn:operator").ToUpperInvariant();
        public IEnumerable<AbstractRule> subRules => GetProp<AbstractRule>("spn:subRule");
        internal CompoundRule(RdfNSDef ns, string name, string _operator, IEnumerable<AbstractRule> subRules, IUriNode node = null) : base(ns, name,  SPNDefs.CompoundRule, node)
        {
            SetProp("spn:operator", _operator);
            SetProps("spn:subRule", subRules);
        }
    }

    public class ConditionRule : AbstractRule
    {
        public AbstractRule If => GetPropSingle<AbstractRule>("spn:if");
        public AbstractRule Then => GetPropSingle<AbstractRule>("spn:then");
        public AbstractRule Else => GetPropSingle<AbstractRule>("spn:else");
        internal ConditionRule(RdfNSDef ns, string name, AbstractRule _if, AbstractRule then, AbstractRule _else, IUriNode node = null) : base(ns, name,  SPNDefs.ConditionRule, node)
        {
            SetProp("spn:if", _if);
            SetProp("spn:then", then);
            SetProp("spn:else", _else);
        }
    }
}
