using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;

namespace CBIMS.LDP.Def
{

    public interface IRdfInst : IRdfNode
    {
        IGraph Graph { get; }

        IEnumerable<IRdfClassDef> Types { get; }
        IEnumerable<T> GetProp<T>(string qname);
        IEnumerable<object> GetProp(string qname);
        T GetPropSingle<T>(string qname);
        object GetPropSingle(string qname);

        bool HasProp(string qname);
        bool HasPropVal(string qname, object v);
    }
    public interface IRdfInstEditable : IRdfInst
    {
        IRdfInstEditable AddType(IRdfClassDef type);
        IRdfInstEditable AddProp(string key, object val);
        IRdfInstEditable SetProp(string key, object val);
        IRdfInstEditable SetProps(string key, IEnumerable<object> vals);
        IRdfInstEditable SetProps<T>(string key, IEnumerable<T> vals);

        IRdfInstEditable RemovePropAll(string key);
        IRdfInstEditable RemoveProp (string key, object val);
        
    }

    public interface IRdfInstWithTransaction : IRdfInstEditable
    {
        IRdfInstEditable Commit();
        IRdfInstEditable Cancel();
    }

}
