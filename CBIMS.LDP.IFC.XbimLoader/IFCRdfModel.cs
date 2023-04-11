using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using Xbim.Common;
using Xbim.Common.Collections;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public class IFCRdfModel : XbimIFCRdfModelBase
    {

        public IFCRdfModel(RdfNSDef ns, IRepository host) : base(ns, host)
        {
            host.NamespaceMap.Import(this.Graph.NamespaceMap);
        }

        public override void LoadIfcSchemaTBox(string ifcVersion)
        {
            ifcVersion = ifcVersion.ToUpperInvariant();
            //_EXPSchema = EXPSchema.GetEXPSchema(ifcVersion);
            switch (ifcVersion)
            {
                case "IFC4":
                    NS_Schema = IFCRdfDefs.IFCRDF_IFC4;
                    _KernelName = "Xbim.Ifc4.Kernel";
                    break;
                default:
                    throw new NotImplementedException();
            }

            Graph g_tbox = new Graph();
            using (MemoryStream ms = new MemoryStream(PublicResource.ifcrdf_ifc4))
            {
                using (StreamReader reader = new StreamReader(ms))
                {
                    TurtleParser ttlparser = new TurtleParser();
                    ttlparser.Load(g_tbox, reader);
                }
            }

            Host.Store.Add(g_tbox);

            this.Graph.AddPrefix(NS_Schema);
            Host.NamespaceMap.Import(this.Graph.NamespaceMap);

        }

        protected override AbstractIFCRdfEntity _CreateNewEntity(IPersistEntity entity)
        {
            return new IFCRdfEntity(this, entity);
        }

        public override string GetEdgeName(string name, Type sourceType)
        {
            //ignore source name
            string first = name.Substring(0, 1).ToLowerInvariant();
            return NS_Schema.PrefixNC + ":" + first + name.Substring(1);
        }

    }


}
