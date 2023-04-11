using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.IFC
{
    public class RelDefInfo
    {
        public string RelName;

        public string RelatingArgName;
        public string RelatingTargetName;
        public Type RelatingTargetType;
        public string RelatingInvArgName;
        public bool RelatingColl;

        public string RelatedArgName;
        public string RelatedTargetName;
        public Type RelatedTargetType;
        public string RelatedInvArgName;
        public bool RelatedColl;
    }
}
