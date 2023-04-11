using CBIMS.LDP.Def;
using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.Repo
{
    public static class RepoDefs
    {
        //classes

        public static readonly RdfURIClassDef Container
            = new RdfURIClassDef(RdfCommonNS.LDP, "Container", null, false, typeof(LDPContainer), null,
                _this =>
                {
                    _this.AddPropDef(new RdfPropDef(RdfCommonNS.LDP, "contains"));
                    _this.AddPropDef(new RdfPropDef(RdfCommonNS.LDP, "membershipResource"));
                    _this.AddPropDef(new RdfPropDef(RdfCommonNS.LDP, "hasMemberRelation"));
                    _this.AddPropDef(new RdfPropDef(RdfCommonNS.LDP, "isMemberOfRelation"));
                    _this.AddPropDef(new RdfPropDef(RdfCommonNS.LDP, "insertedContentRelation"));
                }
            );


        public static readonly RdfURIClassDef BasicContainer = new RdfURIClassDef(RdfCommonNS.LDP, "BasicContainer", Container, false, typeof(LDPContainer), null);

        public static readonly RdfURIClassDef DirectContainer = new RdfURIClassDef(RdfCommonNS.LDP, "DirectContainer", Container, false, typeof(LDPContainer), null);

        public static readonly RdfURIClassDef IndirectContainer = new RdfURIClassDef(RdfCommonNS.LDP, "IndirectContainer", Container, false, typeof(LDPContainer), null);

        //contant nodes

        public const string QNAME_member = "ldp:member";
        public const string QNAME_MemberSubject = "ldp:MemberSubject";

    }
}
