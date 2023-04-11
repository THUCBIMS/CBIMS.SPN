using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.IFC
{
    public abstract class AbstractIFCRdfEntity : RdfInstPersist
    {
        public object Entity { get; }
        public AbstractIFCRdfModel Host { get; }
        protected string PrefixNC_Schema => Host.NS_Schema.PrefixNC;


        protected AbstractIFCRdfEntity(AbstractIFCRdfModel host, object ent, Type instType, int entId) : base(host.NS, $"{instType.Name}_{entId}", null, null)
        {
            Entity = ent;
            Host = host;

            RdfURIClassDef classDef = host.GetClassDef(instType);
            this.AddType(classDef);

            host.AddEntity(this);
        }

    }
    public class DefaultIFCRdfEntity : AbstractIFCRdfEntity
    {
        public DefaultIFCRdfEntity(AbstractIFCRdfModel host, object ent, Type instType, int entId) : base(host, ent, instType, entId)
        {
        }
    }
}
