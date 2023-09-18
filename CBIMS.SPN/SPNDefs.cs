// Copyright (C) 2023  Liu, Han; School of Software, Tsinghua University
//
// This file is part of CBIMS.SPN.
// CBIMS.SPN is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// CBIMS.SPN is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with CBIMS.SPN. If not, see <https://www.gnu.org/licenses/>.

using CBIMS.LDP.Repo;
using CBIMS.LDP.Def;
using System;

namespace CBIMS.SPN
{
    public static class SPNDefs
    {
        //order matters

        public static readonly RdfNSDef NS_SPN = new RdfNSDef(null, "http://www.cbims.org.cn/ns/spn#", "spn", true);



        public static readonly OwlClassDef ArgDef
            = new OwlClassDef(NS_SPN, "ArgDef", null, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new OwlDatatypePropertyDef(NS_SPN, "argName", _this, XsdLiteral.STRING, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "argType", _this, RDFSCommonDef.Class, null)
                    );
                }
            );

        public static readonly OwlObjectPropertyDef hasArg 
            = new OwlObjectPropertyDef(NS_SPN, "hasArg", null, ArgDef, null);




        public static readonly OwlClassDef Rule = new OwlClassDef(NS_SPN, "Rule", null, true, null, null);

        public static readonly OwlClassDef SPARQLRule
            = new OwlClassDef(NS_SPN, "SPARQLRule", Rule, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new OwlDatatypePropertyDef(NS_SPN, "hasSPARQL", _this, XsdLiteral.STRING, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                }
            );

        public static readonly OwlClassDef ConstantRule
            = new OwlClassDef(NS_SPN, "ConstantRule", Rule, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new RdfPropDef(NS_SPN, "hasValue", _this, RDFSCommonDef.Resource, null)
                    );
                }
            );


        public static readonly OwlClassDef ArgRule
            = new OwlClassDef(NS_SPN, "ArgRule", Rule, false, null, null,
                _this => {
                    _this.AddPropDef(hasArg.SetCardinalityRestriction(_this, 1, null));
                }
            );


        public static readonly OwlDataRangeDef CompoundRule_Operators
            = new OwlDataRangeDef(new string[] { "AND", "OR", "XOR", "NOT" }, NS_SPN, "CompoundRule_Operators");


        public static readonly OwlClassDef CompoundRule
            = new OwlClassDef(NS_SPN, "CompoundRule", Rule, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "subRule", _this, Rule, null)
                        
                    );
                    _this.AddPropDef(
                        new OwlDatatypePropertyDef(NS_SPN, "operator", _this, CompoundRule_Operators, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                }
            );
        public static readonly OwlClassDef ConditionRule
            = new OwlClassDef(NS_SPN, "ConditionRule", Rule, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "if", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "then", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "else", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                }
            );





        public static readonly OwlClassDef Place
            = new OwlClassDef(NS_SPN, "Place", RepoDefs.Container, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "colorRule", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 0, 1)
                    );
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "initRule", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 0, 1)
                    );
                }
            );

        public static readonly OwlClassDef Transition
            = new OwlClassDef(NS_SPN, "Transition", null, false, null, null,
                _this => {
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "guardRule", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 0, 1)
                    );
                    _this.AddPropDef(hasArg.SetCardinalityRestriction(_this, 1, null));
                }
            );

        public static readonly OwlClassDef Arc
            = new OwlClassDef(NS_SPN, "Arc", null, true, null, null,
                _this =>
                {
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "relPlace", _this, Place, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "relTransition", _this, Transition, null)
                        .SetCardinalityRestriction(_this, 1, 1)
                    );
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "arcExpr", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 0, 1)
                    );
                    _this.AddPropDef(hasArg.SetCardinalityRestriction(_this, 1, null));
                }
            );

        public static readonly OwlClassDef ArcP2T 
            = new OwlClassDef(NS_SPN, "ArcP2T", Arc, false, null, null, 
                _this => {
                    _this.AddPropDef(
                        new OwlObjectPropertyDef(NS_SPN, "arcExprInv", _this, Rule, null)
                        .SetCardinalityRestriction(_this, 0, 1)
                    );
                }
            );

        public static readonly OwlClassDef ArcT2P = new OwlClassDef(NS_SPN, "ArcT2P", Arc, false, null, null);





        public static readonly RdfURIClassDef MultiNumProperty
            = new RdfURIClassDef(NS_SPN, "MultiNumProperty", RDFSCommonDef.Property, false, null, null,
                _this => {
                    _this.AddPropDef(new RdfPropDef(NS_SPN, "multi_num", _this, XsdLiteral.NonNegativeInteger, null));
                    _this.AddPropDef(new RdfPropDef(NS_SPN, "multi_usage", _this, XsdLiteral.NonNegativeInteger, null));
                }
            );



    }
}
