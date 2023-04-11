using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public class IFCOwlEntity : AbstractIFCRdfEntity
    {
        

        

        public IFCOwlEntity(IFCOwlModel host, IPersistEntity ent) : base(host, ent, ent.GetType(), ent.EntityLabel)
        {
            if (ent is IIfcRoot root) _LoadIfcRootAttrs(root);
            if (ent is IIfcObject _object) _LoadIfcObjectAttrs(_object);
            if (ent is IIfcProduct product) _LoadIfcProductAttrs(product);
            if (ent is IIfcElement elem) _LoadIfcElementAttrs(elem);

            if (ent is IIfcTypeObject type) _LoadIfcTypeObjectAttrs(type);


            if (ent is IIfcPropertySet pSet) _LoadIfcPropertySetAttrs(pSet);
            if (ent is IIfcPropertySingleValue pSingle) _LoadIfcPropertySingleValueAttrs(pSingle);
        }


        private void _LoadIfcRootAttrs(IIfcRoot root)
        {
            AddProp("ifcowl:name_IfcRoot", root.Name.UnWrap());
            AddProp("ifcowl:description_IfcRoot", root.Description.UnWrap());
        }

        private void _LoadIfcObjectAttrs(IIfcObject _object)
        {
            AddProp("ifcowl:globalId_IfcRoot", _object.GlobalId.UnWrap());

            AddProp("ifcowl:objectType_IfcObject", _object.ObjectType.UnWrap());
        }
        private void _LoadIfcProductAttrs(IIfcProduct product)
        {
            //TODO
        }

        private void _LoadIfcElementAttrs(IIfcElement elem)
        {
            AddProp("ifcowl:tag_IfcElement", elem.Tag.UnWrap());
        }

        private void _LoadIfcTypeObjectAttrs(IIfcTypeObject type)
        {
            //TODO
        }


        private void _LoadIfcPropertySetAttrs(IIfcPropertySet pSet)
        {
            //TODO
        }

        private void _LoadIfcPropertySingleValueAttrs(IIfcPropertySingleValue pSingle)
        {
            AddProp("ifcowl:name_IfcProperty", pSingle.Name.UnWrap());
            AddProp("ifcowl:description_IfcProperty", pSingle.Description.UnWrap());

            IFCRdfValue val = Host.CreateVal(pSingle.NominalValue);
            AddProp("ifcowl:nominalValue_IfcPropertySingleValue", val);
        }

    }
}
