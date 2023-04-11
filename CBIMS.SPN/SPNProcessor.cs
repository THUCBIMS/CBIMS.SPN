using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Nodes;
using VDS.RDF.Shacl.Validation;
using static System.Net.Mime.MediaTypeNames;

namespace CBIMS.SPN
{
    internal class SPNProcessor : SparqlQuerier
    {
        public SPNProcessor(SPNModel model) : base(model.Host as IQueryableRepository)
        {
            
        }

        internal bool CheckForBoolean(AbstractRule rule, SparqlInjectee injectee)
        {
            if (rule is ConstantRule constRule)
            {
                object val = constRule.hasValue;
                if (val == null)
                    return false;
                if (val is bool boolVal)
                    return boolVal;
                if(val is BooleanNode boolNode)
                    return boolNode.AsBoolean();

                return true;
            } 
            else if (rule is SPARQLRule sparqlRule)
            {
                if(!string.IsNullOrEmpty(sparqlRule.hasSPARQL))
                    return QueryForBoolean(sparqlRule.hasSPARQL, injectee);

                return false;
            }
            else if (rule is CompoundRule compRule)
            {
                int subCount = compRule.subRules.Count();
                if (subCount == 0)
                {
                    throw new InvalidOperationException("0 sub-rules");
                }

                switch (compRule.Operator)
                {
                    case "NOT":
                        {
                            if (subCount != 1)
                            {
                                throw new InvalidOperationException("must be only 1 sub-rule with operator \"NOT\"");
                            }
                            return !CheckForBoolean(compRule.subRules.First(), injectee);
                        }
                    case "AND":
                        {
                            foreach (var sub in compRule.subRules)
                            {
                                if(!CheckForBoolean(sub, injectee))
                                    return false;
                            }
                            return true;
                        }
                    case "OR":
                        {
                            foreach (var sub in compRule.subRules)
                            {
                                if (CheckForBoolean(sub, injectee))
                                    return true;
                            }
                            return false;
                        }
                    case "XOR":
                        {
                            if (subCount != 2)
                            {
                                throw new InvalidOperationException("must be only 2 sub-rule2 with operator \"XOR\"");
                            }
                            var opA = CheckForBoolean(compRule.subRules.First(), injectee);
                            var opB = CheckForBoolean(compRule.subRules.Last(), injectee);
                            return opA != opB;
                        }
                    default:
                        throw new InvalidOperationException(compRule.Operator);
                }
            }
            else if (rule is ConditionRule condRule)
            {
                bool if_result = CheckForBoolean(condRule.If, injectee);
                if (if_result)
                {
                    return CheckForBoolean(condRule.Then, injectee);
                }
                else
                {
                    return CheckForBoolean(condRule.Else, injectee);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal ArgTuples CheckForTuple(AbstractRule rule, SparqlInjectee injectee, string[] args = null)
        {
            ArgTuples output = new ArgTuples(args);

            if (rule == null)
            {
                return output;
            }
            else if (rule is ConstantRule constRule)
            {
                object val = constRule.hasValue;

                if (val == null)
                    return output;

                var node = val.ToNode(null);
                if (node == null)
                    return output;

                if (args == null)
                    output.Args = new string[] { "?RESULT" };

                output.Recs.Add(new INode[] { node });
                return output;
            }
            else if (rule is ArgRule argRule)
            {
                var arg = argRule.hasArg;
               

                if (arg == null)
                    throw new InvalidOperationException();

                string argName = arg.argName;

                output.Args = new string[] { argName };

                if (injectee.Binds.ContainsKey(argName))
                {
                    output.Recs.Add(new INode[] { injectee.Binds[argName] });
                }
                else if (injectee.Values.Args.Contains(argName))
                {
                    int index = injectee.Values.Args.ToList().IndexOf(argName);
                    foreach (var rec in injectee.Values.Recs)
                    {
                        output.Recs.Add(new INode[] { rec[index] });
                    }
                }
                else
                {
                    throw new InvalidOperationException("arg not found");
                }
                return output;
            }
            else if (rule is SPARQLRule sparqlRule)
            {
                if (!string.IsNullOrEmpty(sparqlRule.hasSPARQL))
                {
                    var result = QueryForTuple(sparqlRule.hasSPARQL, injectee);
                    if (result != null)
                        return result;
                }
                return output;
            }
            else if (rule is CompoundRule compRule)
            {
                int subCount = compRule.subRules.Count();
                if (subCount == 0)
                {
                    throw new InvalidOperationException("0 sub-rules");
                }

                switch (compRule.Operator)
                {
                    case "AND":
                        {
                            List<ArgTuples> out_results = new List<ArgTuples>();
                            foreach (var sub in compRule.subRules)
                            {
                                out_results.Add(CheckForTuple(sub, injectee));
                            }
                            var intersect = _intersect(out_results);
                            if(intersect != null)
                                return intersect;
                            else
                                return output;
                        }
                    case "OR":
                        {
                            List<ArgTuples> out_results = new List<ArgTuples>();
                            foreach (var sub in compRule.subRules)
                            {
                                out_results.Add(CheckForTuple(sub, injectee));
                            }
                            var union = _union(out_results);
                            if (union != null)
                                return union;
                            else
                                return output;
                        }
                    case "XOR":
                    case "NOT":
                    default:
                        throw new InvalidOperationException(compRule.Operator);
                }
            }
            else if (rule is ConditionRule condRule)
            {
                bool if_result = CheckForBoolean(condRule.If, injectee);
                if (if_result)
                {
                    return CheckForTuple(condRule.Then, injectee, args);
                }
                else
                {
                    return CheckForTuple(condRule.Else, injectee, args);
                }
            }
            else if(rule is InternalInvRule_SelectUnion selUnionRule)
            {
                List<ArgTuples> out_results = new List<ArgTuples>();
                foreach (var path in selUnionRule.Paths)
                {
                    SparqlInjectee _injectee  = injectee.Clone();

                    var firstRule = path.First().Item2;

                    ArgTuples _result = CheckForTuple(firstRule, _injectee, args);

                    if (!_result.Recs.Any())
                        break;

                    //pass the current value as the injectee for next step
                    _injectee.Values = _result.Clone();

                    for (int i = 1; i < path.Length; i++)
                    {
                        bool op = path[i].Item1;
                        AbstractRule filterRule = path[i].Item2;

                        var new_result = CheckForTuple(filterRule, _injectee, args);

                        if (op)
                            _result = new_result;
                        else
                            _result = _except(_result, new_result);

                        if (!_result.Recs.Any())
                            break;
                    }
                    if (_result.Recs.Any())
                        out_results.Add(_result);
                }
                var union = _union(out_results);
                if (union != null)
                    return union;
                else
                    return output;
            }
            else if (rule is InternalInvRule_SelectIntersect selIntersectRule)
            {
                List<ArgTuples> out_results = new List<ArgTuples>();
                foreach (var path in selIntersectRule.Paths)
                {
                    SparqlInjectee _injectee = injectee.Clone();


                    var firstRule = path.First().Item2;

                    ArgTuples _result = CheckForTuple(firstRule, _injectee, args);

                    if (!_result.Recs.Any())
                        break;

                    //pass the current value as the injectee for next step
                    _injectee.Values = _result.Clone();

                    for (int i = 1; i < path.Length; i++)
                    {
                        bool op = path[i].Item1;
                        AbstractRule filterRule = path[i].Item2;

                        var new_result = CheckForTuple(filterRule, _injectee, args);

                        if (op)
                            _result = new_result;
                        else
                            _result = _except(_result, new_result);

                        if (!_result.Recs.Any())
                            break;
                    }
                    if (_result.Recs.Any())
                        out_results.Add(_result);
                }
                var intersect = _intersect(out_results);
                if (intersect != null)
                    return intersect;
                else
                    return output;
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        private ArgTuples _intersect(ArgTuples current, ArgTuples next)
        {
            var intersetcArgs = current.Args.Intersect(next.Args).ToArray();

            ArgTuples output = new ArgTuples(intersetcArgs);

            List<INode[]> current_recs = _reform(current, intersetcArgs);
            List<INode[]> next_recs = _reform(next, intersetcArgs);

            foreach (var rec in current_recs)
            {
                if (_containsRec(rec, next_recs))
                {
                    output.Recs.Add(rec);
                }
            }
           
            return output;
        }

        private List<INode[]> _reform(ArgTuples current, string[] intersetcArgs)
        {
            List<INode[]> output = new List<INode[]>();

            Dictionary<int, int> argIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < intersetcArgs.Length; i++)
            {
                int old_i = Array.IndexOf(current.Args, intersetcArgs[i]);
                argIndexMap[i] = old_i;
            }

            foreach (var rec in current.Recs)
            {
                INode[] new_rec = new INode[intersetcArgs.Length];
                for (int i = 0; i < intersetcArgs.Length; i++)
                {
                    var old_i = argIndexMap[i];
                    if(old_i < 0)
                    {
                        new_rec[i] = null;
                    }
                    else
                    {
                        new_rec[i] = rec[old_i];
                    }
                }
                output.Add(new_rec);
            }

            return output;
        }

        private ArgTuples _intersect(List<ArgTuples> results)
        {
            ArgTuples current = null;
            foreach (var result in results)
            {
                if (current == null)
                {
                    current = result;
                }
                else
                {
                    current = _intersect(current, result);
                }
            }
            return current;
        }

        private ArgTuples _union(ArgTuples current, ArgTuples next)
        {
            var unionArgs = current.Args.Union(next.Args).ToArray();

            ArgTuples output = new ArgTuples(unionArgs);

            List<INode[]> current_recs = _reform(current, unionArgs);
            List<INode[]> next_recs = _reform(next, unionArgs);

            Dictionary<string, INode[]> resultsMap = new Dictionary<string, INode[]>();

            foreach (var result in current_recs)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var val in result)
                {
                    sb.Append(val.ToString());
                    sb.Append("|");
                }
                var identifier = sb.ToString();
                resultsMap[identifier] = result;
            }

            foreach (var result in next_recs)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var val in result)
                {
                    if(val != null)
                        sb.Append(val.ToString());
                    sb.Append("|");
                }
                var identifier = sb.ToString();
                resultsMap[identifier] = result;
            }

            output.Recs = resultsMap.Values.ToList();

            return output;
        }

        private ArgTuples _union(List<ArgTuples> results)
        {
            ArgTuples current = null;
            foreach (var result in results)
            {
                if (current == null)
                {
                    current = result;
                }
                else
                {
                    current = _union(current, result);
                }
            }
            return current;
        }


        private ArgTuples _except(ArgTuples current, ArgTuples next)
        {
            if(!next.Recs.Any())
            {
                return current;
            }

            var args = current.Args;

            ArgTuples output = new ArgTuples(args);

            List<INode[]> current_recs = _reform(current, args);
            List<INode[]> next_recs = _reform(next, args);

            Dictionary<string, INode[]> resultsMap = new Dictionary<string, INode[]>();

            foreach (var result in current_recs)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var val in result)
                {
                    sb.Append(val.ToString());
                    sb.Append("|");
                }
                var identifier = sb.ToString();
                resultsMap[identifier] = result;
            }

            foreach (var result in next_recs)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var val in result)
                {
                    sb.Append(val.ToString());
                    sb.Append("|");
                }
                var identifier = sb.ToString();
                if(resultsMap.ContainsKey(identifier))
                    resultsMap.Remove(identifier);
            }

            output.Recs = resultsMap.Values.ToList();

            return output;
        }

        private bool _containsRec(INode[] rec, List<INode[]> recs)
        {
            return recs.Exists(t => _match(t, rec) == true);
        }

        private bool _match(INode[] args1, INode[] args2)
        {
            if (args1.Length != args2.Length)
                return false;
            for (int i = 0; i < args1.Length; i++)
            {
                if (args1[i].ToString() != args2[i].ToString())
                    return false;
            }
            return true;
        }

        internal static void _AddBindingArgsToInjectee(SparqlInjectee injectee, Binding binding, IEnumerable<string> args)
        {
            HashSet<string> visited = new HashSet<string>();
            foreach (var arg in args)
            {
                if (binding.HasArg(arg))
                {
                    var argVal = binding.GetArg(arg);
                    injectee.Binds.Add(arg, argVal);
                    visited.Add(arg);
                }
                else
                {
                    throw new InvalidOperationException($"unfulfilled arg {arg}");
                }
            }

            //other args

            foreach (var arg in binding.Keys)
            {
                if (!visited.Contains(arg))
                {
                    var argVal = binding.GetArg(arg);
                    injectee.Binds.Add(arg, argVal);
                }
            }
        }


        internal static void _AddTokenSet(ref Dictionary<INode, int> current, ArgTuples tuples)
        {
            foreach (var rec in tuples.Recs)
            {
                foreach(var node in rec)
                _AddTokenSet(ref current, node, 1);
            }
        }

        internal static void _AddTokenSet(ref Dictionary<INode, int> current, INode argVal, int count)
        {
            if (!current.ContainsKey(argVal))
            {
                current.Add(argVal, count);
            }
            else
            {
                current[argVal] += count;
            }
        }

        internal static bool _CheckConsume(Place relPlace, Dictionary<INode, int> tokensToConsume, out string warn)
        {
            warn = null;
            foreach (var node in tokensToConsume.Keys)
            {
                var count = tokensToConsume[node];
                var has = relPlace.HasToken(node);
                if (has < count)
                {
                    warn = $"not enough token {node} in place {relPlace.Name}";
                    return false;
                }
            }
            return true;
        }

        internal ArgTuples ForceCheckForTuple(string out_arg, AbstractRule rule, SparqlInjectee injectee)
        {
            ArgTuples output = new ArgTuples(new string[] { out_arg });

            int count = injectee.Values.Recs.Count;

            if (rule == null)
            {
                return output;
            }
            else if (rule is ConstantRule constRule)
            {
                object val = constRule.hasValue;

                if (val == null)
                    return output;

                var node = val.ToNode(null);
                if (node == null)
                    return output;

                for(int i=0;i< count; i++)
                {
                    output.Recs.Add(new INode[] { node });
                }
                
                return output;
            }
            else if (rule is ArgRule argRule)
            {
                var arg = argRule.hasArg;

                if (arg == null)
                    throw new InvalidOperationException();

                string argName = arg.argName;

                if (injectee.Binds.ContainsKey(argName))
                {
                    for (int i = 0; i < count; i++)
                    {
                        output.Recs.Add(new INode[] { injectee.Binds[argName] });
                    }
                }
                else if (injectee.Values.Args.Contains(argName))
                {
                    int index = injectee.Values.Args.ToList().IndexOf(argName);
                    foreach (var rec in injectee.Values.Recs)
                    {
                        output.Recs.Add(new INode[] { rec[index] });
                    }
                }
                else
                {
                    throw new InvalidOperationException("arg not found");
                }
                return output;
            }
            else if (rule is SPARQLRule sparqlRule)
            {
                if (!string.IsNullOrEmpty(sparqlRule.hasSPARQL))
                {
                    string sparql = sparqlRule.hasSPARQL.Trim();
                    if (sparql.StartsWith("ASK"))
                    {
                        sparql = $"SELECT {out_arg} " + sparql.Substring(3);
                    }
                    return QueryForTuple(sparql, injectee);
                }
                return null;
            }
            else if (rule is CompoundRule compRule)
            {
                int subCount = compRule.subRules.Count();
                if (subCount == 0)
                {
                    throw new InvalidOperationException("0 sub-rules");
                }

                switch (compRule.Operator)
                {
                    case "AND":
                        {
                            ArgTuples current = null;
                            foreach (var sub in compRule.subRules)
                            {
                                if (current == null)
                                {
                                    current = ForceCheckForTuple(out_arg, sub, injectee);
                                }
                                else
                                {
                                    var next = ForceCheckForTuple(out_arg, sub, injectee);
                                    current = _intersect(current, next);
                                }
                                if (!current.Recs.Any())
                                    break;
                            }
                            return current;
                        }
                    case "OR":
                        {
                            ArgTuples current = null;
                            foreach (var sub in compRule.subRules)
                            {
                                if (current == null)
                                {
                                    current = ForceCheckForTuple(out_arg, sub, injectee);
                                }
                                else
                                {
                                    var next = ForceCheckForTuple(out_arg, sub, injectee);
                                    current = _union(current, next);
                                }
                            }
                            return current;
                        }
                    case "XOR":
                        {
                            if (subCount != 2)
                            {
                                throw new InvalidOperationException("must be only 2 sub-rule2 with operator \"XOR\"");
                            }
                            var opA = ForceCheckForTuple(out_arg, compRule.subRules.First(), injectee);
                            var opB = ForceCheckForTuple(out_arg, compRule.subRules.Last(), injectee);
                            return _union(_except(opA, opB), _except(opB, opA));
                        }
                    case "NOT":
                        {
                            ArgTuples current = new ArgTuples(new string[] { out_arg });
                            int index = injectee.Values.Args.ToList().IndexOf(out_arg);
                            foreach (var rec in injectee.Values.Recs)
                            {
                                current.Recs.Add(new INode[] { rec[index] });
                            }

                            if (!current.Recs.Any())
                            {
                                return current;
                            }

                            var sub = compRule.subRules.First();
                            ArgTuples next = ForceCheckForTuple(out_arg, sub, injectee);
                            current = _except(current, next);
                            return current;
                        }
                    default:
                        throw new InvalidOperationException(compRule.Operator);
                }
            }
            else if (rule is ConditionRule condRule)
            {
                var if_result = ForceCheckForTuple(out_arg, condRule.If, injectee);

                //TODO: Split

                throw new NotImplementedException();
                //if (if_result)
                //{
                //    return CheckForTuple(condRule.Then, injectee, args);
                //}
                //else
                //{
                //    return CheckForTuple(condRule.Else, injectee, args);
                //}
            }
            else
            {
                throw new NotImplementedException();
            }
        }

    }
}
