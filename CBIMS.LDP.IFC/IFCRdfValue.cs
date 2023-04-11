using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.IFC
{
    public class IFCRdfValue : RdfInstPersist
    {
        public object Value { get; }
        public IFCRdfValue(AbstractIFCRdfModel host, string typeName, int id, object value) : base(host.NS, $"{typeName}_{id}", null, null)
        {
            Value = value;
            AddType(host.NS_Schema, typeName);

            if (ExpressValType != null)
                AddProp(ExpressValType, Value);
        }

        public string ExpressValType
        {
            get
            {
                if (Value == null) return null;
                if (Value is string) return "express:hasString";
                if (Value is double) return "express:hasDouble";
                if (Value is int) return "express:hasInteger";
                if (Value is bool) return "express:hasBoolean";

                throw new NotImplementedException();
            }
        }
    }
}
