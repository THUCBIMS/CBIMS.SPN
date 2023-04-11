using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public class IFCOwlModel : XbimIFCRdfModelBase
    {
        public IFCOwlModel(RdfNSDef ns, IRepository host) : base(ns, host)
        {
            
        }
        public override void LoadIfcSchemaTBox(string ifcVersion)
        {
            ifcVersion = ifcVersion.ToUpperInvariant();
            //_EXPSchema = EXPSchema.GetEXPSchema(ifcVersion);

            RdfNSDef NS_Schema = null;

            switch (ifcVersion)
            {
                case "IFC4":
                    NS_Schema = IFCRdfDefs.IFCOWL_IFC4;
                    break;
                default:
                    throw new NotImplementedException();
            }



            Graph g_tbox = new Graph();
            using (MemoryStream ms = new MemoryStream(PublicResource.ifcowl_ifc4))
            {
                using (StreamReader reader = new StreamReader(ms))
                {
                    TurtleParser ttlparser = new TurtleParser();
                    ttlparser.Load(g_tbox, reader);
                }
            }

            Host.Store.Add(g_tbox);



            this.Graph.AddPrefix(NS_Schema);
            this.Graph.AddPrefix(RdfCommonNS.EXPRESS);
        }


        protected override AbstractIFCRdfEntity _CreateNewEntity(IPersistEntity entity)
        {
            return new IFCOwlEntity(this, entity);
        }


        public override string GetEntityQName(object _ent)
        {
            if (_ent is IPersistEntity ent)
            {
                return $"{NS.PrefixNC}:{ent.GetType().Name}_{ent.EntityLabel}";
            }
            throw new InvalidCastException();
        }
        public override string GetEdgeName(string name, Type sourceType)
        {
            //TODO
            string first = name.Substring(0, 1).ToLowerInvariant();
            return NS_Schema.PrefixNC + ":" + first + name.Substring(1) + "_" + sourceType.GetType().Name;
        }


        public override string GetValString(object _nominalValue)
        {
            if (_nominalValue is IIfcValue nominalValue)
            {
                var data = nominalValue.Value;
                return data.ToString();
            }
            throw new InvalidCastException();
        }



        internal IFCOwlEntity _LoadIfcTypeObject(IIfcTypeObject typeObj, bool loadProp)
        {
            IFCOwlEntity type = new IFCOwlEntity(this, typeObj);

            if (typeObj.Types != null)
            {
                foreach (var rel in typeObj.Types)
                {
                    _LoadRelDefinesByType(rel);
                }
            }


            if (loadProp && typeObj.HasPropertySets != null)
            {
                foreach (var _pset in typeObj.HasPropertySets)
                {
                    //TODO : other IIfcPropertySetDefinition
                    if (_pset is IIfcPropertySet pset)
                    {
                        var qname_pset = GetEntityQName(pset);
                        var inst_pset = GetEntity(qname_pset);

                        if (inst_pset == null)
                            inst_pset = _LoadIfcPropertySet(pset);

                        if (inst_pset != null)
                        {
                            type.AddProp("ifcowl:hasPropertySets_IfcTypeObject", inst_pset);
                        }
                            
                    }
                }
            }
            return type;
        }



        internal IFCOwlEntity _LoadIfcPropertySet(IIfcPropertySet pset)
        {
            IFCOwlEntity inst_pset = new IFCOwlEntity(this, pset);

            foreach (var p in pset.HasProperties)
            {
                if (!(p is IIfcPropertySingleValue pSingle))
                    continue;

                var inst_pSingle = new IFCOwlEntity(this, pSingle);

                inst_pset.AddProp("ifcowl:hasProperties_IfcPropertySet", inst_pSingle);
            }
            return inst_pset;
        }



        private void _LoadRelDefinesByProperties(IIfcRelDefinesByProperties pRel)
        {
            IIfcPropertySet pSet = null;
            if (pRel.RelatingPropertyDefinition is IIfcPropertySet)
            {
                pSet = pRel.RelatingPropertyDefinition as IIfcPropertySet;
            }
            else
            {
                throw new NotImplementedException();
            }
            if (pSet == null)
                return;

            IFCOwlEntity inst_pSet = _LoadIfcPropertySet(pSet);

            IFCOwlEntity inst_pRel = new IFCOwlEntity(this, pRel);

            inst_pRel.AddProp("ifcowl:relatingPropertyDefinition_IfcRelDefinesByProperties", inst_pSet);

            foreach (var target in pRel.RelatedObjects)
            {
                var ent = GetEntity(GetEntityQName(target));
                if (ent != null)
                {
                    inst_pRel.AddProp("ifcowl:relatedObjects_IfcRelDefinesByProperties", ent);

                    ent.AddProp("ifcowl:isDefinedBy_IfcObject", inst_pRel);

                }
            }

        }

        private void _LoadRelDefinesByType(IIfcRelDefinesByType tRel)
        {

            IFCOwlEntity inst_type = GetEntity(GetEntityQName(tRel.RelatingType)) as IFCOwlEntity;
            if (inst_type != null)
            {
                IFCOwlEntity inst_tRel = new IFCOwlEntity(this, tRel);

                inst_tRel.AddProp("ifcowl:relatingType_IfcRelDefinesByType", inst_type);

                foreach (var target in tRel.RelatedObjects)
                {
                    var ent = GetEntity(GetEntityQName(target));
                    if (ent != null)
                    {
                        inst_tRel.AddProp("ifcowl:relatedObjects_IfcRelDefinesByType", ent);

                        ent.AddProp("ifcowl:isTypedBy_IfcObject", inst_tRel);
                    }
                }
            }
        }


    }


}
