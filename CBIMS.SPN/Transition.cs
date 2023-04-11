using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;

namespace CBIMS.SPN
{
    public class Transition : RdfInstPersist
    {
        public SPNModel Model { get; }

        public AbstractRule guardRule => GetPropSingle<AbstractRule>("spn:guardRule");
        public IEnumerable<ArgDef> hasArg => GetProp<ArgDef>("spn:hasArg"); //string or ArgDef

        internal List<ArcP2T> _Input_Arcs = new List<ArcP2T>();
        internal List<ArcT2P> _Output_Arcs = new List<ArcT2P>();


        public delegate void TransitionEventHandler(Transition sender, Binding binding);

        public event TransitionEventHandler Triggering;
        public event TransitionEventHandler Triggered;
        public event TransitionEventHandler BindingChecking;
        public event TransitionEventHandler BindingAccepted;
        public event TransitionEventHandler BindingRejected;


        internal Transition(SPNModel model, string name, AbstractRule guardRule, IEnumerable<ArgDef> args, IUriNode node = null) : base(model.NS, name, SPNDefs.Transition, node)
        {
            Model = model;
            if (args == null || !args.Any())
            {
                throw new InvalidOperationException($"args can not be null: transition {name}");
            }

            SetProp("spn:guardRule", guardRule);
            SetProps("spn:hasArg", args);
        }

        internal IEnumerable<Binding> _GetOptions(SPNModelRunner runner)
        {
            if (runner._CanGetOptions_Transition(this))
            {
                return runner._GetOptions_Transition(this);
            }
            return null;
        }


        internal bool _Trigger(Binding binding, out IEnumerable<Place> modifiedPlaces, out string warn)
        {
            if (!binding.ContainsKey("?SELF"))
            {
                binding["?SELF"] = this.Node;
            }

            var _modifiedPlaces = new HashSet<Place>();
            modifiedPlaces = _modifiedPlaces;

            if (!_CheckTrigger(binding,
                out var tokensToConsume, out var tokensToGenerate, out warn))
                return false;

            Triggering?.Invoke(this, binding);

            _minusTokensToConsume(ref tokensToConsume, ref tokensToGenerate);


            foreach (var place in tokensToConsume.Keys)
            {
                if (tokensToConsume[place].Any())
                {
                    _modifiedPlaces.Add(place);
                    place._BindingConsume(tokensToConsume[place]);
                }
            }
            foreach (var place in tokensToGenerate.Keys)
            {
                if (tokensToGenerate[place].Any())
                {
                    _modifiedPlaces.Add(place);
                    place._BindingGenerate(tokensToGenerate[place]);
                }
            }

            Triggered?.Invoke(this, binding);
            return true;
        }

        private static void _minusTokensToConsume(ref Dictionary<Place, Dictionary<INode, int>> left, ref Dictionary<Place, Dictionary<INode, int>> right)
        {
            foreach (var place in left.Keys)
            {
                if (right.ContainsKey(place))
                {
                    _minusTokensToConsume_one(left[place], right[place]);
                }
            }
        }

        private static void _minusTokensToConsume_one(Dictionary<INode, int> left, Dictionary<INode, int> right)
        {
            foreach (var token in left.Keys.ToList())
            {
                if (right.ContainsKey(token))
                {
                    int left_count = left[token];
                    int right_count = right[token];
                    int minus = left_count - right_count;
                    if (minus == 0)
                    {
                        left.Remove(token);
                        right.Remove(token);
                    }
                    else if (minus > 0)
                    {
                        left[token] = minus;
                        right.Remove(token);
                    }
                    else
                    {
                        left.Remove(token);
                        right[token] = -minus;
                    }
                }
            }
        }



        internal bool _CheckTrigger(Binding binding,
            out Dictionary<Place, Dictionary<INode, int>> tokensToConsume,
            out Dictionary<Place, Dictionary<INode, int>> tokensToGenerate,
            out string warn)
        {
            BindingChecking?.Invoke(this, binding);

            tokensToConsume = new Dictionary<Place, Dictionary<INode, int>>();
            tokensToGenerate = new Dictionary<Place, Dictionary<INode, int>>();

            warn = null;
            if (!_CheckBindingArg(binding, out warn))
            {
                BindingRejected?.Invoke(this, binding);
                return false;
            }

            // check consume

            foreach (var input_arc in this._Input_Arcs)
            {
                if (!tokensToConsume.ContainsKey(input_arc.relPlace))
                    tokensToConsume[input_arc.relPlace] = new Dictionary<INode, int>();

                Dictionary<INode, int> current = tokensToConsume[input_arc.relPlace];

                var in_argNames = _getArgNames(input_arc.hasArg);

                if (input_arc.arcExpr != null)
                {
                    SparqlInjectee injectee = new SparqlInjectee();
                    SPNProcessor._AddBindingArgsToInjectee(injectee, binding, in_argNames);

                    var result = Model.Processor.CheckForTuple(input_arc.arcExpr, injectee);
                    SPNProcessor._AddTokenSet(ref current, result);
                }
                else
                {
                    foreach (var arg in in_argNames)
                    {
                        if (binding.HasArg(arg))
                        {
                            var argVal = binding.GetArg(arg);
                            SPNProcessor._AddTokenSet(ref current, argVal, 1);
                        }
                        else
                        {
                            throw new InvalidOperationException($"unfulfilled arg {arg}");
                        }
                    }
                }


                if (!SPNProcessor._CheckConsume(input_arc.relPlace, current, out warn))
                {
                    BindingRejected?.Invoke(this, binding);
                    return false;
                }

            }

            // check guard

            if (!_CheckGuard(binding, out warn))
            {
                BindingRejected?.Invoke(this, binding);
                return false;
            }



            // check generate

            foreach (var output_arc in this._Output_Arcs)
            {
                if (!tokensToGenerate.ContainsKey(output_arc.relPlace))
                    tokensToGenerate[output_arc.relPlace] = new Dictionary<INode, int>();

                Dictionary<INode, int> current = tokensToGenerate[output_arc.relPlace];

                var out_argNames = _getArgNames(output_arc.hasArg);

                if (output_arc.arcExpr != null)
                {
                    SparqlInjectee injectee = new SparqlInjectee();
                    SPNProcessor._AddBindingArgsToInjectee(injectee, binding, out_argNames);

                    var result = Model.Processor.CheckForTuple(output_arc.arcExpr, injectee);
                    SPNProcessor._AddTokenSet(ref current, result);
                }
                else
                {
                    
                    foreach (var arg in out_argNames)
                    {
                        if (binding.HasArg(arg))
                        {
                            var argVal = binding.GetArg(arg);
                            SPNProcessor._AddTokenSet(ref current, argVal, 1);
                        }
                        else
                        {
                            throw new InvalidOperationException($"unfulfilled arg {arg}");
                        }
                    }
                }

            }

            BindingAccepted?.Invoke(this, binding);
            return true;
        }


        private bool _CheckBindingArg(Binding binding, out string warn)
        {
            warn = null;

            var argNames = _getArgNames(this.hasArg);
            
            foreach (var argName in argNames)
            {
                if (!binding.HasArg(argName))
                {
                    warn = $"unfulfilled arg {argName} in binging for transition {this.Name}";
                    return false;
                }
            }
            return true;
        }

        private static IEnumerable<string> _getArgNames(IEnumerable<object> args)
        {
            foreach(var arg in args)
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

        private bool _CheckGuard(Binding binding, out string warn)
        {
            warn = null;
            if (this.guardRule != null)
            {
                SparqlInjectee injectee = new SparqlInjectee();

                var argNames = _getArgNames(this.hasArg);

                SPNProcessor._AddBindingArgsToInjectee(injectee, binding, argNames);

                bool result = Model.Processor.CheckForBoolean(this.guardRule, injectee);
                if (!result)
                {
                    warn = $"guard not pass: transition {this.Name}";
                    return false;
                }
            }
            return true;
        }



        internal bool _CanBatchTrigger(SPNModelRunner runner)
        {
            // for all-simple arcs, try Batch Trigger to speed-up

            if (_Input_Arcs.Count != 1)
            {
                return false;
            }

            if (_Input_Arcs[0].arcExpr != null)
                return false;
            if (_Input_Arcs[0].hasArg.Count() != 1)
                return false;

            foreach (var arc in _Output_Arcs)
            {
                if (arc.arcExpr != null)
                    return false;
                if (arc.hasArg.Count() != 1)
                    return false;
            }

            return runner._CanGetOptions_Transition(this);
        }

        internal bool _BatchTrigger(SPNModelRunner runner, out IEnumerable<Place> modifiedPlaces, out string warn)
        {
            // for all-simple arcs, try Batch Trigger to speed-up

            var _modifiedPlaces = new HashSet<Place>();
            modifiedPlaces = _modifiedPlaces;

            var input_place = _Input_Arcs[0].relPlace;
            var input_arg = _Input_Arcs[0].hasArg.First();

            var input_argName = _getArgName(input_arg);

            if (!_CheckBatchTrigger(runner, input_place, input_argName,
                out var tokensToConsume, out var tokensToGenerate, out warn))
                return false;

            //Triggering?.Invoke(this, binding);

            if (tokensToGenerate.ContainsKey(input_place))
            {
                _minusTokensToConsume_one(tokensToConsume, tokensToGenerate[input_place]);
            }



            if (tokensToConsume.Any())
            {
                _modifiedPlaces.Add(input_place);
                input_place._BindingConsume(tokensToConsume);
            }

            foreach (var place in tokensToGenerate.Keys)
            {
                if (tokensToGenerate[place].Any())
                {
                    _modifiedPlaces.Add(place);
                    place._BindingGenerate(tokensToGenerate[place]);
                }
            }

            //Triggered?.Invoke(this, binding);
            return true;
        }


        private bool _CheckBatchTrigger(SPNModelRunner runner, Place input_place, string input_arg, 
            out Dictionary<INode, int> tokensToConsume, out Dictionary<Place, Dictionary<INode, int>> tokensToGenerate, out string warn)
        {
            //BindingChecking?.Invoke(this, binding);
            tokensToConsume = new Dictionary<INode, int>();
            tokensToGenerate = new Dictionary<Place, Dictionary<INode, int>>();

            warn = null;
            SparqlInjectee injectee = new SparqlInjectee();

            var argNames = _getArgNames(this.hasArg);

            foreach (var arg in argNames)
            {
                if(arg == input_arg)
                {
                    continue;
                }
                else
                {
                    var val = runner.GetUserArg(this, arg);
                    if (val == null)
                    {
                        warn = $"unfulfilled arg {arg} in binging for transition {this.Name}";
                        return false;
                    }
                    else
                    {
                        injectee.Binds[arg] = val;
                    }
                }
            }

            injectee.Values.Args = new string[] { input_arg };
            injectee.Values.Recs.AddRange(input_place.Contents.Select(t => new INode[] { t }));

            // check guard

            if (!_CheckBatchGuard(input_arg, injectee, out tokensToConsume, out warn))
            {
                //BindingRejected?.Invoke(this, binding);
                return false;
            }


            if (!tokensToConsume.Any())
                return false;

            // check generate


            foreach (var output_arc in this._Output_Arcs)
            {
                if (!tokensToGenerate.ContainsKey(output_arc.relPlace))
                    tokensToGenerate[output_arc.relPlace] = new Dictionary<INode, int>();

                Dictionary<INode, int> current = tokensToGenerate[output_arc.relPlace];
                var out_argNames = _getArgNames(output_arc.hasArg);
                foreach (var arg in out_argNames)
                {
                    if(arg == input_arg)
                    {
                        foreach(var k in tokensToConsume.Keys)
                        {
                            current.Add(k, tokensToConsume[k]);
                        }
                    }
                    else if (injectee.Binds.ContainsKey(arg))
                    {
                        var argVal = injectee.Binds[arg];
                        SPNProcessor._AddTokenSet(ref current, argVal, tokensToConsume.Count);
                    }
                    else
                    {
                        throw new InvalidOperationException($"unfulfilled arg {arg}");
                    }
                }

            }

            //BindingAccepted?.Invoke(this, binding);
            return true;
        }

        private bool _CheckBatchGuard(string arg, SparqlInjectee injectee, out Dictionary<INode, int> tokensToConsume, out string warn)
        {
            warn = null;
            tokensToConsume = new Dictionary<INode, int>();

            if (this.guardRule != null)
            {

                ArgTuples result = Model.Processor.ForceCheckForTuple(arg, this.guardRule, injectee);

                if (!result.Recs.Any())
                {
                    warn = $"guard not pass: transition {this.Name}";
                    return false;
                }


                int index = result.Args.ToList().IndexOf(arg);

                if (index == -1)
                {
                    warn = $"invalid returned args";
                    return false;
                }

                foreach (var rec in result.Recs)
                {
                    var val = rec[index];
                    if (!tokensToConsume.ContainsKey(val))
                    {
                        tokensToConsume[val] = 1;
                    }
                    else
                    {
                        tokensToConsume[val]++;
                    }
                }

            }
            else
            {

                int index = injectee.Values.Args.ToList().IndexOf(arg);

                if (index == -1)
                {
                    warn = $"invalid returned args";
                    return false;
                }

                foreach (var rec in injectee.Values.Recs)
                {
                    var val = rec[index];
                    if (!tokensToConsume.ContainsKey(val))
                    {
                        tokensToConsume[val] = 1;
                    }
                    else
                    {
                        tokensToConsume[val]++;
                    }
                }
            }
            return true;
        }
    }
}
