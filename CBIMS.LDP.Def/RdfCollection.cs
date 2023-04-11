using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using static System.Net.Mime.MediaTypeNames;

namespace CBIMS.LDP.Def
{
    public class RdfReadOnlyList : RdfInstPersist
    {
        public RdfReadOnlyList(IEnumerable<object> contents, RdfNSDef ns, string name, IUriNode node = null) : base(ns, name, RDFSCommonDef.List, node)
        {
            RdfInstPersist current_list = this;

            string prefix = name;
            if (prefix.EndsWith("_0"))
            {
                prefix = prefix.Substring(0, prefix.Length - 2);
            }

            var _contents = contents.ToList();

            for (int i = 0; i < _contents.Count; i++)
            {
                object content = _contents[i];
                current_list.AddProp(RDFSCommonDef.first.QName, content);
                if(i < _contents.Count - 1)
                {
                    RdfInstPersist next = new RdfInstPersist(ns, prefix + $"_{i + 1}", null);
                    current_list.AddProp(RDFSCommonDef.rest.QName, next);
                    current_list = next;
                }
                else
                {
                    current_list.AddProp(RDFSCommonDef.rest.QName, RDFSCommonDef.nil);
                }
            }
        }
    }


}
