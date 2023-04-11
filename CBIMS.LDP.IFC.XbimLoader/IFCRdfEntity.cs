using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public class IFCRdfEntity : AbstractIFCRdfEntity
    {
        

        

        public IFCRdfEntity(AbstractIFCRdfModel host, IPersistEntity ent) : base(host, ent, ent.GetType(), ent.EntityLabel)
        {
            if(ent is IIfcRoot root) _LoadIfcRootAttrs(root);
            if (ent is IIfcObject _object) _LoadIfcObjectAttrs(_object);
            if (ent is IIfcProduct product) _LoadIfcProductAttrs(product);
            if (ent is IIfcElement elem) _LoadIfcElementAttrs(elem);

            if (ent is IIfcDoor door) _LoadIfcDoorAttrs(door);

            if (ent is IIfcTypeObject type) _LoadIfcTypeObjectAttrs(type);

            if (ent is IIfcPropertySingleValue pSingle) _LoadIfcPropertySingleValueAttrs(pSingle);
        }


        private void _LoadIfcRootAttrs(IIfcRoot root)
        {
            AddProp(PrefixNC_Schema + ":name", root.Name.UnWrap());
        }

        private void _LoadIfcObjectAttrs(IIfcObject _object)
        {
            AddProp(PrefixNC_Schema + ":globalId", _object.GlobalId.UnWrap());
            AddProp(PrefixNC_Schema + ":objectType", _object.ObjectType.UnWrap());
        }
        private void _LoadIfcProductAttrs(IIfcProduct product)
        {
            //TODO: ObjectPlacement
            //TODO: Representation
        }

        private void _LoadIfcElementAttrs(IIfcElement elem)
        {
            AddProp(PrefixNC_Schema + ":tag", elem.Tag.UnWrap());
        }


        private void _LoadIfcDoorAttrs(IIfcDoor door)
        {
            AddProp(PrefixNC_Schema + ":overallHeight", door.OverallHeight.UnWrap());
            AddProp(PrefixNC_Schema + ":overallWidth", door.OverallWidth.UnWrap());
        }

        private void _LoadIfcTypeObjectAttrs(IIfcTypeObject type)
        {
            AddProp(PrefixNC_Schema + ":applicableOccurrence", type.ApplicableOccurrence.UnWrap());
        }

        private void _LoadIfcPropertySingleValueAttrs(IIfcPropertySingleValue pSingle)
        {
            IIfcValue valData = pSingle.NominalValue;

            AddProp(PrefixNC_Schema + ":name", pSingle.Name.UnWrap());

            AddProp(PrefixNC_Schema + ":nominalValue", valData.UnWrap());
            //AddProp(PrefixNC_Schema + ":nominalValueType", Host._GetValTypeDef(valData));
        }

    }
}
