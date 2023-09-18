// Copyright (C) 2023  Liu, Han; School of Software, Tsinghua University
//
// This file is part of CBIMS.SPN.
// CBIMS.SPN is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// CBIMS.SPN is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with CBIMS.SPN. If not, see <https://www.gnu.org/licenses/>.

using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Writing.Formatting;

namespace CBIMS.SPN
{
    internal abstract class InternalInvRuleBase : AbstractRule
    {
        internal (bool, AbstractRule)[][] Paths { get; }

        protected InternalInvRuleBase(RdfNSDef ns, string name, IRdfClassDef type, (bool, AbstractRule)[][] paths) 
            : base(ns, name, type, null)
        {
            Paths = paths;
        }
    }

    internal class InternalNegativeRule : AbstractRule
    {
        internal AbstractRule Inner;

        public InternalNegativeRule(RdfNSDef ns, string name, AbstractRule inner) 
            : base(ns, name, InvArcExprCal.InvRule_Negative, null)
        {
            this.Inner = inner;
            this.AddProp(InvArcExprCal.hasInner.QName, inner);
        }
    }

    internal class InternalInvRule_SelectUnion : InternalInvRuleBase
    {
        // select first node.
        // filter with the following nodes (each also a "select").
        // union the final results

        public InternalInvRule_SelectUnion(RdfNSDef ns, string name, (bool, AbstractRule)[][] paths) 
            : base(ns, name, InvArcExprCal.InvRule_SelectUnion, paths)
        {
            
        }

    }

    internal class InternalInvRule_SelectIntersect : InternalInvRuleBase
    {
        // select first node.
        // filter with the following nodes (each also a "select").
        // intersect the final results

        public InternalInvRule_SelectIntersect(RdfNSDef ns, string name, (bool, AbstractRule)[][] paths) 
            : base(ns, name, InvArcExprCal.InvRule_SelectIntersect, paths)
        {
        }
    }

    internal class InvArcExprCal
    {
        internal static readonly RdfURIClassDef InvRule
            = new RdfURIClassDef(SPNDefs.NS_SPN, "InvRule", SPNDefs.Rule, true, null);

        internal static readonly RdfURIClassDef InvRule_Negative
            = new RdfURIClassDef(SPNDefs.NS_SPN, "InvRule_Negative", InvRule, false, null);

        internal static readonly RdfURIClassDef InvRule_SelectUnion 
            = new RdfURIClassDef(SPNDefs.NS_SPN, "InvRule_SelectUnion", InvRule, false, null);

        internal static readonly RdfURIClassDef InvRule_SelectIntersect
            = new RdfURIClassDef(SPNDefs.NS_SPN, "InvRule_SelectIntersect", InvRule, false, null);

        internal static readonly RdfPropDef hasPath = new RdfPropDef(SPNDefs.NS_SPN, "hasPath", InvRule, RDFSCommonDef.List);
        internal static readonly RdfPropDef hasInner = new RdfPropDef(SPNDefs.NS_SPN, "hasInner", InvRule, RDFSCommonDef.List);

        SPNModelRunner Runner;

        SPNModel Model => Runner.Model;

        RdfNSDef NS => Model.NS;

        TurtleFormatter Formatter;

        internal static int InvCount = 1;

        public InvArcExprCal(SPNModelRunner runner)
        {
            Runner = runner;
            Formatter = new TurtleFormatter(Model.Graph.NamespaceMap);
        }

        public AbstractRule CalInvArcExpr(ArcP2T arc)
        {
            // arcExpr : from a binding to tokens, {?x, ?y} -> ?z in :place
            // arcExprInv : from tokens to bindings, :place -> table of [ {?x, ?y} ]


            AbstractRule rule = arc.arcExpr;
            var args = arc.hasArg.ToList();
            var place = arc.relPlace;
            var transition = arc.relTransition;

            AbstractRule guardSelect = null; //h0

            if (transition.guardRule != null)
            {
                guardSelect = _ToSelectRule(transition.guardRule, args);
            }

            var invRule = _GetInvArcExpr(args, rule, place);

            if (guardSelect != null)
                return _append(invRule, guardSelect);

            return invRule;
        }

        private AbstractRule _append(AbstractRule invRule, AbstractRule guardSelect)
        {
            var paths = new List<List<(bool, AbstractRule)>>();
            var path = new List<(bool, AbstractRule)>
            { 
                (true, invRule), (true, guardSelect) 
            };
            paths.Add(path);
            return CreateSelectUnion(paths);
        }

        private AbstractRule _GetInvArcExpr(List<ArgDef> args_xy, AbstractRule rule, Place place)
        {

            if (rule == null)
                throw new InvalidOperationException();


            if (args_xy.Count == 0)
            {
                throw new InvalidOperationException("zero args in ArcP2T");
            }

            if (rule is SPARQLRule spRule)
            {
                return _invLeafSPARQL(spRule, place, args_xy);
            }
            else if (rule is ConditionRule condRule)
            {
                List<List<(bool, AbstractRule)>> paths = _splitCompoundPaths(args_xy, condRule);

                List<List<(bool, AbstractRule)>> out_paths = new List<List<(bool, AbstractRule)>>();

                foreach (var path in paths)
                {
                    List<(bool, AbstractRule)> out_path = new List<(bool, AbstractRule)>();

                    (bool, AbstractRule) leaf = path.Last();
                    path.RemoveAt(path.Count - 1);

                    AbstractRule inv_leaf = _GetInvArcExpr(args_xy, leaf.Item2, place);
                    out_path.Add((true, inv_leaf));

                    foreach (var branchNode in path)
                    {
                        out_path.Add((branchNode.Item1, _ToSelectRule(branchNode.Item2, args_xy)));
                    }
                    out_paths.Add(out_path);
                }

                return CreateSelectUnion(out_paths);

            }
            else if (rule is CompoundRule compRule)
            {
                //usually not in the leaf of an arcExpr
                throw new InvalidOperationException();
            }
            else if (rule is ConstantRule constRule)
            {
                // only type constraint for ?x ?y
               
                _getArgTypeString(args_xy, out string content_type);

                string content = content_type;
                _getArgHeaderWithContentFilter(args_xy, content, out string header);
                string out_sparql = $"SELECT {header} WHERE {{ {content} }}";

                return Model.CreateSparqlRule(out_sparql);
            }
            else if (rule is ArgRule argRule)
            {
                // no constraints to the tokens.
                // the output arg is contained in the place.

                List<string> args_z = new List<string> { argRule.hasArg.argName };

                _getArgTypeString(args_xy, out string content_type);
                _getArgInPlaceString(place, args_z, out string content_contain);

                string content = $"{content_type} {content_contain}";
                _getArgHeaderWithContentFilter(args_xy, content, out string header);
                string out_sparql = $"SELECT {header} WHERE {{ {content} }}";

                return Model.CreateSparqlRule(out_sparql);
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        private List<string> _getHeaderArgs(string sparql)
        {
            var index = sparql.IndexOf('{');
            var prefix = sparql.Substring(0, index);
            var split = prefix.Split(' ', '\r', '\n', '\t');
            return split.Where(t => t.StartsWith("?")).ToList();
        }
        private string _getOldContent(string sparql)
        {
            var index = sparql.IndexOf('{');
            var last_index = sparql.LastIndexOf('}');

            var content_old = sparql.Substring(index + 1, last_index - index - 1);

            content_old = content_old.Trim();

            if (!content_old.EndsWith(".")
                && !content_old.EndsWith(")")
                && !content_old.EndsWith("}"))
                content_old += " . ";

            return content_old;
        }

        private void _getArgHeaderWithContentFilter(List<ArgDef> args, string content, out string header)
        {
            List<string> argNames = new List<string>();

            foreach(var item in args)
            {
                if (!content.Contains(item.argName))
                    continue;
                argNames.Add(item.argName);
            }

            header = string.Join(" ", argNames);
        }


        private void _getArgTypeString(List<ArgDef> args_xy, out string content_type)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var arg in args_xy)
            {
                if (arg.argType != null && arg.argType.Any())
                {
                    var _argTypes = arg.argType.Select(x => Formatter.Format(x)).ToList();

                    for (int i = _argTypes.Count - 1; i >= 0; i--)
                    {
                        var _type = _argTypes[i];
                        if (_type.StartsWith("xsd"))
                        {
                            //skip
                            _argTypes.RemoveAt(i);
                        }
                    }

                    if (!_argTypes.Any())
                        continue;


                    if (_argTypes.Count == 1)
                    {
                        sb.Append($"{arg.argName} a/rdfs:subClassOf* {_argTypes.First()} . ");
                    }
                    else
                    {
                        bool first = true;
                        foreach (var _argType in _argTypes)
                        {
                            if (first)
                                first = false;
                            else
                                sb.Append(" UNION ");

                            sb.Append($"{{ {arg.argName} a/rdfs:subClassOf* {_argType} . }}");
                        }
                    }
                }
                else
                {
                    //pass
                }
            }
            content_type = sb.ToString();
        }

        private void _getArgInPlaceString(Place place, List<string> args_z, out string content)
        {
            string placeStr = Formatter.Format(place.Node);
            StringBuilder sb = new StringBuilder();
            foreach (var argName in args_z)
            {
                sb.Append($"{placeStr} ldp:contains {argName} . ");
                //sb.Append($"OPTIONAL {{ {placeStr} ?multi {arg.argName} . ?multi spn:multi_num {__count(arg.argName)} }} ");
            }

            content = sb.ToString();
        }



        private AbstractRule _ToSelectRule(AbstractRule rule, List<ArgDef> args)
        {
            if(rule is ConstantRule constRule)
            {
                //TRUE or FALSE
                return constRule;
            }
            else if(rule is SPARQLRule spRule)
            {
                var sparql = spRule.hasSPARQL.Trim();
                if (sparql.StartsWith("ASK"))
                {
                    string content_old = _getOldContent(sparql);

                    _getArgTypeString(args, out string content_type);

                    string content = $"{content_old} {content_type}";
                    _getArgHeaderWithContentFilter(args, content, out string header);
                    string out_sparql = $"SELECT {header} WHERE {{ {content} }}";


                    return Model.CreateSparqlRule(out_sparql);
                }
                else
                {
                    //should be a boolean rule
                    throw new InvalidOperationException();
                }
            }
            else if(rule is CompoundRule compRule)
            {
                if(compRule.Operator == "AND")
                {
                    List<List<(bool, AbstractRule)>> paths = new List<List<(bool, AbstractRule)>>();
                    foreach(var item in compRule.subRules)
                    {
                        paths.Add(new List<(bool, AbstractRule)> { (true, _ToSelectRule(item, args)) });
                    }
                    return CreateSelectIntersect(paths);
                }
                else if(compRule.Operator == "OR")
                {
                    List<List<(bool, AbstractRule)>> paths = new List<List<(bool, AbstractRule)>>();
                    foreach (var item in compRule.subRules)
                    {
                        paths.Add(new List<(bool, AbstractRule)> { (true, _ToSelectRule(item, args)) });
                    }
                    return CreateSelectUnion(paths);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (rule is ConditionRule condRule)
            {
                List<List<(bool, AbstractRule)>> paths = _splitCompoundPaths(args, condRule);
                List<List<(bool, AbstractRule)>> out_paths = new List<List<(bool, AbstractRule)>>();
                foreach(var path in paths)
                {
                    List<(bool, AbstractRule)> new_path = new List<(bool, AbstractRule)>();
                    var leaf = path.Last();
                    new_path.Add((true, _ToSelectRule(leaf.Item2, args)));
                    for(int i=0;i< path.Count - 1; i++)
                    {
                        var pair = path[i];
                        new_path.Add((pair.Item1, _ToSelectRule(pair.Item2, args)));
                    }
                    out_paths.Add(new_path);
                }
                return CreateSelectUnion(out_paths);
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        private SPARQLRule _invLeafSPARQL(SPARQLRule spRule, Place place, List<ArgDef> args_xy)
        {
            string sparql = spRule.hasSPARQL.Trim();

            if (sparql.StartsWith("SELECT"))
            {
                // get the original combination of the binding args
                //
                // args:
                //   ?x a :type_x.
                //   ?y a :type_y.
                //
                // arcExpr:
                //
                // SELECT ?z WHERE {
                //   ?x :rel1 ?z.
                //   ?z :rel2 ?y.
                // }
                //
                // implicitly with :place ldp:contains ?z .
                //
                // Inv =>
                //
                // SELECT ?x ?y WHERE {
                //   ?x a :type_x.
                //   ?y a :type_y.
                //   :place ldp:contains ?z.
                //
                //   ?x :rel1 ?z.
                //   ?z :rel2 ?y.
                // }


                List<string> args_z = _getHeaderArgs(sparql);
                string content_old = _getOldContent(sparql);

                
                _getArgTypeString(args_xy, out string content_type);
                _getArgInPlaceString(place, args_z, out string content_contain);


                string content = $"{content_old} {content_type} {content_contain}";
                _getArgHeaderWithContentFilter(args_xy, content, out string header);
                string out_sparql = $"SELECT {header} WHERE {{ {content} }}";


                return Model.CreateSparqlRule(out_sparql);
            }
            else if (sparql.StartsWith("ASK"))
            {
                throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException();
            }
        }


        private List<List<(bool, AbstractRule)>> _splitCompoundPaths(List<ArgDef> args, AbstractRule rule)
        {
            List<List<(bool, AbstractRule)>> output = new List<List<(bool, AbstractRule)>>();
            
            if(rule == null)
            {
                throw new InvalidOperationException();
            }
            if (rule is ConditionRule condRule)
            {
                //List<(bool, AbstractRule)> then_branch = new List<(bool, AbstractRule)>();
                //List<(bool, AbstractRule)> else_branch = new List<(bool, AbstractRule)>();

                //then_branch.Add((true, condRule.If));
                //else_branch.Add((false, condRule.If));

                List<List<(bool, AbstractRule)>> paths_then = _splitCompoundPaths(args, condRule.Then);
                List<List<(bool, AbstractRule)>> paths_else = _splitCompoundPaths(args, condRule.Else);

                foreach(var branch in paths_then)
                {
                    branch.Insert(0, (true, condRule.If));
                    output.Add(branch);
                }

                foreach (var branch in paths_else)
                {
                    branch.Insert(0, (false, condRule.If));
                    output.Add(branch);
                }
            }
            else
            {
                output.Add(new List<(bool, AbstractRule)> { (true, rule) });
            }

            return output;
        }


        internal InternalInvRule_SelectUnion CreateSelectUnion(List<List<(bool, AbstractRule)>> paths)
        {
            (bool, AbstractRule)[][] _paths = paths.Select(t => t.ToArray()).ToArray();
            var output = new InternalInvRule_SelectUnion(NS, $"Inv_{InvCount++}", _paths);

            _writeToGraph(output);
            return output;
        }

        internal InternalInvRule_SelectIntersect CreateSelectIntersect(List<List<(bool, AbstractRule)>> paths)
        {
            (bool, AbstractRule)[][] _paths = paths.Select(t => t.ToArray()).ToArray();
            var output = new InternalInvRule_SelectIntersect(NS, $"Inv_{InvCount++}", _paths);

            _writeToGraph(output);
            return output;
        }


        private void _writeToGraph(InternalInvRuleBase invRule)
        {
            foreach (var path in invRule.Paths)
            {
                List<AbstractRule> list = new List<AbstractRule>();
                foreach (var pair in path)
                {
                    if (pair.Item1)
                    {
                        list.Add(pair.Item2);
                    }
                    else
                    {
                        list.Add(CreateNegative(pair.Item2));
                    }
                }

                RdfReadOnlyList _list = new RdfReadOnlyList(list, NS, "Inv_" + InvCount++);
                invRule.AddProp(hasPath.QName, _list);
            }
        }

        internal InternalNegativeRule CreateNegative(AbstractRule inner)
        {
            return new InternalNegativeRule(NS, $"Inv_{InvCount++}", inner);
        }


    }
}
