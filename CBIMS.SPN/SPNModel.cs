using AngleSharp.Dom;
using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;

namespace CBIMS.SPN
{
    public class SPNModel : RdfModel
    {
        public Dictionary<string, Place> Places = new Dictionary<string, Place>(); //name after colon, instance
        public Dictionary<string, Transition> Transitions = new Dictionary<string, Transition>();
        public Dictionary<string, ArcP2T> ArcsP2T = new Dictionary<string, ArcP2T>();
        public Dictionary<string, ArcT2P> ArcsT2P = new Dictionary<string, ArcT2P>();


        private Dictionary<string, SPARQLRule> SparqlRules = new Dictionary<string, SPARQLRule>();
        private Dictionary<object, ConstantRule> ConstantRules = new Dictionary<object, ConstantRule>();
        private Dictionary<object, ArgRule> ArgRules = new Dictionary<object, ArgRule>();
        private Dictionary<string, CompoundRule> CompoundRules = new Dictionary<string, CompoundRule>();
        private Dictionary<string, ConditionRule> ConditionRules = new Dictionary<string, ConditionRule>();

        private Dictionary<string, ArgDef> ArgDefs = new Dictionary<string, ArgDef>();


        public ConstantRule NullRule;


        internal SPNProcessor Processor { get; }

        public SparqlQuerier SparqlQuerier => Processor;

        public SPNModel(RdfNSDef ns, IQueryableRepository host) : base(ns, host)
        {
            Processor = new SPNProcessor(this);
            NullRule = new ConstantRule(NS, "ctrule_NULL", null);

            ns.Graph.AddPrefix(SPNDefs.NS_SPN);
            ns.Graph.AddPrefix(RdfCommonNS.LDP);
            Host.NamespaceMap.Import(this.Graph.NamespaceMap);
        }



        public void Init()
        {
            _InitPlaces();
            _CheckPlaceColors();
            _CheckTransitionArgs();
        }



        private void _InitPlaces()
        {
            foreach(var place in Places.Values)
            {
                if (place.InitRule != null)
                {
                    ArgTuples result = Processor.CheckForTuple(place.InitRule, null);
                    if (result != null)
                    {
                        foreach(var rec in result.Recs)
                        {
                            foreach (VDS.RDF.INode val in rec)
                            {
                                place.AddToken(val);
                            }
                        }
                    }
                }
            }
        }

        private void _CheckPlaceColors()
        {
            foreach (var place in Places.Values)
            {
                if (place.ColorRule != null && place.Contents.Any())
                {

                    foreach (var content in place.Contents)
                    {
                        SparqlInjectee injectee = new SparqlInjectee();
                        injectee.Binds.Add("?TOKEN", content.ToNode(Graph));

                        var result = Processor.CheckForBoolean(place.ColorRule, injectee);
                        if (!result)
                        {
                            throw new InvalidOperationException($"ColorRule not pass in place {place.Name}: {content.ToNode(Graph)}");
                        }
                    }

                }
            }
        }

        private void _CheckTransitionArgs()
        {

            foreach (var transition in Transitions.Values)
            {
                HashSet<string> arc_args = new HashSet<string>();
                foreach(var arc in transition._Input_Arcs)
                {
                    var arc_argNames = _getArgNames(arc.hasArg);
                    arc_args.UnionWith(arc_argNames);
                }

                foreach (var arc in transition._Output_Arcs)
                {
                    var arc_argNames = _getArgNames(arc.hasArg);
                    arc_args.UnionWith(arc_argNames);
                }

                var trans_argNames = _getArgNames(transition.hasArg);

                arc_args.ExceptWith(trans_argNames);

                if (arc_args.Count > 0)
                {
                    throw new InvalidOperationException($"unfulfilled args for transition {transition.Name}: "
                        + string.Join(",", arc_args));
                }

            }
        }


        private static IEnumerable<string> _getArgNames(IEnumerable<object> args)
        {
            foreach (var arg in args)
            {
                yield return _getArgName(arg);
            }
        }

        private static string _getArgName(object arg)
        {
            string argName = null;
            if (arg is string)
            {
                argName = (string)arg;
            }
            else if (arg is ArgDef argdef)
            {
                argName = argdef.argName;
            }
            else
            {
                throw new NotImplementedException();
            }
            return argName;
        }
        public void LinkPTP(Place p1, Transition t, Place p2, string arg)
        {
            CreateArcP2T(p1, t, null, null, arg);
            CreateArcT2P(t, p2, null, arg);
        }

        public bool Trigger(Transition trans, Binding binding, out IEnumerable<Place> modifiedPlaces, out string warn)
        {
            return trans._Trigger(binding, out modifiedPlaces, out warn);
        }


        // Get

        public Transition GetTransitionByName(string name)
        {
            if (Transitions.ContainsKey(name))
            {
                return Transitions[name];
            }
            return null;
        }

        public Place GetPlaceByName(string name)
        {
            if (Places.ContainsKey(name))
            {
                return Places[name];
            }
            return null;
        }


        // Creators

        public Place CreatePlace(string name, AbstractRule colorRule, AbstractRule initRule)
        {
            if (Places.ContainsKey(name))
            {
                throw new InvalidOperationException("Place name conflict");
            }
            Places[name] = new Place(this, name, colorRule, initRule);
            return Places[name];
        }


        public Transition CreateTransition(string name, AbstractRule guardRule, IEnumerable<ArgDef> args)
        {
            if (Transitions.ContainsKey(name))
            {
                throw new InvalidOperationException("Transition name conflict");
            }
            Transitions[name] = new Transition(this, name, guardRule, args);
            return Transitions[name];
        }

        public Transition CreateTransition(string name, AbstractRule guardRule, IEnumerable<string> argNames)
        {
            if (Transitions.ContainsKey(name))
            {
                throw new InvalidOperationException("Transition name conflict");
            }

            List<ArgDef> args = new List<ArgDef>();
            foreach(var argName in argNames)
            {
                ArgDef arg = CreateArgDef(argName, null);
                args.Add(arg);
            }

            Transitions[name] = new Transition(this, name, guardRule, args);
            return Transitions[name];
        }

        public ArcP2T CreateArcP2T(Place relPlace, Transition relTransition, 
            AbstractRule arcExpr, AbstractRule arcExprInv, IEnumerable<ArgDef> args)
        {
            string name = $"ArcP2T_{relPlace.Name}_{relTransition.Name}";
            if(ArcsP2T.ContainsKey(name))
            {
                throw new InvalidOperationException("ArcP2T name conflict");
            }
            var arc = new ArcP2T(this, name, relPlace, relTransition, arcExpr, arcExprInv, args);
            ArcsP2T[name] = arc;
            relPlace._Output_Arcs.Add(arc);
            relTransition._Input_Arcs.Add(arc);

            return ArcsP2T[name];
        }
        public ArcP2T CreateArcP2T(Place relPlace, Transition relTransition, 
            AbstractRule arcExpr, AbstractRule arcExprInv, string arg)
        {
            return CreateArcP2T(relPlace, relTransition, arcExpr, arcExprInv, new ArgDef[] { CreateArgDef(arg, null) });
        }

        public ArcT2P CreateArcT2P(Transition relTransition, Place relPlace, AbstractRule arcExpr, IEnumerable<ArgDef> args)
        {
            string name = $"ArcT2P_{relTransition.Name}_{relPlace.Name}";
            if (ArcsT2P.ContainsKey(name))
            {
                throw new InvalidOperationException("ArcT2P name conflict");
            }
            var arc = new ArcT2P(this, name, relPlace, relTransition, arcExpr, args);
            ArcsT2P[name] = arc;
            relPlace._Input_Arcs.Add(arc);
            relTransition._Output_Arcs.Add(arc);
            return ArcsT2P[name];
        }
        public ArcT2P CreateArcT2P(Transition relTransition, Place relPlace, AbstractRule arcExpr, string arg)
        {
            return CreateArcT2P(relTransition, relPlace, arcExpr, new ArgDef[] { CreateArgDef(arg, null) });
        }

        public SPARQLRule CreateSparqlRule(string rule)
        {
            if (!SparqlRules.ContainsKey(rule))
            {
                SparqlRules[rule] = new SPARQLRule(NS, $"sprule_{SparqlRules.Count}", rule);
            }
            return SparqlRules[rule];
        }

        public SPARQLRule CreateSparqlRule_OfType(string qName, bool allowSubType = true, string additionalRuleForTOKEN = null)
        {
            if (allowSubType)
            {
                return CreateSparqlRule($"SELECT ?TOKEN {{ ?TOKEN a/rdfs:subClassOf* {qName}. {additionalRuleForTOKEN} }}");
            }
            else
            {
                return CreateSparqlRule($"SELECT ?TOKEN {{ ?TOKEN a {qName}. {additionalRuleForTOKEN} }}");
            }
            
        }


        public ConstantRule CreateConstantRule(object value)
        {
            if (value == null)
                return NullRule;

            if (!ConstantRules.ContainsKey(value))
            {
                ConstantRules[value] = new ConstantRule(NS, $"ctrule_{ConstantRules.Count}", value);
            }
            return ConstantRules[value];
        }

        public ArgRule CreateArgRule(ArgDef arg)
        {
            if (!ArgRules.ContainsKey(arg))
            {
                ArgRules[arg] = new ArgRule(NS, $"agrule_{ConstantRules.Count}", arg);
            }
            return ArgRules[arg];
        }

        public CompoundRule CreateCompoundRule(string _operator, IEnumerable<AbstractRule> subRules)
        {
            var names = subRules.Select(t => t.Name);
            string name = $"{_operator}({string.Join(",", names)})";
            if (!CompoundRules.ContainsKey(name))
            {
                CompoundRules[name] = new CompoundRule(NS, $"cprule_{CompoundRules.Count}", _operator, subRules);
            }
            return CompoundRules[name];
        }
        public ConditionRule CreateConditionRule(AbstractRule _if, AbstractRule then, AbstractRule _else)
        {
            string name = $"{_if.Name},{then.Name},{_else.Name}";
            if (!ConditionRules.ContainsKey(name))
            {
                ConditionRules[name] = new ConditionRule(NS, $"cdrule_{ConditionRules.Count}", _if, then, _else);
            }
            return ConditionRules[name];
        }

        public ArgDef CreateArgDef(string argName, IEnumerable<string> typeQNames)
        {
            string identifier = argName;
            if (typeQNames != null)
            {
                var sorted = typeQNames.ToList();
                sorted.Sort();
                identifier = $"{argName}({string.Join(",", sorted)})";
            }
            if (!ArgDefs.ContainsKey(identifier))
            {
                var output = new ArgDef(NS, $"arg_{ArgDefs.Count}", argName, null);
                if (typeQNames != null)
                {
                    foreach (var qname in typeQNames)
                    {
                        var typeNode = Graph.CreateUriNode(qname);
                        output.AddProp("spn:argType", typeNode);
                    }
                }


                ArgDefs[identifier] = output;
            }
            return ArgDefs[identifier];
        }








        public bool CanBatchTrigger(Transition trans, SPNModelRunner runner)
        {
            return trans._CanBatchTrigger(runner);
        }

        public bool BatchTrigger(Transition trans, SPNModelRunner runner, out string warn)
        {
            var result = trans._BatchTrigger(runner, out IEnumerable<Place> modifiedPlaces, out warn);
            runner.UpdateOptions(modifiedPlaces);
            return result;
        }
    }
}
