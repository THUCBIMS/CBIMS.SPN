// Copyright (C) 2023  Liu, Han; School of Software, Tsinghua University
//
// This file is part of CBIMS.SPN.
// CBIMS.SPN is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// CBIMS.SPN is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with CBIMS.SPN. If not, see <https://www.gnu.org/licenses/>.

using CBIMS.LDP.Def;
using CBIMS.LDP.IFC;
using CBIMS.LDP.IFC.XbimLoader;
using CBIMS.LDP.Repo;
using CBIMS.SPN;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;

namespace Run
{
    internal class TimeTableSPNExpr
    {

        internal SPNModel SPN;

        IFCRdfModel IFC;
        SPNModelRunner Runner;

        HashSet<Transition> TransAutoRun = new HashSet<Transition>();

        internal int DAYS;

        //List<LDPContainer> DayRecs;
        Dictionary<INode, int> LastOpRecs = new Dictionary<INode, int>();

        Place P_DayOpCount;
        LDPContainer C_Today;
        int TODAY = 0;

        RdfNSDef NS => SPN.NS;
        string PrefixNC => NS.PrefixNC;
        string Schema_PrefixNC => IFC.NS_Schema.PrefixNC;

        Place P_Level_Start;

        Transition T_Level_Start_Struct;
        Transition T_Level_End_Struct;

        Transition T_Level_Start_Archi;
        Transition T_Level_End_Archi;

        Transition T_Level_Start_MEP;
        Transition T_Level_End_MEP;


        int ONGOING_DAYS = 7;
        int OFFSET = 0;

        int MAX_OP_INSTALL_A = 200;
        int MAX_OP_INSTALL_B = 100;
        int MAX_OP_INSTALL_C = 50;

        int MAX_ONGOING_COUNT_A = 500;
        int MAX_ONGOING_COUNT_B = 250;
        int MAX_ONGOING_COUNT_C = 125;

        Dictionary<string, List<(int, string)>> Schedule; // NodeQName, day, transName

        //short-cuts

        string Rel_Contain_ElemToSpace => $"{Schema_PrefixNC}:containedInSpatialStructure_RelatedInv/{Schema_PrefixNC}:relatingStructure";
        string Rel_Contain_SpaceToElem => $"{Schema_PrefixNC}:containsElements/{Schema_PrefixNC}:relatedElements";
        
        string Rel_Voids_HostToVoid => $"{Schema_PrefixNC}:hasOpenings/{Schema_PrefixNC}:relatedOpeningElement";
        string Rel_Voids_VoidToHost => $"{Schema_PrefixNC}:voidsElements/{Schema_PrefixNC}:relatingBuildingElement";

        string Rel_Fills_ElemToVoid => $"{Schema_PrefixNC}:fillsVoids/{Schema_PrefixNC}:relatingOpeningElement";
        string Rel_Fills_VoidToElem => $"{Schema_PrefixNC}:hasFillings/{Schema_PrefixNC}:relatedBuildingElement";


        public TimeTableSPNExpr(SPNModel SPN, IFCRdfModel IFC)
        {
            this.SPN = SPN;
            this.IFC = IFC;
            
        }
        internal void Init()
        {

            Console.WriteLine("Init SPN Places ...");

            SPN.Init();
        }

        internal void Build(int days)
        {
            DAYS = days;
            _BuildDayClock();

            _BuildLevelStates();
            _BuildStructEntStates();
            _BuildArchiEntStates();
            _BuildMEPEntStates();


            this.Runner = new SPNModelRunner(SPN, TransAutoRun);
        }

        internal void SetSchedule(Dictionary<string, List<(int, string)>> periods)
        {
            this.Schedule = periods;
        }

        internal void Run(string out_rec_path, string output_spn_daily_ttl)
        {

            if (!Runner.CanRun())
            {
                throw new InvalidOperationException();
            }

            List<OutPutRec> outputRec = new List<OutPutRec>();

            Runner.RefreshOptions();

            StringBuilder sb_log = new StringBuilder();

            try
            {

                for (int i = 0; i <= DAYS; i++)
                {

                    var start = DateTime.Now;

                    _logLine("", sb_log);
                    _logLine("DAY " + i, sb_log);
                    _logLine("", sb_log);

                    foreach (var nodeQName in Schedule.Keys)
                    {
                        INode node = IFC.Graph.GetUriNode(nodeQName);
                        if (node == null)
                        {
                            throw new InvalidOperationException("node not found: " + nodeQName);
                        }

                        var transs_today = Schedule[nodeQName].Where(t => t.Item1 == TODAY);
                        foreach (var pair in transs_today)
                        {
                            string transName = pair.Item2;

                            var trans = SPN.GetTransitionByName(transName);
                            if (trans == null)
                            {
                                throw new InvalidOperationException("transition not found: " + transName);
                            }

                            var binding = new Binding().AddArg("?TOKEN", node);

                            _logLine($"Day event: Transition {trans.Name} Binding {JsonConvert.SerializeObject(binding)}", sb_log);
                            _logLine("", sb_log);

                            var trans_result = SPN.Trigger(trans, binding, out _, out string warn);
                            if (!trans_result)
                            {
                                throw new InvalidOperationException($"timetable event failed: {transName} {nodeQName} {warn}");
                            }
                        }
                    }

                    Runner.RunThroughOneTurn(out int trigger_count, out int check_count, sb_log);


                    var end = DateTime.Now;

                    outputRec.Add(new OutPutRec
                    {
                        Day = TODAY,
                        Checked = check_count,
                        Triggered = trigger_count,
                        TimeUsage = (end - start).TotalSeconds
                    });

                    if (output_spn_daily_ttl != null)
                    {
                        var writer = new CompressingTurtleWriter(TurtleSyntax.Original);
                        writer.HighSpeedModePermitted = false;
                        SPN.Graph.SaveToFile(output_spn_daily_ttl + $"{i}.ttl", writer);
                    }

                    _NextDay();
                }

            }
            catch(Exception ex)
            {
                _logLine(ex.GetType().Name, sb_log);
                _logLine(ex.Message, sb_log);
                _logLine(ex.StackTrace, sb_log);
            }

            if (!string.IsNullOrEmpty(out_rec_path))
            {
                using (StreamWriter writer = new StreamWriter(out_rec_path))
                {
                    writer.WriteLine(sb_log.ToString());

                    foreach (var rec in outputRec)
                    {
                        writer.WriteLine(JsonConvert.SerializeObject(rec));
                    }
                }
            }


        }

        private static void _logLine(string v, StringBuilder sb_log)
        {
            Console.WriteLine(v);
            sb_log?.AppendLine(v);
        }

        private void _NextDay()
        {
            P_DayOpCount.ClearToken();

            C_Today.RemoveContent(TODAY);

            TODAY++;

            C_Today.AddContent(TODAY);
        }



        //Build

        private void _BuildDayClock()
        {
            P_DayOpCount = SPN.CreatePlace("P_DayOpCount", null, null);
            C_Today = new LDPContainer(NS, "C_Today", null, null);
            C_Today.AddContent(0);
        }


        private void _BuildLevelStates()
        {
            P_Level_Start = SPN.CreatePlace("P_Level_Start", null, 
                SPN.CreateSparqlRule_OfType($"{Schema_PrefixNC}:IfcBuildingStorey"));

            Place P_Level_OnGoing_Struct = SPN.CreatePlace("P_Level_OnGoing_Struct", null, null);
            Place P_Level_Finish_Struct = SPN.CreatePlace("P_Level_Finish_Struct", null, null);

            Place P_Level_OnGoing_Archi = SPN.CreatePlace("P_Level_OnGoing_Archi", null, null);
            Place P_Level_Finish_Archi = SPN.CreatePlace("P_Level_Finish_Archi", null, null);
            
            Place P_Level_OnGoing_MEP = SPN.CreatePlace("P_Level_OnGoing_MEP", null, null);
            Place P_Level_Finish_MEP = SPN.CreatePlace("P_Level_Finish_MEP", null, null);


            var endGuard_inv = SPN.CreateSparqlRule($"ASK {{" +
                $"?place a spn:Place. " +
                $"?place {PrefixNC}:disciplineTag ?dTag1. " +
                $"?SELF {PrefixNC}:disciplineTag ?dTag2. " +
                $"?place {PrefixNC}:stateTag ?sTag. " +
                $"FILTER (?dTag1 = ?dTag2 && ?sTag != 'END') " +
                $"?TOKEN {Rel_Contain_SpaceToElem} ?elem. " +
                $"?place ldp:contains ?elem. " +
                $"}}");

            var endGuard = SPN.CreateCompoundRule("NOT", new AbstractRule[] { endGuard_inv });

            //Transition
            T_Level_Start_Struct = SPN.CreateTransition("T_Level_Start_Struct", null, new string[] { "?TOKEN" });
            T_Level_End_Struct = SPN.CreateTransition("T_Level_End_Struct", endGuard, new string[] { "?TOKEN" });

            T_Level_Start_Archi = SPN.CreateTransition("T_Level_Start_Archi", null, new string[] { "?TOKEN" });
            T_Level_End_Archi = SPN.CreateTransition("T_Level_End_Archi", endGuard, new string[] { "?TOKEN" });
            
            T_Level_Start_MEP = SPN.CreateTransition("T_Level_Start_MEP", null, new string[] { "?TOKEN" });
            T_Level_End_MEP = SPN.CreateTransition("T_Level_End_MEP", endGuard, new string[] { "?TOKEN" });

            T_Level_End_Struct.AddProp($"{PrefixNC}:disciplineTag", "STRUCT");
            T_Level_End_Archi.AddProp($"{PrefixNC}:disciplineTag", "ARCHI");
            T_Level_End_MEP.AddProp($"{PrefixNC}:disciplineTag", "MEP");

            //Arc
            SPN.LinkPTP(P_Level_Start, T_Level_Start_Struct, P_Level_OnGoing_Struct, "?TOKEN");
            SPN.LinkPTP(P_Level_OnGoing_Struct, T_Level_End_Struct, P_Level_Finish_Struct, "?TOKEN");
            
            SPN.LinkPTP(P_Level_Start, T_Level_Start_Archi, P_Level_OnGoing_Archi, "?TOKEN");
            SPN.LinkPTP(P_Level_OnGoing_Archi, T_Level_End_Archi, P_Level_Finish_Archi, "?TOKEN");

            SPN.LinkPTP(P_Level_Start, T_Level_Start_MEP, P_Level_OnGoing_MEP, "?TOKEN");
            SPN.LinkPTP(P_Level_OnGoing_MEP, T_Level_End_MEP, P_Level_Finish_MEP, "?TOKEN");

            //Arcs send back
            SPN.CreateArcT2P(T_Level_Start_Struct, P_Level_Start, null, "?TOKEN");
            SPN.CreateArcT2P(T_Level_Start_Archi, P_Level_Start, null, "?TOKEN");
            SPN.CreateArcT2P(T_Level_Start_MEP, P_Level_Start, null, "?TOKEN");

        }


        private void _BuildStructEntStates()
        {
            List<string> types = new List<string>() { "IfcBeam", "IfcColumn", "IfcSlab", "IfcFooting", 
                "IfcStair", "IfcStairFlight", "IfcRamp", "IfcRampFlight", "IfcRoof" };
            foreach(var type in types)
            {
                _buildOneEntPeriod(type, "STRUCT", MAX_OP_INSTALL_B, MAX_ONGOING_COUNT_B, "P_Level_OnGoing_Struct", ONGOING_DAYS);
            }

        }


        private void _BuildArchiEntStates()
        {
            List<string> types_period_1 = new List<string>() { "IfcWall", "IfcCovering" };
            foreach (var type in types_period_1)
            {
                _buildOneEntPeriod(type, "ARCHI", MAX_OP_INSTALL_B, MAX_ONGOING_COUNT_B, "P_Level_OnGoing_Archi", ONGOING_DAYS);
            }

            List<string> types_period_2 = new List<string>() { "IfcCurtainWall", "IfcChimney" };
            foreach (var type in types_period_2)
            {
                _buildOneEntPeriod(type, "ARCHI", MAX_OP_INSTALL_C, MAX_ONGOING_COUNT_C, "P_Level_OnGoing_Archi", ONGOING_DAYS);
            }

            List<string> types_transient = new List<string>() { "IfcBuildingElementProxy", "IfcMember",
                "IfcShadingDevice", "IfcPile",  "IfcPlate", "IfcRailing", "IfcFurnishingElement" };
            foreach (var type in types_transient)
            {
                _buildOneEntInstall(type, "ARCHI", MAX_OP_INSTALL_A, "P_Level_OnGoing_Archi", null, null, 0);
            }


            List<string> types_void = new List<string>() { "IfcOpeningElement" };
            foreach (var type in types_void)
            {
                _buildOneEntInstall(type, "ARCHI", MAX_OP_INSTALL_B, "P_Level_OnGoing_Archi", Rel_Voids_VoidToHost, "ON_GOING", OFFSET);
            }

            List<string> types_voidFills = new List<string>() { "IfcDoor", "IfcWindow" };
            foreach (var type in types_voidFills)
            {
                _buildOneEntInstall(type, "ARCHI", MAX_OP_INSTALL_B, "P_Level_OnGoing_Archi", Rel_Fills_ElemToVoid, "END", OFFSET);
            }

        }
        

        private void _BuildMEPEntStates()
        {
            List<string> types = new List<string>() { "IfcDistributionElement" };
            foreach (var type in types)
            {
                _buildOneEntInstall(type, "MEP", MAX_OP_INSTALL_A, "P_Level_OnGoing_MEP", null, null, 0);
            }
        }


        private void _buildOneEntPeriod(string type, string discipline, int maxOpOneDay, int maxOnGoing, string spaceStage, int minDays)
        {
            AbstractRule guard_start = null;
            AbstractRule guard_end = null;

            string T_Start_Name = $"T_{type}_Start";
            string P_OnGoing_Name = $"P_{type}_OnGoing";

            //maxOpOneDay

            if (maxOpOneDay <= 0)
            {
                //pass
            }
            else if (maxOpOneDay == 1)
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"{P_DayOpCount.QName} ldp:contains {PrefixNC}:{T_Start_Name} }}");
                guard_start = SPN.CreateCompoundRule("NOT", new AbstractRule[] { _rule });
            }
            else
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"{P_DayOpCount.QName} ?R {PrefixNC}:{T_Start_Name}. " +
                    $"?R spn:multi_num ?N. " +
                    $"FILTER (?N >= {maxOpOneDay}) }}");
                guard_start = SPN.CreateCompoundRule("NOT", new AbstractRule[] { _rule });
            }

            //maxOnGoing
            if (maxOnGoing <= 0)
            {
                //pass
            }
            else
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"SELECT ( COUNT(?t) AS ?count ) {{ {PrefixNC}:{P_OnGoing_Name} ldp:contains ?t }} " +
                    $"HAVING (COUNT(?t) < {maxOnGoing}) }}");
                guard_start = SPN.CreateCompoundRule("AND", new AbstractRule[] { guard_start, _rule });
            }

            //spaceStage

            if (!string.IsNullOrEmpty(spaceStage))
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"?TOKEN {Rel_Contain_ElemToSpace} ?space. " +
                    $"{PrefixNC}:{spaceStage} ldp:contains ?space. }}");
                guard_start = SPN.CreateCompoundRule("AND", new AbstractRule[] { guard_start, _rule });
            }




            //minDays

            if (minDays <= 0)
            {
                //pass
            }
            else
            {
                guard_end = SPN.CreateSparqlRule($"ASK {{ " +
                    $"?TOKEN {PrefixNC}:lastOpAt ?day. " +
                    $"{PrefixNC}:C_Today ldp:contains ?today. " +
                    $"FILTER (?today - ?day >= {minDays}) }}");
            }

            var P_Start = SPN.CreatePlace($"P_{type}_Start", null,
                SPN.CreateSparqlRule_OfType($"{Schema_PrefixNC}:{type}", true,
                    $"FILTER EXISTS {{ ?TOKEN {Rel_Contain_ElemToSpace} ?R }}"));
            var P_OnGoing = SPN.CreatePlace(P_OnGoing_Name, null, null);
            var P_End = SPN.CreatePlace($"P_{type}_End", null, null);
            var T_Start = SPN.CreateTransition(T_Start_Name, guard_start, new string[] { "?TOKEN" });
            var T_End = SPN.CreateTransition($"T_{type}_End", guard_end, new string[] { "?TOKEN" });
            SPN.LinkPTP(P_Start, T_Start, P_OnGoing, "?TOKEN");
            SPN.LinkPTP(P_OnGoing, T_End, P_End, "?TOKEN");

            TransAutoRun.Add(T_Start);
            TransAutoRun.Add(T_End);

            P_Start.AddProp($"{PrefixNC}:stateTag", "START");
            P_OnGoing.AddProp($"{PrefixNC}:stateTag", "ON_GOING");
            P_End.AddProp($"{PrefixNC}:stateTag", "END");

            P_Start.AddProp($"{PrefixNC}:disciplineTag", discipline);
            P_OnGoing.AddProp($"{PrefixNC}:disciplineTag", discipline);
            P_End.AddProp($"{PrefixNC}:disciplineTag", discipline);
            


            T_Start.Triggered += _onStartPeriod;

        }
        private void _buildOneEntInstall(string type, string discipline, int maxOpOneDay, string spaceStage, string hostRel, string hostStateTag, int hostTimeOffset)
        {
            AbstractRule guard = null;

            string T_Install_Name = $"T_{type}_Install";

            if (maxOpOneDay <= 0)
            {
                //pass
            }
            else if (maxOpOneDay == 1)
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"{P_DayOpCount.QName} ldp:contains {PrefixNC}:{T_Install_Name} }}");
                guard = SPN.CreateCompoundRule("NOT", new AbstractRule[] { _rule });
            }
            else
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ {P_DayOpCount.QName} ?R {PrefixNC}:{T_Install_Name}. " +
                    $"?R spn:multi_num ?N. FILTER (?N >= {maxOpOneDay}) }}");
                guard = SPN.CreateCompoundRule("NOT", new AbstractRule[] { _rule });
            }

            //spaceStage

            if (!string.IsNullOrEmpty(spaceStage))
            {
                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"?TOKEN {Rel_Contain_ElemToSpace} ?space. " +
                    $"{PrefixNC}:{spaceStage} ldp:contains ?space. }}");
                guard = SPN.CreateCompoundRule("AND", new AbstractRule[] { guard, _rule });
            }


            //hostRel

            if (!string.IsNullOrEmpty(hostRel) && !string.IsNullOrEmpty(hostStateTag))
            {
                string time_stats = null;
                if (hostTimeOffset > 0)
                {
                    time_stats = $"?TOKEN {PrefixNC}:lastOpAt ?day. " +
                    $"{PrefixNC}:C_Today ldp:contains ?today. " +
                    $"FILTER (?today - ?day >= {hostTimeOffset}) ";
                }

                var _rule = SPN.CreateSparqlRule($"ASK {{ " +
                    $"?TOKEN {hostRel} ?host. " +
                    $"?state ldp:contains ?host. " +
                    $"?state {PrefixNC}:stateTag ?stateTag. " +
                    $"FILTER (?stateTag = '{hostStateTag}') " +
                    $" {time_stats} }}");
                guard = SPN.CreateCompoundRule("AND", new AbstractRule[] { guard, _rule });
            }


            var P_Start = SPN.CreatePlace($"P_{type}_Start", null,
                SPN.CreateSparqlRule_OfType($"{Schema_PrefixNC}:{type}", true,
                    $"FILTER EXISTS {{ ?TOKEN {Rel_Contain_ElemToSpace} ?R }}"));
            var P_End = SPN.CreatePlace($"P_{type}_End", null, null);
            var T_Install = SPN.CreateTransition(T_Install_Name, guard, new string[] { "?TOKEN" });
            SPN.LinkPTP(P_Start, T_Install, P_End, "?TOKEN");


            P_Start.AddProp($"{PrefixNC}:stateTag", "START");
            P_End.AddProp($"{PrefixNC}:stateTag", "END");

            P_Start.AddProp($"{PrefixNC}:disciplineTag", discipline);
            P_End.AddProp($"{PrefixNC}:disciplineTag", discipline);


            TransAutoRun.Add(T_Install);

            T_Install.Triggered += _onInstall;
        }


        void _onStartPeriod(Transition sender, Binding binding)
        {
            P_DayOpCount.AddToken(sender);

            var token = binding["?TOKEN"];

            var pred = SPN.Graph.CreateUriNode($"{PrefixNC}:lastOpAt");

            if (LastOpRecs.ContainsKey(token))
            {
                SPN.Graph.Retract(token, pred, LastOpRecs[token].ToNode(SPN.Graph));
            }

            LastOpRecs[token] = TODAY;
            SPN.Graph.Assert(token, pred, LastOpRecs[token].ToNode(SPN.Graph));
        }
        void _onInstall(Transition sender, Binding binding)
        {
            P_DayOpCount.AddToken(sender);

            var token = binding["?TOKEN"];

            var pred = SPN.Graph.CreateUriNode($"{PrefixNC}:lastOpAt");

            if (LastOpRecs.ContainsKey(token))
            {
                SPN.Graph.Retract(token, pred, LastOpRecs[token].ToNode(SPN.Graph));
            }

            LastOpRecs[token] = TODAY;
            SPN.Graph.Assert(token, pred, LastOpRecs[token].ToNode(SPN.Graph));
        }

    }


    internal struct OutPutRec
    {
        public int Day;
        public int Checked;
        public int Triggered;
        public double TimeUsage;
    }
}
