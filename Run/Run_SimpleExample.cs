using CBIMS.LDP.Def;
using CBIMS.LDP.IFC;
using CBIMS.LDP.IFC.XbimLoader;
using CBIMS.LDP.Repo;
using CBIMS.SPN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using Xbim.Common;
using Xbim.IO.Memory;

namespace Run
{
    internal static class Run_SimpleExample
    {
        internal static void Run()
        {
            string dir = @"D:\_UserDoc\SPNTest\";


            string input_ifc_path = Path.Combine(dir, "Office-compressed.ifc");

            string output_ifc_ttl_path = Path.Combine(dir, "Office-compressed.ifc.ttl");
            string output_spn_init_path = Path.Combine(dir, "spn_simple_example_init.ttl");
            string output_spn_final_path = Path.Combine(dir, "spn_simple_example_final.ttl");

            StringBuilder sb_log = new StringBuilder();

            var writer = new CompressingTurtleWriter(TurtleSyntax.Original);
            writer.HighSpeedModePermitted = false;


            Repository repo = new Repository();

            RdfNSDef ns_mybim = new RdfNSDef(null, "http://foo.org/mybim#", "mybim", true);
            RdfNSDef ns_myspn = new RdfNSDef(null, "http://foo.org/myspn#", "myspn", true);
            
            // create the SPN model

            SPNModel SPN = new SPNModel(ns_myspn, repo);


            ns_myspn.Graph.AddPrefix(ns_mybim);
            ns_myspn.Graph.AddPrefix(IFCRdfDefs.IFCRDF_IFC4);


            var arg_x = SPN.CreateArgDef("?x", new string[] { "ifc4:IfcDoor" });
            var arg_y = SPN.CreateArgDef("?y", new string[] { "ifc4:IfcDoor" });

            var P0 = SPN.CreatePlace("P0",
                SPN.CreateSparqlRule_OfType("ifc4:IfcDoor"),
                SPN.CreateSparqlRule("SELECT ?TOKEN { ?TOKEN a/rdfs:subClassOf* ifc4:IfcDoor }")
            );

            var P1 = SPN.CreatePlace("P1", null, null);
            var P2 = SPN.CreatePlace("P2", null, null);

            var T1 = SPN.CreateTransition("T1",
                SPN.CreateSparqlRule("ASK { ?x ifc4:overallHeight ?h. FILTER(?h > 1800) }"), 
                new ArgDef[] { arg_x }
            );

            var T2 = SPN.CreateTransition("T2",
                SPN.CreateSparqlRule("ASK { ?y ifc4:overallHeight ?h. FILTER(?h <= 1800) }"),
                new ArgDef[] { arg_y }
            );

            SPN.CreateArcP2T(P0 , T1, null, null, new ArgDef[] { arg_x });
            SPN.CreateArcP2T(P0 , T2, null, null, new ArgDef[] { arg_y });

            SPN.CreateArcT2P(T1, P1, null, new ArgDef[] { arg_x });
            SPN.CreateArcT2P(T2, P2, null, new ArgDef[] { arg_y });

            // load the IFC model


            IModel ifc_model = MemoryModel.OpenReadStep21(input_ifc_path);

            IFCRdfModel ifcrdf_model = new IFCRdfModel(ns_mybim, repo);

            ifcrdf_model.LoadIfcSchemaTBox("IFC4");
            ifcrdf_model.Load(ifc_model, false, false);

            // runner

            SPN.Init();

            ifcrdf_model.Graph.SaveToFile(output_ifc_ttl_path, writer);
            SPN.Graph.SaveToFile(output_spn_init_path, writer);


            SPNModelRunner runner = new SPNModelRunner(SPN);
            

            if (!runner.CanRun())
            {
                throw new InvalidOperationException();
            }

            runner.RunRandom();

            SPN.Graph.SaveToFile(output_spn_final_path, writer);

        }
    }
}
