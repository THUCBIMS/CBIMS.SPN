// Copyright (C) 2023  Liu, Han; School of Software, Tsinghua University
//
// This file is part of CBIMS.SPN.
// CBIMS.SPN is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// CBIMS.SPN is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
// You should have received a copy of the GNU Lesser General Public License along with CBIMS.SPN. If not, see <https://www.gnu.org/licenses/>.

using CBIMS.LDP.Def;
using CBIMS.LDP.IFC;
using CBIMS.SPN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using Xbim.Common.Metadata;

namespace Run
{
    internal static class MetaExport
    {
        internal static void RunExportSPNMeta()
        {
            string output_path = @"D:/_UserDoc/SPNTest/spn_owl.ttl";
            var ttl_writer = new CompressingTurtleWriter(TurtleSyntax.W3C);
            ttl_writer.HighSpeedModePermitted = false;

            ttl_writer.DefaultNamespaces.AddNamespace(RdfCommonNS.LDP.PrefixNC, new Uri(RdfCommonNS.LDP.FullPath));

            SPNDefs.NS_SPN.Graph.SaveToFile(output_path, ttl_writer);
        }

        internal static void RunExportIFCRDFMeta()
        {
            string output_path = @"D:/_UserDoc/SPNTest/ifcrdf_ifc4.ttl";
            IGraph graph = IFCRdfDefs.IFCRDF_IFC4.Graph;

            Module module = (typeof(Xbim.Ifc4.Kernel.IfcRoot)).Module;
            ExpressMetaData _Metadata = ExpressMetaData.GetMetadata(module);

            Dictionary<string, RdfURIClassDef> classes = new Dictionary<string, RdfURIClassDef>();
            Dictionary<string, RdfPropDef> properties = new Dictionary<string, RdfPropDef>();

            foreach(ExpressType type in _Metadata.Types())
            {
                _addOneType(IFCRdfDefs.IFCRDF_IFC4, type, classes, properties);
            }



            var ttl_writer = new CompressingTurtleWriter(TurtleSyntax.W3C);
            ttl_writer.HighSpeedModePermitted = false;

            ttl_writer.DefaultNamespaces.AddNamespace(RdfCommonNS.LDP.PrefixNC, new Uri(RdfCommonNS.LDP.FullPath));

            graph.SaveToFile(output_path, ttl_writer);

        }

        private static void _addOneType(RdfNSDef ns, ExpressType type, 
            Dictionary<string, RdfURIClassDef> classes, 
            Dictionary<string, RdfPropDef> properties)
        {
            //TODO: _isAbstract
            bool _isAbstract = false;

            string nameUpper = type.ExpressNameUpper;
            string name = type.ExpressName;

            if(classes.ContainsKey(nameUpper))
            {
                return;
            }

            var superClass = type.SuperType;
            RdfURIClassDef superDef = null;
            if (superClass!=null)
            {
                if (!classes.ContainsKey(superClass.ExpressNameUpper))
                {
                    _addOneType(ns, superClass, classes, properties);
                }
                superDef = classes[superClass.ExpressNameUpper];
            }

            RdfURIClassDef classDef = new RdfURIClassDef(ns, name, superDef, _isAbstract, type.Type);
            classes[nameUpper] = classDef;

            foreach(var pInfo in type.IndexedProperties) 
            { 
                var pUpper = pInfo.Name.ToUpperInvariant();
                var pLower = _firstLower(pInfo.Name);
                if (!properties.ContainsKey(pUpper))
                {
                    properties.Add(pUpper, new RdfPropDef(ns, pLower));
                }
                RdfPropDef pDef = properties[pUpper];
                pDef.AddDomain(classDef);
            }

            HashSet<string> superInvs = null;
            if (superClass != null)
            {
                superInvs = new HashSet<string>(
                    superClass.Inverses.Select(t=>t.Name.ToUpperInvariant()) 
                );
            }
            else
            {
                superInvs = new HashSet<string>();
            }


            foreach(ExpressMetaProperty invInfo in type.Inverses) 
            {
                var pUpper = invInfo.Name.ToUpperInvariant();
                var pLower = _firstLower(invInfo.Name);
                if (superInvs.Contains(pUpper))
                    continue;

                if (!properties.ContainsKey(pUpper))
                {
                    properties.Add(pUpper, new RdfPropDef(ns, pLower));
                }
                RdfPropDef pDef = properties[pUpper];
                pDef.AddDomain(classDef);
            }

        }

        private static string _firstLower(string name)
        {
            string first = name.Substring(0, 1).ToLowerInvariant();
            return first + name.Substring(1);
        }
    }
}
