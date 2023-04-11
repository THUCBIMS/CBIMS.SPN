using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public abstract class XbimIFCRdfModelBase : AbstractIFCRdfModel
    {
        protected ExpressMetaData _Metadata;
        protected string _KernelName;

        protected XbimIFCRdfModelBase(RdfNSDef ns, IRepository host) : base(ns, host)
        {
        }
        protected abstract AbstractIFCRdfEntity _CreateNewEntity(IPersistEntity entity);


        public override void Load(object _model, bool loadProp = true, bool loadRel = true)
        {
            IModel model = (IModel)_model;

            _Metadata = model.Metadata;

            var projects = model.FindAll<IIfcContext>();
            Console.WriteLine("Load IfcProject ...");
            foreach (var proj in projects)
            {
                _LoadIfcProject(proj);
            }

            var products = model.FindAll<IIfcProduct>();
            Console.WriteLine("Load IfcProduct ...");
            foreach (var product in products)
            {
                _LoadIfcProduct(product);
            }

            var typeProducts = model.FindAll<IIfcTypeProduct>();
            Console.WriteLine("Load IfcTypeProduct ...");
            foreach (var typeProduct in typeProducts)
            {
                _LoadIfcTypeObject(typeProduct as IIfcTypeObject, loadProp);
            }

            if (loadProp)
            {
                var pSetRels = model.FindAll<IIfcRelDefinesByProperties>();
                Console.WriteLine("Load IfcRelDefinesByProperties ...");
                foreach (var pSetRel in pSetRels)
                {
                    _LoadIfcRelDefinesByProperties(pSetRel);
                }
            }

            if (loadRel)
            {
                var relDecomposes = model.FindAll<IIfcRelDecomposes>();
                Console.WriteLine("Load IfcRelDecomposes ...");
                foreach (var rel in relDecomposes)
                {
                    _LoadRel(rel as IIfcRelationship);
                }

                var relConnects = model.FindAll<IIfcRelConnects>();
                Console.WriteLine("Load IfcRelConnects ...");
                foreach (var rel in relConnects)
                {
                    _LoadRel(rel as IIfcRelationship);
                }

                var relAssigns = model.FindAll<IIfcRelAssigns>();
                Console.WriteLine("Load IfcRelAssigns ...");
                foreach (var rel in relAssigns)
                {
                    _LoadRel(rel as IIfcRelationship);
                }

                _FixOpeningElementRelWithSpace(model);

            }


        }



        private void _FixOpeningElementRelWithSpace(IModel model)
        {
            var rel_fills = model.FindAll<IIfcRelFillsElement>();

            Dictionary<IPersistEntity, IPersistEntity> pairs = new Dictionary<IPersistEntity, IPersistEntity>();
            foreach (var rel_fill in rel_fills)
            {
                var opening = rel_fill.RelatingOpeningElement;
                var fill = rel_fill.RelatedBuildingElement;
                pairs.Add(fill, opening);
            }

            var rel_contains = model.FindAll<IIfcRelContainedInSpatialStructure>();

            foreach (var rel_contain in rel_contains)
            {
                string relUpper = rel_contain.GetType().Name.ToUpperInvariant();
                var relDef = _refDefs[relUpper];

                _getRelPairs(rel_contain as IIfcRelationship, relDef,
                    out IEnumerable<IPersistEntity> relatedElements, out IEnumerable<IPersistEntity> relatingSpaces);

                List<IPersistEntity> openingsToAdd = new List<IPersistEntity>();
                foreach (var elem in relatedElements)
                {
                    if (pairs.ContainsKey(elem))
                    {
                        openingsToAdd.Add(pairs[elem]);
                    }
                }

                if (openingsToAdd.Count > 0)
                {
                    _addRelPair(rel_contain, relDef, openingsToAdd, relatingSpaces);
                }

            }
        }

        private AbstractIFCRdfEntity _LoadIfcProject(IIfcContext proj)
        {
            if (_alreadyLoaded(proj, out AbstractIFCRdfEntity item))
                return item;

            AbstractIFCRdfEntity inst = _CreateNewEntity(proj);

            return inst;
        }



        internal AbstractIFCRdfEntity _LoadIfcProduct(IIfcProduct product)
        {
            if (_alreadyLoaded(product, out AbstractIFCRdfEntity item))
                return item;

            AbstractIFCRdfEntity inst = _CreateNewEntity(product);

            return inst;
        }



        internal AbstractIFCRdfEntity _LoadIfcTypeObject(IIfcTypeObject typeObj, bool loadProp)
        {
            if (_alreadyLoaded(typeObj, out AbstractIFCRdfEntity item))
                return item;

            AbstractIFCRdfEntity type = _CreateNewEntity(typeObj);

            if (typeObj.Types != null)
            {
                foreach (var rel in typeObj.Types)
                {
                    _LoadRel(rel);
                }
            }


            if (loadProp && typeObj.HasPropertySets != null)
            {
                foreach (var _pset in typeObj.HasPropertySets)
                {
                    //TODO : other IIfcPropertySetDefinition

                    AbstractIFCRdfEntity pSetEnt = _LoadPSetDef(_pset);
                    if (pSetEnt != null)
                    {
                        type.AddProp(GetEdgeName("hasPropertySets", typeObj.GetType()), pSetEnt);
                        pSetEnt.AddProp(GetEdgeName("definesType", _pset.GetType()), type);
                    }
                }
            }
            return type;
        }



        internal AbstractIFCRdfEntity _LoadIfcRelDefinesByProperties(IIfcRelDefinesByProperties pRel)
        {
            if (_alreadyLoaded(pRel, out AbstractIFCRdfEntity item))
                return item;

            AbstractIFCRdfEntity inst_pRel = _CreateNewEntity(pRel);
            foreach (var _target in pRel.RelatedObjects)
            {
                var target = GetEntity(GetEntityQName(_target));
                if (target != null)
                {
                    inst_pRel.AddProp(GetEdgeName("relatedObjects", pRel.GetType()), target);
                    target.AddProp(GetEdgeName("isDefinedBy", _target.GetType()), inst_pRel);
                }
            }


            var pSetSelect = pRel.RelatingPropertyDefinition;
            if (pSetSelect is IIfcPropertySetDefinition psetDef)
            {
                AbstractIFCRdfEntity pSetEnt = _LoadPSetDef(psetDef);
                if (pSetEnt != null)
                {
                    inst_pRel.AddProp(GetEdgeName("relatingPropertyDefinition", pRel.GetType()), pSetEnt);
                    pSetEnt.AddProp(GetEdgeName("definesOccurrence", psetDef.GetType()), inst_pRel);
                }
            }
            else if (pSetSelect is IfcPropertySetDefinitionSet psets)
            {
                foreach (var subDef in psets.PropertySetDefinitions)
                {
                    AbstractIFCRdfEntity pSetEnt = _LoadPSetDef(subDef);
                    if (pSetEnt != null)
                    {
                        inst_pRel.AddProp(GetEdgeName("relatingPropertyDefinition", pRel.GetType()), pSetEnt);
                        pSetEnt.AddProp(GetEdgeName("definesOccurrence", subDef.GetType()), inst_pRel);
                    }
                }
            }


            return inst_pRel;
        }

        private AbstractIFCRdfEntity _LoadPSetDef(IIfcPropertySetDefinition psetDef)
        {
            if (psetDef is IIfcPropertySet pset)
            {
                return _LoadPSet(pset);
            }
            else
            {
                //TODO
            }
            return null;
        }

        private AbstractIFCRdfEntity _LoadPSet(IIfcPropertySet pset)
        {
            if (_alreadyLoaded(pset, out AbstractIFCRdfEntity item))
                return item;

            AbstractIFCRdfEntity inst_pset = _CreateNewEntity(pset);

            foreach (var p in pset.HasProperties)
            {
                if (p is IIfcPropertySingleValue pSingle)
                {
                    AbstractIFCRdfEntity inst_pSingle = _LoadPSingle(pSingle);
                    inst_pset.AddProp(GetEdgeName("hasProperties", pset.GetType()), inst_pSingle);
                }
                else
                {
                    //TODO
                }

            }

            return inst_pset;
        }

        private AbstractIFCRdfEntity _LoadPSingle(IIfcPropertySingleValue pSingle)
        {
            if (_alreadyLoaded(pSingle, out AbstractIFCRdfEntity item))
                return item;

            return _CreateNewEntity(pSingle);
        }

        private AbstractIFCRdfEntity _LoadRel(IIfcRelationship rel)
        {
            var relUpper = rel.GetType().Name.ToUpperInvariant();
            if (!_refDefs.ContainsKey(relUpper))
            {
                _initRelDef(rel.GetType());
            }

            var relDef = _refDefs[relUpper];

            _getRelPairs(rel, relDef, out IEnumerable<IPersistEntity> relateds, out IEnumerable<IPersistEntity> relatings);

            return _addRelPair(rel, relDef, relateds, relatings);

        }



        public override string GetEntityQName(object _ent)
        {
            if (_ent is IPersistEntity ent)
            {
                return $"{NS.PrefixNC}:{ent.GetType().Name}_{ent.EntityLabel}";
            }
            throw new InvalidCastException();
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

        protected bool _alreadyLoaded(IPersistEntity ent, out AbstractIFCRdfEntity item)
        {
            item = GetEntity(GetEntityQName(ent));
            return item != null;
        }

        protected RelDefInfo _initRelDef(Type relType)
        {
            string relTypeName = relType.Name;
            string relTypeUpper = relTypeName.ToUpperInvariant();
            var def = _Metadata.ExpressType(relTypeUpper);

            var def_relating = def.Properties.FirstOrDefault(t => t.Value.Name.StartsWith("Relating")).Value;
            var def_related = def.Properties.FirstOrDefault(t => t.Value.Name.StartsWith("Related")).Value;

            if (def_relating != null && def_related != null)
            {
                RelDefInfo output = new RelDefInfo();

                output.RelName = def.Name;

                //relating

                output.RelatingArgName = def_relating.Name;

                var pType = def_relating.PropertyInfo.PropertyType;
                if (pType.IsGenericType
                    && pType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    pType = pType.GetGenericArguments().First();
                }


                if (pType.IsGenericType)
                {
                    output.RelatingTargetType = pType.GetGenericArguments().First();
                    output.RelatingTargetName = output.RelatingTargetType.Name;
                    output.RelatingColl = true;
                }
                else
                {
                    output.RelatingTargetType = pType;
                    output.RelatingTargetName = pType.Name;
                    output.RelatingColl = false;
                }

                if (output.RelatingTargetType.IsInterface)
                {
                    //TODO: SELECT
                }
                else
                {
                    var relatingUpper = output.RelatingTargetName.ToUpperInvariant();
                    var relatingEnt = _Metadata.ExpressType(relatingUpper);
                    var relatingInv = relatingEnt.Inverses.FirstOrDefault(
                        t => _unpack(t.PropertyInfo.PropertyType).IsAssignableFrom(relType)
                        && t.InverseAttributeProperty.RemoteProperty == output.RelatingArgName);

                    if (relatingInv != null)
                        output.RelatingInvArgName = relatingInv.Name;
                }




                //related

                output.RelatedArgName = def_related.Name;

                pType = def_related.PropertyInfo.PropertyType;

                if (pType.IsGenericType
                    && pType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    pType = pType.GetGenericArguments().First();
                }


                if (pType.IsGenericType)
                {
                    output.RelatedTargetType = pType.GetGenericArguments().First();
                    output.RelatedTargetName = output.RelatedTargetType.Name;
                    output.RelatedColl = true;
                }
                else
                {
                    output.RelatedTargetType = pType;
                    output.RelatedTargetName = pType.Name;
                    output.RelatedColl = false;
                }

                if (output.RelatedTargetType.IsInterface)
                {
                    //TODO: SELECT
                }
                else
                {
                    var relatedUpper = output.RelatedTargetName.ToUpperInvariant();
                    var relatedEnt = _Metadata.ExpressType(relatedUpper);

                    var relatedInv = relatedEnt.Inverses.FirstOrDefault(
                        t => _unpack(t.PropertyInfo.PropertyType).IsAssignableFrom(relType)
                        && t.InverseAttributeProperty.RemoteProperty == output.RelatedArgName);

                    if (relatedInv != null)
                        output.RelatedInvArgName = relatedInv.Name;
                }

                _refDefs[relTypeUpper] = output;
                return output;
            }
            else
            {
                throw new NotImplementedException(relTypeName);
            }
        }

        internal static Type _unpack(Type type)
        {
            while (type.IsGenericType)
            {
                type = type.GetGenericArguments().First();
            }
            return type;
        }

        private void _getRelPairs(IIfcRelationship rel, RelDefInfo relDef, out IEnumerable<IPersistEntity> relateds, out IEnumerable<IPersistEntity> relatings)
        {


            var relCLRType = rel.GetType();

            relateds = null;
            relatings = null;


            if (relDef.RelatedColl)
            {
                relateds = relCLRType.GetProperty(relDef.RelatedArgName).GetValue(rel) as IEnumerable<IPersistEntity>;
            }
            else
            {
                relateds = new IPersistEntity[] { relCLRType.GetProperty(relDef.RelatedArgName).GetValue(rel) as IPersistEntity };
            }

            if (relDef.RelatingColl)
            {
                relatings = relCLRType.GetProperty(relDef.RelatingArgName).GetValue(rel) as IEnumerable<IPersistEntity>;
            }
            else
            {
                relatings = new IPersistEntity[] { relCLRType.GetProperty(relDef.RelatingArgName).GetValue(rel) as IPersistEntity };
            }

        }

        private AbstractIFCRdfEntity _addRelPair(IIfcRelationship rel, RelDefInfo relDef, IEnumerable<IPersistEntity> relateds, IEnumerable<IPersistEntity> relatings)
        {
            var ent_rel = GetEntity(GetEntityQName(rel));
            if (ent_rel == null)
            {
                ent_rel = _CreateNewEntity(rel);
            }

            foreach (var related in relateds)
            {
                var ent_related = GetEntity(GetEntityQName(related));
                if (ent_related != null)
                {
                    ent_rel.AddProp(GetEdgeName(relDef.RelatedArgName, rel.GetType()), ent_related);

                    if (relDef.RelatedInvArgName != null)
                        ent_related.AddProp(GetEdgeName(relDef.RelatedInvArgName, related.GetType()), ent_rel);
                    else
                        ent_related.AddProp(GetEdgeName(relDef.RelName.Substring(6) + "_RelatedInv", related.GetType()), ent_rel);
                }
            }

            foreach (var relating in relatings)
            {
                var ent_relating = GetEntity(GetEntityQName(relating));
                if (ent_relating != null)
                {
                    ent_rel.AddProp(GetEdgeName(relDef.RelatingArgName, rel.GetType()), ent_relating);

                    if (relDef.RelatingInvArgName != null)
                        ent_relating.AddProp(GetEdgeName(relDef.RelatingInvArgName, relating.GetType()), ent_rel);
                    else
                        ent_relating.AddProp(GetEdgeName(relDef.RelName.Substring(6) + "_RelatingInv", relating.GetType()), ent_rel);
                }
            }

            return ent_rel;
        }


    }
}
