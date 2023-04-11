using CBIMS.LDP.Def;
using CBIMS.LDP.Repo;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.IFC
{
    public abstract class AbstractIFCRdfModel : RdfModel
    {
        public RdfNSDef NS_Schema;

        protected Dictionary<string, RdfURIClassDef> _usedIfcTypes 
            = new Dictionary<string, RdfURIClassDef>();

        protected Dictionary<string, AbstractIFCRdfEntity> _entities 
            = new Dictionary<string, AbstractIFCRdfEntity>(); //qname, entity

        protected Dictionary<string, Dictionary<string, IFCRdfValue>> _values
            = new Dictionary<string, Dictionary<string, IFCRdfValue>>();//type, string, val

        protected Dictionary<string, RelDefInfo> _refDefs = new Dictionary<string, RelDefInfo>();

        protected string PrefixNC_Schema => NS_Schema.PrefixNC;


        public abstract string GetEntityQName(object ent);
        public abstract string GetValString(object nominalValue);
        public abstract string GetEdgeName(string name, Type sourceType);

        

        public abstract void LoadIfcSchemaTBox(string ifcVersion);
        public abstract void Load(object model, bool loadProp, bool loadRel);


        protected AbstractIFCRdfModel(RdfNSDef ns, IRepository host) : base(ns, host)
        {

        }




        public RdfURIClassDef GetClassDef(Type entType)
        {
            if (!_usedIfcTypes.ContainsKey(entType.Name))
            {
                _usedIfcTypes[entType.Name] = new RdfURIClassDef(NS_Schema, entType.Name, null, false, entType, null);
            }
            return _usedIfcTypes[entType.Name];
        }

        protected RdfURIClassDef _GetValTypeDef(Type valType)
        {
            string typeUpper = valType.Name.ToUpperInvariant();
            //string typeName = EXPSchema.GetEXPType(typeUpper).Name;
            string typeName = typeUpper;

            if (!_usedIfcTypes.ContainsKey(typeName))
            {
                _usedIfcTypes[typeName] = new RdfURIClassDef(NS_Schema, typeName, null, false, null, null);
            }
            return _usedIfcTypes[typeName];
        }

        public AbstractIFCRdfEntity GetEntity(string qname)
        {
            if (_entities.ContainsKey(qname))
                return _entities[qname];
            return null;
        }

        public bool AddEntity(AbstractIFCRdfEntity ent)
        {
            if (!_entities.ContainsKey(ent.QName))
            {
                _entities[ent.QName] = ent;
                return true;
            }
            return false;
        }


        public IFCRdfValue CreateVal(object nominalValue)
        {
            //string typeName = EXPSchema.GetEXPType(nominalValue.Type.ToString()).Name;
            string typeName = nominalValue.GetType().Name.ToUpperInvariant();

            if (!_values.ContainsKey(typeName))
            {
                _values[typeName] = new Dictionary<string, IFCRdfValue>();
            }
            string strVal = GetValString(nominalValue);
            if (!_values[typeName].ContainsKey(strVal))
            {
                IFCRdfValue val = new IFCRdfValue(this, typeName, _values[typeName].Count, nominalValue);
                _values[typeName][strVal] = val;
            }
            return _values[typeName][strVal];
        }

    }
}
