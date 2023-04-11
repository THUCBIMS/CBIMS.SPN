using System;
using System.Collections.Generic;
using System.Text;

namespace CBIMS.LDP.Def
{

    public static class RdfCommonNS
    {
        public static readonly RdfNSDef XSD = new RdfNSDef(null, "http://www.w3.org/2001/XMLSchema#", "xsd", false);
        public static readonly RdfNSDef RDF = new RdfNSDef(null, "http://www.w3.org/1999/02/22-rdf-syntax-ns#", "rdf", false);
        public static readonly RdfNSDef RDFS = new RdfNSDef(null, "http://www.w3.org/2000/01/rdf-schema#", "rdfs", false);
        public static readonly RdfNSDef OWL = new RdfNSDef(null, "http://www.w3.org/2002/07/owl#", "owl", false);
        public static readonly RdfNSDef LDP = new RdfNSDef(null, "http://www.w3.org/ns/ldp#", "ldp", false);
        public static readonly RdfNSDef EXPRESS = new RdfNSDef(null, "https://w3id.org/express#", "express", false);
        
    }
    public static class XsdLiteral
    {
        public static readonly RdfLiteralClassDef STRING = new RdfLiteralClassDef(LiteralBasic.STRING, RdfCommonNS.XSD, "string", null);
        public static readonly RdfLiteralClassDef INTEGER = new RdfLiteralClassDef(LiteralBasic.INTEGER, RdfCommonNS.XSD, "integer", null);
        public static readonly RdfLiteralClassDef DOUBLE = new RdfLiteralClassDef(LiteralBasic.DOUBLE, RdfCommonNS.XSD, "double", null);
        public static readonly RdfLiteralClassDef BOOLEAN = new RdfLiteralClassDef(LiteralBasic.BOOLEAN, RdfCommonNS.XSD, "boolean", null);
    
        public static readonly RdfLiteralClassDef NonNegativeInteger = new RdfLiteralClassDef(LiteralBasic.INTEGER, RdfCommonNS.XSD, "nonNegativeInteger", null);

    }
    public static class RDFSCommonDef
    {
        //order matters
        public static readonly RdfPropDef a = new RdfPropDef(RdfCommonNS.RDF, "type", null);

        public static readonly RdfPropDef label = new RdfPropDef(RdfCommonNS.RDFS, "label", null);
        public static readonly RdfPropDef comment = new RdfPropDef(RdfCommonNS.RDFS, "comment", null);
        public static readonly RdfPropDef subClassOf = new RdfPropDef(RdfCommonNS.RDFS, "subClassOf", null);
        public static readonly RdfPropDef domain = new RdfPropDef(RdfCommonNS.RDFS, "domain", null);
        public static readonly RdfPropDef range = new RdfPropDef(RdfCommonNS.RDFS, "range", null);

        public static readonly RdfURIClassDef Resource = new RdfURIClassDef(RdfCommonNS.RDFS, "Resource", null, false, null, null);
        public static readonly RdfURIClassDef Class = new RdfURIClassDef(RdfCommonNS.RDFS, "Class", Resource, false, null, null);
        public static readonly RdfURIClassDef Literal = new RdfURIClassDef(RdfCommonNS.RDFS, "Literal", Resource, false, null, null);
        public static readonly RdfURIClassDef DataType = new RdfURIClassDef(RdfCommonNS.RDFS, "DataType", Class, false, null, null);
        public static readonly RdfURIClassDef Property = new RdfURIClassDef(RdfCommonNS.RDF, "Property", Resource, false, null, null);
        
        //Collection
        
        public static readonly RdfURIClassDef List = new RdfURIClassDef(RdfCommonNS.RDF, "List", Resource, false, null, null);
        public static readonly RdfPropDef first = new RdfPropDef(RdfCommonNS.RDF, "first", List, Resource, null);
        public static readonly RdfPropDef rest = new RdfPropDef(RdfCommonNS.RDF, "rest", List, List, null);
        public static readonly RdfInstPersist nil = new RdfInstPersist(RdfCommonNS.RDF, "nil", List);


    }

    public static class OWLCommonDef
    {
        public static readonly RdfURIClassDef Class = new RdfURIClassDef(RdfCommonNS.OWL, "Class", RDFSCommonDef.Class, false, null, null);

        public static readonly RdfURIClassDef DataRange = new RdfURIClassDef(RdfCommonNS.OWL, "DataRange", RDFSCommonDef.Class, false, null, null);


        public static readonly OwlClassDef Thing = new OwlClassDef(RdfCommonNS.OWL, "Thing", null, false, null, null);
        public static readonly OwlClassDef Nothing = new OwlClassDef(RdfCommonNS.OWL, "Nothing", null, false, null, null);

        public static readonly OwlClassDef ObjectProperty = new OwlClassDef(RdfCommonNS.OWL, "ObjectProperty", RDFSCommonDef.Property, false, null, null);
        public static readonly OwlClassDef DatatypeProperty = new OwlClassDef(RdfCommonNS.OWL, "DatatypeProperty", RDFSCommonDef.Property, false, null, null);




        public static readonly RdfPropDef oneOf = new RdfPropDef(RdfCommonNS.OWL, "oneOf", Class, RDFSCommonDef.List, null);



        //restrictions

        public static readonly OwlClassDef Restriction = new OwlClassDef(RdfCommonNS.OWL, "Restriction", null, false, null, null);

        public static readonly RdfPropDef onProperty = new RdfPropDef(RdfCommonNS.OWL, "onProperty", Restriction, RDFSCommonDef.Property, null);



        public static readonly RdfPropDef cardinality = new RdfPropDef(RdfCommonNS.OWL, "cardinality", Restriction, XsdLiteral.NonNegativeInteger, null);
        public static readonly RdfPropDef minCardinality = new RdfPropDef(RdfCommonNS.OWL, "minCardinality", Restriction, XsdLiteral.NonNegativeInteger, null);
        public static readonly RdfPropDef maxCardinality = new RdfPropDef(RdfCommonNS.OWL, "maxCardinality", Restriction, XsdLiteral.NonNegativeInteger, null);
        public static readonly RdfPropDef allValuesFrom = new RdfPropDef(RdfCommonNS.OWL, "allValuesFrom", Restriction, RDFSCommonDef.Class, null);



    }
}
