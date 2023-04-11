using CBIMS.LDP.Repo;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;

namespace CBIMS.SPN
{
    public class SPNModelRunner
    {
        static Random RANDOM = new Random();

        internal SPNModel Model;
        internal InvArcExprCal InvCal;

        ConcurrentDictionary<string, Transition> RegisteredTransitions;

        ConcurrentDictionary<ArcP2T, List<Binding>> ArcOptions = new ConcurrentDictionary<ArcP2T, List<Binding>>();
        ConcurrentDictionary<Transition, List<Binding>> TransitionOptions = new ConcurrentDictionary<Transition, List<Binding>>();

        Dictionary<Transition, Binding> UserAddedArgs = new Dictionary<Transition, Binding>();

        public int TotalOptions => TransitionOptions.Sum(c => c.Value.Count);

        public SPNModelRunner(SPNModel model, IEnumerable<Transition> partial_transtions = null)
        {
            Model = model;
            InvCal = new InvArcExprCal(this);

            if (partial_transtions != null)
            {
                RegisteredTransitions = new ConcurrentDictionary<string, Transition>();
                foreach(var trans in partial_transtions)
                {
                    RegisteredTransitions.TryAdd(trans.Name, trans);
                }
            }
            else
            {
                RegisteredTransitions = new ConcurrentDictionary<string, Transition>();
                foreach (var trans in model.Transitions.Values)
                {
                    RegisteredTransitions.TryAdd(trans.Name, trans);
                }
            }

        }

        public void AddUserArg(Transition trans, string arg, INode val)
        {
            if (!UserAddedArgs.ContainsKey(trans))
            {
                UserAddedArgs.Add(trans, new Binding());
            }
            UserAddedArgs[trans][arg] = val;
        }

        internal INode GetUserArg(Transition trans, string arg)
        {
            if (UserAddedArgs.ContainsKey(trans))
            {
                if (UserAddedArgs[trans].ContainsKey(arg))
                {
                    return UserAddedArgs[trans][arg];
                }
            }
            return null;
        }

        public bool CanRun()
        {
            foreach (var trans in RegisteredTransitions.Values)
            {
                if (!_CanGetOptions_Transition(trans))
                    return false;
            }
            return true;
        }

        public void RunRandom()
        {
            RefreshOptions();
            while (true)
            {
                if (!RunRandomOneTurn(out string warn))
                {
                    Console.WriteLine(warn);
                    break;
                }
            }
        }

        public void RunThrough()
        {
            RefreshOptions();
            while (true)
            {
                if (!RunThroughOneTurn(out int trigger_count, out int check_count, null))
                {
                    break;
                }
                
            }
            
        }

        public bool RunThroughOneTurn(out int trigger_count, out int check_count, StringBuilder sb_log)
        {
            HashSet<Place> modifiedPlaces = new HashSet<Place>();
            trigger_count = 0;
            check_count = 0;
            if (TotalOptions == 0)
            {
                _logLine("no options", sb_log);
                return false;
            }

            var options = GetOptions_InRandomOrder();


            foreach (var pair in options)
            {
                check_count++;
                if (Model.Trigger(pair.Item1, pair.Item2, out var _modifiedPlaces, out string warn))
                {
                    trigger_count++;
                    _logLine($"Triggering: Transition {pair.Item1.Name} Binding {JsonConvert.SerializeObject(pair.Item2)}", sb_log);
                    modifiedPlaces.UnionWith(_modifiedPlaces);
                }
                else
                {
                    //pass
                }
            }

            if (trigger_count == 0)
            {
                _logLine($"Checked {check_count}, end with 0 triggered", sb_log);
                return false;
            }
            else
            {
                UpdateOptions(modifiedPlaces);

                _logLine($"Checked {check_count}, triggered {trigger_count}", sb_log);
                return true;
            }

            
        }

        private static void _logLine(string v, StringBuilder sb_log)
        {
            Console.WriteLine(v);
            sb_log?.AppendLine(v);
        }

        public bool CanRun(Transition trans)
        {
            if (!RegisteredTransitions.ContainsKey(trans.Name))
                return false;

            if (!_CanGetOptions_Transition(trans))
                return false;
            return true;
        }
        public bool Run(Transition trans)
        {
            _GetOptions_Transition(trans);

            var bindings = GetOptions_InRandomOrder(trans);
            foreach (var binding in bindings)
            {
                if (Model.Trigger(trans, binding, out var modifiedPlaces, out string warn))
                {
                    Console.WriteLine($"Triggering: Transition {trans.Name} Binding {JsonConvert.SerializeObject(binding)}");
                    UpdateOptions(modifiedPlaces);
                    return true;
                }
            }
            return false;
        }

        public void RefreshOptions()
        {
            ArcOptions.Clear();
            TransitionOptions.Clear();

            foreach (var trans in RegisteredTransitions.Values)
            {
                _GetOptions_Transition(trans);
            }
        }

        public void UpdateOptions(IEnumerable<Place> modifiedPlaces)
        {

            HashSet<ArcP2T> modifiedArcs = new HashSet<ArcP2T>();

            HashSet<Transition> modifiedTransitions = new HashSet<Transition>();

            foreach (var place in modifiedPlaces)
            {
                foreach(var out_arc in place._Output_Arcs)
                {
                    modifiedArcs.Add(out_arc);
                    modifiedTransitions.Add(out_arc.relTransition);
                }
            }

            foreach(var trans in modifiedTransitions)
            {
                _GetOptions_Transition(trans, modifiedArcs);
            }

        }

        public IEnumerable<(Transition, Binding)> GetOptions_InRandomOrder()
        {
            return GetOptions().OrderBy(x => RANDOM.Next());
        }

        public IEnumerable<(Transition, Binding)> GetOptions()
        {
            return TransitionOptions.SelectMany(p => p.Value.Select(v => (p.Key, v)));
        }

        public IEnumerable<Binding> GetOptions_InRandomOrder(Transition trans)
        {
            var output = new List<Binding>(TransitionOptions[trans]);
            output.OrderBy(x => RANDOM.Next());
            return output;
        }


        public bool RunRandomOneTurn(out string warn)
        {
            warn = null;
            if (TotalOptions == 0)
            {
                warn = "end with no options";
                return false;
            }

            foreach(var pair in GetOptions_InRandomOrder())
            {
                if (Model.Trigger(pair.Item1, pair.Item2, out var modifiedPlaces, out warn))
                {
                    Console.WriteLine($"Triggering: Transition {pair.Item1.Name} Binding {_toJson(pair.Item2)}");
                    UpdateOptions(modifiedPlaces);
                    return true;
                }
            }
            warn = "pause with no avalible bindings";
            return false;
        }

        private string _toJson(Binding binding)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach(var k in binding.Keys)
            {
                result[k] = binding[k].ToString();
            }
            return JsonConvert.SerializeObject(result);
        }



        ////////

        private static IEnumerable<string> _getArgNames(IEnumerable<ArgDef> args)
        {
            foreach (var arg in args)
            {
                yield return arg.argName;
            }
        }

        ////////


        internal bool _CanGetOptions_Transition(Transition trans)
        {
            //check whether transition args are fulfilled by all input arcs
            var total_args = trans.hasArg;

            HashSet<string> total_arg_names = new HashSet<string>(_getArgNames(total_args));

            HashSet<string> input_args = new HashSet<string>();
            foreach (var arc in trans._Input_Arcs)
            {
                if (arc.hasArg != null)
                {
                    input_args.UnionWith(_getArgNames(arc.hasArg));
                }
            }

            if (UserAddedArgs.ContainsKey(trans))
            {
                input_args.UnionWith(UserAddedArgs[trans].Keys);
            }

            foreach (var arg in total_arg_names)
            {
                if (!input_args.Contains(arg))
                {
                    return false;
                }
            }
            return true;
        }


        private List<Binding> _GetOptions_ArcP2T(ArcP2T arc, out bool skip)
        {
            skip = false;
            if (arc.arcExprInv != null)
            {
                return _GetOptions_ArcP2T_withInv(arc, arc.arcExprInv);
            }
            else if (arc.arcExpr != null)
            {
                AbstractRule new_arg_expr = null;
                try
                {
                    new_arg_expr = InvCal.CalInvArcExpr(arc);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Unable to create inversion rule for arc " + arc.Name);
                    Console.WriteLine(ex.GetType().Name + ex.Message + ex.StackTrace);
                }
                

                if(new_arg_expr != null)
                {
                    arc.SetProp("spn:arcExprInv", new_arg_expr);
                    return _GetOptions_ArcP2T_withInv(arc, new_arg_expr);
                }
                else
                {
                    // fail to calculate the invArcExpr

                    //return _GetOptions_ArcP2T_withoutInv(arc);

                    skip = true;
                    return null;

                }
            }
            else
            {
                return _GetOptions_ArcP2T_withoutInv(arc);
            }

        }


        private List<Binding> _GetOptions_ArcP2T_withInv(ArcP2T arc, AbstractRule invRule)
        {
            List<Binding> output = new List<Binding>();
            SparqlInjectee injectee = new SparqlInjectee();

            if (UserAddedArgs.ContainsKey(arc.relTransition))
            {
                injectee.Binds = new Dictionary<string, INode>(UserAddedArgs[arc.relTransition]);
            }


            var result = Model.Processor.CheckForTuple(invRule, injectee);

            foreach (var rec in result.Recs)
            {
                var binding = new Binding();
                for (int i = 0; i < result.Args.Length; i++)
                {
                    binding.Add(result.Args[i], rec[i]);
                }
                output.Add(binding);
            }
            ArcOptions[arc] = output;
            return output;
        }

        private List<Binding> _GetOptions_ArcP2T_withoutInv(ArcP2T arc)
        {
            List<Binding> output = new List<Binding>();

            if (arc.hasArg.Count() > 1)
            {
                throw new InvalidOperationException("only one arg supported when arcExpr is not set");
            }

            var arg = arc.hasArg.First();

            foreach (var token in arc.relPlace.Contents)
            {
                output.Add(new Binding().AddArg(arg.argName, token));
            }
            

            ArcOptions[arc] = output;
            return output;
        }

        internal List<Binding> _GetOptions_Transition(Transition trans, HashSet<ArcP2T> modifiedArcs = null)
        {
            List<ArcP2T> skipped = new List<ArcP2T>();

            List<Binding> current = new List<Binding>();
            foreach (var arc in trans._Input_Arcs)
            {
                List<Binding> next = null;
                if (modifiedArcs != null && !modifiedArcs.Contains(arc) && ArcOptions.ContainsKey(arc))
                {
                    next = ArcOptions[arc];

                    current = _MergeOptions(current, next);
                }
                else
                {
                    next = _GetOptions_ArcP2T(arc, out bool skip);

                    if (!skip)
                    {
                        current = _MergeOptions(current, next);
                    }
                    else
                    {
                        skipped.Add(arc);
                    }
                }
            }

            if (skipped.Any())
            {
                _checkConsumeBySkippedArcs(current, skipped);
            }





            if (UserAddedArgs.ContainsKey(trans))
            {
                var userArg = UserAddedArgs[trans];
                foreach(var binding in current)
                {
                    foreach(var arg in userArg.Keys)
                    {
                        binding[arg] = userArg[arg];
                    }
                }
            }


            TransitionOptions[trans] = current;
            return current;
        }

        private void _checkConsumeBySkippedArcs(List<Binding> current, List<ArcP2T> skipped)
        {
            List<Binding> output = new List<Binding>();
            foreach (var arc in skipped)
            {
                var place = arc.relPlace;
                var arcExpr = arc.arcExpr;
                if (arcExpr == null)
                    continue;

                var args = arc.hasArg;

                var argNames = _getArgNames(args);


                foreach (var binding in current)
                {
                    //check consume

                    Dictionary<INode, int> consume = new Dictionary<INode, int>();


                    SparqlInjectee injectee = new SparqlInjectee();
                    SPNProcessor._AddBindingArgsToInjectee(injectee, binding, argNames);

                    var result = Model.Processor.CheckForTuple(arcExpr, injectee);
                    SPNProcessor._AddTokenSet(ref consume, result);

                    bool pass = true;

                    foreach(var node in consume.Keys)
                    {
                        int has = place.HasToken(node);
                        if(has< consume[node])
                        {
                            pass = false;
                            break;
                        }  
                    }

                    if (pass)
                        output.Add(binding);

                }

                current = output;
                output = new List<Binding>();

            }
        }

        private static List<Binding> _MergeOptions(List<Binding> current, List<Binding> next)
        {
            if (current.Count == 0)
                return next;

            if (next.Count == 0)
                return current;

            var args_c = current.First().Keys;
            var args_n = next.First().Keys;


            var common_args = args_c.Intersect(args_n);

            if (common_args.Any())
            {

                Dictionary<string, List<Binding>> common_recs_c = _GroupBindingsByArgs(current, common_args);
                Dictionary<string, List<Binding>> common_recs_n = _GroupBindingsByArgs(next, common_args);

                var common_keys = common_recs_c.Keys.Intersect(common_recs_n.Keys);

                if (!common_keys.Any())
                    return new List<Binding>();

                var rest_args_n = args_n.Except(common_args);

                List<Binding> output = new List<Binding>();
                foreach (var key in common_keys)
                {
                    foreach (var rec_c in common_recs_c[key])
                    {
                        foreach (var rec_n in common_recs_n[key])
                        {
                            Binding binding = new Binding(rec_c);
                            foreach (var arg in rest_args_n)
                            {
                                binding.Add(arg, rec_n[arg]);
                            }
                            output.Add(binding);
                        }
                    }
                }
                return output;

            }
            else
            {
                List<Binding> output = new List<Binding>();
                foreach (var rec_c in current)
                {
                    foreach (var rec_n in next)
                    {
                        Binding binding = new Binding(rec_c);
                        foreach (var arg in rec_n.Keys)
                        {
                            binding.Add(arg, rec_n[arg]);
                        }
                        output.Add(binding);
                    }
                }
                return output;
            }

        }

        private static Dictionary<string, List<Binding>> _GroupBindingsByArgs(List<Binding> bindings, IEnumerable<string> args)
        {
            Dictionary<string, List<Binding>> output = new Dictionary<string, List<Binding>>();
            foreach (var binding in bindings)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var arg in args)
                {
                    sb.Append(binding.GetArg(arg));
                    sb.Append('|');
                }
                string key = sb.ToString();
                if (!output.ContainsKey(key))
                {
                    output[key] = new List<Binding>();
                }
                output[key].Add(binding);
            }
            return output;
        }


    }
}
