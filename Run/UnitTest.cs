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
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Writing;
using Xbim.Common;
using Xbim.IO.Memory;

namespace Run
{
    internal static class UnitTest
    {
        internal static void Test_Day59()
        {
            string dir = @"D:\_UserDoc\SPNTest\";

            string input_spn_daily_ttl = Path.Combine(dir, "spn_59.ttl");
            string input_ifc_ttl = Path.Combine(dir, "Office-compressed.ifc.ttl");


            Repository repo = new Repository();


            RdfNSDef ns_mybim = new RdfNSDef(null, "http://foo.org/mybim#", "mybim", true);
            RdfNSDef ns_myspn = new RdfNSDef(null, "http://foo.org/myspn#", "myspn", true);

            Graph g_bim = ns_mybim.Graph as Graph;
            Graph g_spn = ns_myspn.Graph as Graph;



            g_spn.AddPrefix(ns_mybim);
            g_spn.AddPrefix(IFCRdfDefs.IFCRDF_IFC4);


            using (StreamReader reader = new StreamReader(input_ifc_ttl))
            {
                TurtleParser ttlparser = new TurtleParser();
                ttlparser.Load(g_bim, reader);
            }
            using (StreamReader reader = new StreamReader(input_spn_daily_ttl))
            {
                TurtleParser ttlparser = new TurtleParser();
                ttlparser.Load(g_spn, reader);
            }


            repo.Store.Add(g_bim);
            repo.Store.Add(g_spn);

            repo.NamespaceMap.Import(g_bim.NamespaceMap);
            repo.NamespaceMap.Import(g_spn.NamespaceMap);

            SparqlQuerier querier = new SparqlQuerier(repo);

            SparqlInjectee sparqlInjectee = new SparqlInjectee();
            sparqlInjectee.Binds.Add("?TOKEN", g_spn.CreateUriNode(new Uri("http://foo.org/mybim#IfcBuildingStorey_67")));
            sparqlInjectee.Binds.Add("?SELF", g_spn.CreateUriNode(new Uri("http://foo.org/myspn#T_Level_End_Archi")));

            string SPARQL = "SELECT * WHERE { ?place a spn:Place. ?place myspn:disciplineTag ?dTag1. ?SELF myspn:disciplineTag ?dTag2. ?place myspn:stateTag ?sTag. FILTER (?dTag1 = ?dTag2 && ?sTag != 'END') ?TOKEN ifc4:containsElements/ifc4:relatedElements ?elem. ?place ldp:contains ?elem. }";
            //string SPARQL = "ASK { ?place a spn:Place. ?place myspn:disciplineTag ?dTag1. ?SELF myspn:disciplineTag ?dTag2. ?place myspn:stateTag ?sTag. FILTER (?dTag1 = ?dTag2 && ?sTag != 'END') ?TOKEN ifc4:containsElements/ifc4:relatedElements ?elem. ?place ldp:contains ?elem. }";

            var tuple =  querier.QueryForTuple(SPARQL, sparqlInjectee);
            var boolRes = querier.QueryForBoolean(SPARQL, sparqlInjectee);


            Console.WriteLine(tuple.Recs.Count);
        }

        internal static void Test_InvExpr()
        {
            //test the case when arc with single arg

            string dir = @"D:\_UserDoc\SPNTest\";
            string output_unittest_inv_ttl = Path.Combine(dir, "unittest_inv.ttl");


            Repository repo = new Repository();
            RdfNSDef ns_myspn = new RdfNSDef(null, "http://foo.org/myspn#", "myspn", true);

            SPNModel SPN = new SPNModel(ns_myspn, repo);

            var arg_x = SPN.CreateArgDef("?x", new string[] { "myspn:myType1" });
            var arg_y = SPN.CreateArgDef("?y", new string[] { "xsd:string" });

            var P00 = SPN.CreatePlace("P00", SPN.CreateSparqlRule_OfType("myspn:myType1"), null);
            var P01 = SPN.CreatePlace("P01", SPN.CreateSparqlRule_OfType("xsd:string"), null);
            var P1 = SPN.CreatePlace("P1", null, null);


            var arcExpr_P00T0 = SPN.CreateConditionRule(
                //IF
                SPN.CreateCompoundRule("AND", new AbstractRule[] {
                    SPN.CreateSparqlRule("ASK {?x :lower ?t}"),
                    SPN.CreateCompoundRule("OR", new AbstractRule[] {
                        SPN.CreateSparqlRule("ASK {?x :level ?v. FILTER(?v > 5)}"),
                        SPN.CreateSparqlRule("ASK {?x :level ?v. FILTER(?v < 3)}"),
                    })
                }),
                //THEN
                SPN.CreateSparqlRule("SELECT ?t { ?x :lower ?t }"),
                //ELSE
                SPN.CreateArgRule(arg_x));

            var arcExpr_P01T0 = SPN.CreateArgRule(arg_y);

            var arcExpr_T0P1 = SPN.CreateArgRule(arg_x);

            var guard_T0 = SPN.CreateSparqlRule("ASK {?x :value ?v. FILTER(?v = ?y)}");


            var T0 = SPN.CreateTransition("T0", guard_T0, new ArgDef[] { arg_x, arg_y });

            var arc_P00T0 = SPN.CreateArcP2T(P00, T0, arcExpr_P00T0, null, new ArgDef[] { arg_x });
            var arc_P01T0 = SPN.CreateArcP2T(P01, T0, arcExpr_P01T0, null, new ArgDef[] { arg_y });

            var arc_T0P1 = SPN.CreateArcT2P(T0, P1, arcExpr_T0P1, new ArgDef[] { arg_x });

            // invCal

            SPNModelRunner runner = new SPNModelRunner(SPN);

            InvArcExprCal invCal = runner.InvCal;
            var arcInv_P00T0 = invCal.CalInvArcExpr(arc_P00T0);
            var arcInv_P01T0 = invCal.CalInvArcExpr(arc_P01T0);

            var writer = new CompressingTurtleWriter(TurtleSyntax.Original);
            writer.HighSpeedModePermitted = false;

            SPN.Graph.SaveToFile(output_unittest_inv_ttl, writer);

        }


        internal static void Test_InvExpr2()
        {
            string dir = @"D:\_UserDoc\SPNTest\";
            string input_ifc_path = Path.Combine(dir, "Office-compressed.ifc");
            string output_spn_init_path = Path.Combine(dir, "unittest_inv2_init.ttl");
            string output_spn_final_path = Path.Combine(dir, "unittest_inv2_final.ttl");

            var writer = new CompressingTurtleWriter(TurtleSyntax.Original);
            writer.HighSpeedModePermitted = false;


            Repository repo = new Repository();


            RdfNSDef ns_mybim = new RdfNSDef(null, "http://foo.org/mybim#", "mybim", true);
            RdfNSDef ns_myspn = new RdfNSDef(null, "http://foo.org/myspn#", "myspn", true);

            // load the IFC model


            IModel ifc_model = MemoryModel.OpenReadStep21(input_ifc_path);

            IFCRdfModel ifcrdf_model = new IFCRdfModel(ns_mybim, repo);

            ifcrdf_model.LoadIfcSchemaTBox("IFC4");
            ifcrdf_model.Load(ifc_model, false, true);

            //SPN


            SPNModel SPN = new SPNModel(ns_myspn, repo);


            SPN.Graph.AddPrefix(ns_mybim);
            SPN.Graph.AddPrefix(IFCRdfDefs.IFCRDF_IFC4);

            repo.NamespaceMap.Import(SPN.Graph.NamespaceMap);


            var arg_x = SPN.CreateArgDef("?x", new string[] { "ifc4:IfcDoor" });
            var arg_y = SPN.CreateArgDef("?y", new string[] { XsdLiteral.DOUBLE.QName });

            var P0 = SPN.CreatePlace("P0",
                null,
                SPN.CreateSparqlRule("SELECT ?TOKEN {  {?TOKEN a/rdfs:subClassOf* ifc4:IfcOpeningElement} UNION {?TOKEN a/rdfs:subClassOf* ifc4:IfcDoor} }")
            );

            var P1 = SPN.CreatePlace("P1", null, null);

            var T0 = SPN.CreateTransition("T0",
                SPN.CreateSparqlRule("ASK { FILTER(?y > 1600) }"),
                new ArgDef[] { arg_x, arg_y }
            );

            var expr = SPN.CreateConditionRule(
                    SPN.CreateSparqlRule("ASK { ?x ifc4:overallHeight ?h. FILTER(?h > ?y) . ?x ifc4:fillsVoids/ifc4:relatingOpeningElement ?z . myspn:P0 ldp:contains ?z }"),
                    SPN.CreateArgRule(arg_x),
                    SPN.CreateSparqlRule("SELECT ?z { ?x ifc4:fillsVoids/ifc4:relatingOpeningElement ?z } ")
                );


            SPN.CreateArcP2T(P0, T0, expr, null, new ArgDef[] { arg_x, arg_y });
            SPN.CreateArcT2P(T0, P1, expr, new ArgDef[] { arg_x, arg_y });

            // runner

            SPN.Init();

            SPN.Graph.SaveToFile(output_spn_init_path, writer);


            SPNModelRunner runner = new SPNModelRunner(SPN);

            runner.AddUserArg(T0, "?y", 1700.ToNode(SPN.Graph));


            if (!runner.CanRun())
            {
                throw new InvalidOperationException();
            }

            runner.RunRandom();

            SPN.Graph.SaveToFile(output_spn_final_path, writer);


        }

    }
}
