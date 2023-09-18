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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.IO.Memory;

namespace Run
{
    internal static class Run_TimeTable
    {
        internal static void Run()
        {
            string dir = @"D:\_UserDoc\SPNTest\";


            string input_ifc_path = Path.Combine(dir, "Office-compressed.ifc");

            string output_ifc_ttl = Path.Combine(dir, "Office-compressed.ifc.ttl");
            string output_spn_init_ttl = Path.Combine(dir, "spn_init.ttl");
            string output_spn_daily_ttl = Path.Combine(dir, "spn_");
            string output_spn_final_ttl = Path.Combine(dir, "spn_final.ttl");

            string output_log = Path.Combine(dir, "output.log");

            bool USE_GEOM = true;


            DateTime start_time = DateTime.Now;



            Dictionary<string, string> levelNameMap = new Dictionary<string, string>
            {
                { "Footing", "mybim:IfcBuildingStorey_80" },
                { "Level 1", "mybim:IfcBuildingStorey_67" },
                { "Level 2", "mybim:IfcBuildingStorey_71" },
                { "Roof", "mybim:IfcBuildingStorey_75" },
            };
            HashSet<int> required_levelIds = new HashSet<int> { 67, 71, 75, 80 };


            Repository repo = new Repository();

            RdfNSDef ns_mybim = new RdfNSDef(null, "http://foo.org/mybim#", "mybim", true);
            RdfNSDef ns_myspn = new RdfNSDef(null, "http://foo.org/myspn#", "myspn", true);

            IfcStore store = null;
            IModel ifc_model = null;

            if (USE_GEOM) 
            {
                store = IfcStore.Open(input_ifc_path);
                ifc_model = store.Model;
            }
            else
            {
                ifc_model = MemoryModel.OpenReadStep21(input_ifc_path);
            }


            IFCRdfModel ifcrdf_model = new IFCRdfModel(ns_mybim, repo);

            ifcrdf_model.LoadIfcSchemaTBox("IFC4");
            ifcrdf_model.Load(ifc_model, false, true);

            if (USE_GEOM)
            {
                XbimGeomLoader geomLoader = new XbimGeomLoader(ifcrdf_model, store);
                geomLoader.Load(required_levelIds);
                geomLoader.FixLevelRels();
            }
            else
            {
                //pass
            }

            var end_load_time = DateTime.Now;


            var writer = new CompressingTurtleWriter(TurtleSyntax.Original);
            writer.HighSpeedModePermitted = false;

            ns_mybim.Graph.SaveToFile(output_ifc_ttl, writer);



            SPNModel model_spn = new SPNModel(ns_myspn, repo);


            ns_myspn.Graph.AddPrefix(ns_mybim);
            ns_myspn.Graph.AddPrefix(IFCRdfDefs.IFCRDF_IFC4);


            TimeTableSPNExpr timetable = new TimeTableSPNExpr(model_spn, ifcrdf_model);
            timetable.Build(90);

            timetable.Init();


            ns_myspn.Graph.SaveToFile(output_spn_init_ttl, writer);




            Dictionary<string, List<(int, string)>> schedule = new Dictionary<string, List<(int, string)>>();

            _addSchedule(ref schedule, levelNameMap, "Footing", "T_Level_Start_Struct", 0);
            _addSchedule(ref schedule, levelNameMap, "Footing", "T_Level_End_Struct", 15);

            _addSchedule(ref schedule, levelNameMap, "Footing", "T_Level_Start_Archi", 10);
            _addSchedule(ref schedule, levelNameMap, "Footing", "T_Level_End_Archi", 20);



            _addSchedule(ref schedule, levelNameMap, "Level 1", "T_Level_Start_Struct", 20);
            _addSchedule(ref schedule, levelNameMap, "Level 1", "T_Level_End_Struct", 40);

            _addSchedule(ref schedule, levelNameMap, "Level 1", "T_Level_Start_Archi", 35);
            _addSchedule(ref schedule, levelNameMap, "Level 1", "T_Level_End_Archi", 60);

            _addSchedule(ref schedule, levelNameMap, "Level 1", "T_Level_Start_MEP", 50);
            _addSchedule(ref schedule, levelNameMap, "Level 1", "T_Level_End_MEP", 70);



            _addSchedule(ref schedule, levelNameMap, "Level 2", "T_Level_Start_Struct", 40);
            _addSchedule(ref schedule, levelNameMap, "Level 2", "T_Level_End_Struct", 60);

            _addSchedule(ref schedule, levelNameMap, "Level 2", "T_Level_Start_Archi", 55);
            _addSchedule(ref schedule, levelNameMap, "Level 2", "T_Level_End_Archi", 80);

            _addSchedule(ref schedule, levelNameMap, "Level 2", "T_Level_Start_MEP", 70);
            _addSchedule(ref schedule, levelNameMap, "Level 2", "T_Level_End_MEP", 90);



            _addSchedule(ref schedule, levelNameMap, "Roof", "T_Level_Start_Struct", 60);
            _addSchedule(ref schedule, levelNameMap, "Roof", "T_Level_End_Struct", 80);

            _addSchedule(ref schedule, levelNameMap, "Roof", "T_Level_Start_Archi", 70);
            _addSchedule(ref schedule, levelNameMap, "Roof", "T_Level_End_Archi", 85);

            _addSchedule(ref schedule, levelNameMap, "Roof", "T_Level_Start_MEP", 75);
            _addSchedule(ref schedule, levelNameMap, "Roof", "T_Level_End_MEP", 90);


            timetable.SetSchedule(schedule);

            timetable.Run(output_log, output_spn_daily_ttl);


            ns_myspn.Graph.SaveToFile(output_spn_final_ttl, writer);

            DateTime end_time = DateTime.Now;

            var load_time = end_load_time - start_time;
            var total_time = end_time - start_time;

            using (StreamWriter sw = new StreamWriter(output_log, true))
            {
                sw.WriteLine("LOAD TIME " + load_time.ToString());
                sw.WriteLine("TOTAL TIME " + total_time.ToString());
            }


        }

        private static void _addSchedule(ref Dictionary<string, List<(int, string)>> events, Dictionary<string, string> nameMap, string name, string trans, int day)
        {
            var nodeName = nameMap[name];

            if (!events.ContainsKey(nodeName))
            {
                events[nodeName] = new List<(int, string)>();
            }
            events[nodeName].Add((day, trans));
        }
    }
}
