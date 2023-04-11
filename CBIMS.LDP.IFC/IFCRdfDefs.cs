using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.IFC
{
    public static class IFCRdfDefs
    {
        public static readonly RdfNSDef IFCOWL_IFC4 = new RdfNSDef(null, "https://standards.buildingsmart.org/IFC/DEV/IFC4/ADD2_TC1/OWL#", "ifcowl", false);

        public static readonly RdfNSDef IFCRDF_IFC4 = new RdfNSDef(null, "http://www.cbims.org.cn/ns/ifcrdf/ifc4#", "ifc4", true);
    }
}
