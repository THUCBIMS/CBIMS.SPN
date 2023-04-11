using System;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Presentation;
using Xbim.Common.Geometry;
using System.Windows.Media.Media3D;
using Xbim.ModelGeometry.Scene;
using System.ComponentModel;
using System.Text;
using System.IO;
using Xbim.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MathNet.Numerics.LinearAlgebra.Double;
using CBIMS.CommonGeom;
using VDS.RDF;
using CBIMS.LDP.Def;
using System.Security.Cryptography;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public class XbimGeomLoader
    {
        //TODO: only IFC4 now

        AbstractIFCRdfModel RefModel;
        IfcStore Store;

        public bool SPLIT_ELEMENT_INTO_LEVELS = true;
        public bool AUTO_FIND_LEVELS = true;
        public bool USE_DEFAULT_EXCLUSION = true;

        

        BackgroundWorker _loadFileBackgroundWorker;
        public event ProgressChangedEventHandler ProgressChanged;


        Dictionary<string, int> LevelNameToLevelID = new Dictionary<string, int>(); //TODO: multiple level has the same name

        Dictionary<int, HashSet<int>> StructureToContent_Raw = null;


        Dictionary<int, HashSet<int>> StructureToContent = new Dictionary<int, HashSet<int>>();
        Dictionary<int, HashSet<int>> ContentToStructure = new Dictionary<int, HashSet<int>>();

        public LevelRtree Index = new LevelRtree();

        public XbimGeomLoader(AbstractIFCRdfModel refModel, IfcStore store)
        {
            RefModel = refModel;
            Store = store;

            ProgressChanged += OnProgressChanged;

            _loadFileBackgroundWorker = new BackgroundWorker();
            _loadFileBackgroundWorker.ProgressChanged += OnProgressChanged;
            _loadFileBackgroundWorker.WorkerReportsProgress = true;

            _TestCRedist();
        }

        private void _TestCRedist()
        {
            if (Xbim.ModelGeometry.XbimEnvironment.RedistInstalled())
                return;
            //Logger.LogError("Requisite C++ environment missing, download and install from {VCPath}",
            //    Xbim.ModelGeometry.XbimEnvironment.RedistDownloadPath());
            string VCPath = Xbim.ModelGeometry.XbimEnvironment.RedistDownloadPath();
            Console.WriteLine($"Requisite C++ environment missing, download and install from {VCPath}");
        }

        private void OnProgressChanged(object s, ProgressChangedEventArgs args)
        {
            if (args.ProgressPercentage < 0 || args.ProgressPercentage > 100)
                return;

            if(args.ProgressPercentage % 10 == 0)
                Console.WriteLine((string)args.UserState + " " + args.ProgressPercentage + "%");

            //Application.Current.Dispatcher.BeginInvoke(
            //    DispatcherPriority.Send,
            //    new Action(() =>
            //    {
            //        ProgressBar.Value = args.ProgressPercentage;
            //        StatusMsg.Text = (string)args.UserState;
            //    }));
            return;
        }


        public void Load(HashSet<int> required_levelIds)
        {

            Dictionary<int, (PointInt, PointInt)> idAxisMap = _getIdAxisMap(Store);

            _loadRawSubStructureTree(Store, required_levelIds);


            Dictionary<int, BBox> geom = _loadGeom(Store, idAxisMap);

            _loadSpaces(Store);

            Console.WriteLine("Building geomtry index...");


            foreach (int id in geom.Keys)
            {

                IPersistEntity ent = Store.Instances[id];
                BBox box = geom[id];


                string levelName = _tryGetLevelName(ent);
                string input_level_name = null;
                if (levelName != null && levelName != LevelRtree.DEFAULT_LEVEL_NAME)
                {
                    //keeps the level name that are recorded in the raw data
                    input_level_name = levelName;
                }
                    

                PointInt location = _getLocation(ent);

                PointIntList locationCurve = _getLocationCurve(ent, idAxisMap);

                string elemId = ent.EntityLabel.ToString();
                Index.AddEntity(box, elemId, input_level_name, location, locationCurve, SPLIT_ELEMENT_INTO_LEVELS && AUTO_FIND_LEVELS);

                var new_levelName = Index.GetContainedInLevelById(elemId);



                if(new_levelName != null && new_levelName != input_level_name && LevelNameToLevelID.ContainsKey(new_levelName))
                {
                    int levelId = LevelNameToLevelID[new_levelName];
                    if (!StructureToContent.ContainsKey(levelId))
                        StructureToContent[levelId] = new HashSet<int>();
                    StructureToContent[levelId].Add(ent.EntityLabel);
                    if(!ContentToStructure.ContainsKey(ent.EntityLabel))
                        ContentToStructure[ent.EntityLabel] = new HashSet<int>();
                    ContentToStructure[ent.EntityLabel].Add(levelId);

                }

            }

        }


        public void FixLevelRels()
        {
            Console.WriteLine("FixLevelRels");

            Dictionary<int, HashSet<int>> new_StructureToElems = new Dictionary<int, HashSet<int>>();

            var elements = Store.FindAll<Xbim.Ifc4.Interfaces.IIfcElement>();
            foreach(var element in elements)
            {
                int elemId = element.EntityLabel;
                var elemRec = RefModel.GetEntity(RefModel.GetEntityQName(element));

                string levelName = _tryGetLevelName(element); //containing the new-found rels

                if(levelName != null && LevelNameToLevelID.ContainsKey(levelName))
                {
                    var levelId = LevelNameToLevelID[levelName];
                    var level = Store.Instances[levelId];
                    var levelRec = RefModel.GetEntity(RefModel.GetEntityQName(level));

                    if (StructureToContent_Raw.ContainsKey(levelId)
                        && StructureToContent_Raw[levelId].Contains(elemId))
                    {
                        //pass
                    }
                    else
                    {
                        if (!new_StructureToElems.ContainsKey(levelId))
                            new_StructureToElems[levelId] = new HashSet<int>();
                        new_StructureToElems[levelId].Add(elemId);
                    }
                }
            }


            foreach (var structId in new_StructureToElems.Keys)
            {
                var structInst = Store.Instances[structId];
                var structRec = RefModel.GetEntity(RefModel.GetEntityQName(structInst));

                if (structRec == null)
                    continue;

                DefaultIFCRdfEntity new_rel = new DefaultIFCRdfEntity(RefModel, null, typeof(Xbim.Ifc4.ProductExtension.IfcRelContainedInSpatialStructure), - structId);

                RefModel.AddEntity(new_rel);

                new_rel.AddProp(
                    RefModel.GetEdgeName("relatingStructure", typeof(Xbim.Ifc4.ProductExtension.IfcRelContainedInSpatialStructure))
                    , structRec);
                structRec.AddProp(
                    RefModel.GetEdgeName("containsElements", structInst.GetType())
                    , new_rel);

                foreach (var elemId in new_StructureToElems[structId])
                {
                    var elemInst = Store.Instances[elemId];
                    var elemRec = RefModel.GetEntity(RefModel.GetEntityQName(elemInst));
                    if (elemRec == null)
                        continue;

                    new_rel.AddProp(
                    RefModel.GetEdgeName("RelatedElements", typeof(Xbim.Ifc4.ProductExtension.IfcRelContainedInSpatialStructure))
                    , elemRec);

                    elemRec.AddProp(
                        RefModel.GetEdgeName("containedInSpatialStructure_RelatedInv", elemInst.GetType())
                        , new_rel);
                }

            }


        }

        private Dictionary<int, List<GeometryModel3D>> _MyBuildScene(IModel model, XbimMatrix3D modelTransform, List<IPersistEntity> isolateInstances = null, List<IPersistEntity> hideInstances = null, List<Type> excludeTypes = null)
        {
            Dictionary<int, List<GeometryModel3D>> outGeometry = new Dictionary<int, List<GeometryModel3D>>();
            var excludedTypes = new HashSet<short>();
            if (USE_DEFAULT_EXCLUSION)
            {
                excludedTypes = model.DefaultExclusions(excludeTypes);
            }
            var onlyInstances = isolateInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);
            var hiddenInstances = hideInstances?.Where(i => i.Model == model).ToDictionary(i => i.EntityLabel);

            //var scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);

            using (var geomStore = model.GeometryStore)
            {
                using (var geomReader = geomStore.BeginRead())
                {
                    var repeatedShapeGeometries = new Dictionary<int, MeshGeometry3D>();

                    //WpfMaterial defaultMaterial = new WpfMaterial();


                    var shapeInstances = _getShapeInstancesToRender(geomReader, excludedTypes);
                    var tot = 1;
                    if (ProgressChanged != null)
                    {
                        // only enumerate if there's a need for progress update
                        tot = shapeInstances.Count();
                    }
                    var prog = 0;
                    var lastProgress = 0;

                    // !typeof (IfcFeatureElement).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId)) /*&&
                    // !typeof(IfcSpace).IsAssignableFrom(IfcMetaData.GetType(s.IfcTypeId))*/);
                    foreach (var shapeInstance in shapeInstances
                        .Where(s => null == onlyInstances || onlyInstances.Count == 0 || onlyInstances.Keys.Contains(s.IfcProductLabel))
                        .Where(s => null == hiddenInstances || hiddenInstances.Count == 0 || !hiddenInstances.Keys.Contains(s.IfcProductLabel)))
                    {
                        // logging 
                        var currentProgress = 100 * prog++ / tot;
                        if (currentProgress != lastProgress && ProgressChanged != null)
                        {
                            ProgressChanged(this, new ProgressChangedEventArgs(currentProgress, "Creating visuals"));
                            lastProgress = currentProgress;
                        }

                        //GET THE ACTUAL GEOMETRY 
                        MeshGeometry3D wpfMesh;
                        //see if we have already read it
                        if (repeatedShapeGeometries.TryGetValue(shapeInstance.ShapeGeometryLabel, out wpfMesh))
                        {

                            var mg = new GeometryModel3D();
                            mg.Geometry = wpfMesh;
                            mg.Transform =
                                XbimMatrix3D.Multiply(shapeInstance.Transformation,
                                    modelTransform)
                                    .ToMatrixTransform3D();
                            if (!outGeometry.ContainsKey(shapeInstance.IfcProductLabel))
                            {
                                outGeometry.Add(shapeInstance.IfcProductLabel, new List<GeometryModel3D>());
                            }
                            outGeometry[shapeInstance.IfcProductLabel].Add(mg);
                        }
                        else //we need to get the shape geometry
                        {
                            IXbimShapeGeometryData shapeGeom = geomReader.ShapeGeometry(shapeInstance.ShapeGeometryLabel);


                            wpfMesh = new MeshGeometry3D();
                            switch ((XbimGeometryType)shapeGeom.Format)
                            {
                                case XbimGeometryType.PolyhedronBinary:
                                    wpfMesh.Read(shapeGeom.ShapeData);
                                    break;
                                case XbimGeometryType.Polyhedron:
                                    wpfMesh.Read(((XbimShapeGeometry)shapeGeom).ShapeData);
                                    break;
                            }
                            repeatedShapeGeometries.Add(shapeInstance.ShapeGeometryLabel, wpfMesh);

                            var mg = new GeometryModel3D();
                            mg.Geometry = wpfMesh;
                            mg.Transform = XbimMatrix3D.Multiply(shapeInstance.Transformation, modelTransform).ToMatrixTransform3D();
                            if (!outGeometry.ContainsKey(shapeInstance.IfcProductLabel))
                            {
                                outGeometry.Add(shapeInstance.IfcProductLabel, new List<GeometryModel3D>());
                            }
                            outGeometry[shapeInstance.IfcProductLabel].Add(mg);

                        }
                    }


                }
            }
            //Logger.LogDebug("Time to load visual components: {0:F3} seconds", timer.Elapsed.TotalSeconds);

            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(100, $"Ready"));
            //return scene;
            return outGeometry;
        }


        private string _tryGetLevelName(IPersistEntity ent)
        {
            if (SPLIT_ELEMENT_INTO_LEVELS)
            {
                if (ContentToStructure.ContainsKey(ent.EntityLabel))
                {
                    foreach(var levelName in LevelNameToLevelID.Keys)
                    {
                        var levelId = LevelNameToLevelID[levelName];
                        if(ContentToStructure[ent.EntityLabel].Contains(levelId))
                        { 
                            return levelName; 
                        }
                    }

                }
            }

            return LevelRtree.DEFAULT_LEVEL_NAME;
        }


        private void _loadRawSubStructureTree(IfcStore store, HashSet<int> required_levelIds)
        {
            var levels = new List<Level>();
            switch (store.SchemaVersion)
            {
                case Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3:
                    {
                        var storeys = store.Instances.OfType<Xbim.Ifc2x3.Interfaces.IIfcBuildingStorey>();
                        foreach(var storey in storeys)
                        {
                            if (required_levelIds != null && required_levelIds.Contains(storey.EntityLabel))
                                continue;


                            Level level = new Level(storey.Name, (int)storey.Elevation, null, null, null);
                            levels.Add(level);

                            LevelNameToLevelID[storey.Name] = storey.EntityLabel;

                            if (!StructureToContent.ContainsKey(storey.EntityLabel))
                                StructureToContent.Add(storey.EntityLabel, new HashSet<int>());
                            var ids = StructureToContent[storey.EntityLabel];
                            findSubStructures(storey, ref ids);
                        }

                    }
                    break;
                case Xbim.Common.Step21.XbimSchemaVersion.Ifc4:
                    {
                        var storeys = store.Instances.OfType<Xbim.Ifc4.Interfaces.IIfcBuildingStorey>();
                        foreach (var storey in storeys)
                        {
                            Level level = new Level(storey.Name, (int)storey.Elevation, null, null, null);
                            levels.Add(level);

                            LevelNameToLevelID[storey.Name] = storey.EntityLabel;

                            if (!StructureToContent.ContainsKey(storey.EntityLabel))
                                StructureToContent.Add(storey.EntityLabel, new HashSet<int>());
                            var ids = StructureToContent[storey.EntityLabel];
                            findSubStructures(storey, ref ids);
                        }
                    }
                    break;
                default:
                    break;
            }

            //save raw records
            StructureToContent_Raw = new Dictionary<int, HashSet<int>>();
            foreach(int structureId in StructureToContent.Keys)
            {
                StructureToContent_Raw[structureId] = new HashSet<int>(StructureToContent[structureId]);
            }

            //inv
            foreach (int structureId in StructureToContent.Keys)
            {
                foreach(int id in StructureToContent[structureId])
                {
                    if(!ContentToStructure.ContainsKey(id))
                        ContentToStructure[id] = new HashSet<int>();
                    ContentToStructure[id].Add(structureId);
                }
            }

            Index.InitLevels(levels);
        }


        private void _loadSpaces(IfcStore store)
        {
            switch (store.SchemaVersion)
            {
                case Xbim.Common.Step21.XbimSchemaVersion.Ifc2X3:
                    {
                        var spaces = store.Instances.OfType<Xbim.Ifc2x3.Interfaces.IIfcSpace>();
                        //TODO

                    }
                    break;
                case Xbim.Common.Step21.XbimSchemaVersion.Ifc4:
                    {
                        var spaces = store.Instances.OfType<Xbim.Ifc4.Interfaces.IIfcSpace>();


                        foreach (var space in spaces)
                        {
                            if (!ContentToStructure.ContainsKey(space.EntityLabel))
                            {
                                //not allocated in a level
                                continue;
                            }
                            var superStructureIds = ContentToStructure[space.EntityLabel];

                            foreach(var levelName in LevelNameToLevelID.Keys)
                            {
                                var levelId = LevelNameToLevelID[levelName];
                                if (!superStructureIds.Contains(levelId))
                                {
                                    continue;
                                }

                                if (space.Representation != null && space.Representation.Representations != null)
                                {
                                    bool found = false;

                                    foreach (var rep in space.Representation.Representations)
                                    {

                                        if (rep is Xbim.Ifc4.Interfaces.IIfcShapeRepresentation shape)
                                        {
                                            var representationIdentifier = shape.RepresentationIdentifier.UnWrap().ToString();
                                            var representationType = shape.RepresentationType.UnWrap().ToString();
                                            if (representationIdentifier == "Body" && representationType == "SweptSolid")
                                            {
                                                foreach (var item in shape.Items)
                                                {
                                                    if (item is Xbim.Ifc4.Interfaces.IIfcExtrudedAreaSolid solid)
                                                    {
                                                        PointIntList2D curve = IFCGeomUtils_IFC4.ParseExtruded(solid, true) as PointIntList2D;
                                                        if (curve != null)
                                                        {
                                                            string box_id = item.EntityLabel.ToString();
                                                            var box = new Polygon2D(new List<PointIntList2D> { curve }, box_id);

                                                            Index.AddEntity(box, space.EntityLabel.ToString(), levelName, null, curve, false);
                                                            found = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //pass
                                                    }
                                                    if (found)
                                                        break;
                                                }
                                            }
                                            else if (representationIdentifier == "FootPrint")
                                            {
                                                foreach (var item in shape.Items)
                                                {
                                                    if (item is Xbim.Ifc4.Interfaces.IIfcCurve _curve)
                                                    {
                                                        PointIntList2D curve = IFCGeomUtils_IFC4.ParseCurve(_curve, true) as PointIntList2D;

                                                        if (curve != null)
                                                        {
                                                            string box_id = _curve.EntityLabel.ToString();
                                                            var box = new Polygon2D(new List<PointIntList2D> { curve }, box_id);

                                                            Index.AddEntity(box, space.EntityLabel.ToString(), levelName, null, curve, false);
                                                            found = true;
                                                        }
                                                    }
                                                    else if (item is Xbim.Ifc4.Interfaces.IIfcGeometricCurveSet curveSet)
                                                    {
                                                        foreach(var inner_curve in curveSet.Elements)
                                                        {
                                                            if(inner_curve is Xbim.Ifc4.Interfaces.IIfcCurve _inner_curve)
                                                            {
                                                                PointIntList2D curve = IFCGeomUtils_IFC4.ParseCurve(_inner_curve, true) as PointIntList2D;

                                                                if (curve != null)
                                                                {
                                                                    string box_id = _inner_curve.EntityLabel.ToString();
                                                                    var box = new Polygon2D(new List<PointIntList2D> { curve }, box_id);

                                                                    Index.AddEntity(box, space.EntityLabel.ToString(), levelName, null, curve, false);
                                                                    found = true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        //pass
                                                    }
                                                    if (found)
                                                        break;
                                                }
                                            }
                                        }
                                        if (found)
                                            break;
                                    }
                                }
                            }



                        }
                    }
                    break;
                default:
                    break;
            }
        }


        private void findSubStructures(Xbim.Ifc4.Interfaces.IIfcObjectDefinition o, ref HashSet<int> output)
        {
            var spatialElement = o as Xbim.Ifc4.Interfaces.IIfcSpatialStructureElement;
            if (spatialElement != null)
            {
                HashSet<Xbim.Ifc4.Interfaces.IIfcObjectDefinition> elements = new HashSet<Xbim.Ifc4.Interfaces.IIfcObjectDefinition>();
                elements.UnionWith(spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements));
                elements.UnionWith(spatialElement.IsDecomposedBy.SelectMany(rel => rel.RelatedObjects));
                foreach (var element in elements)
                {
                    output.Add(element.EntityLabel);
                    if (element is Xbim.Ifc4.Interfaces.IIfcSpatialStructureElement sub_space)
                    {
                        HashSet<int> sub_output = null;
                        if (!StructureToContent.ContainsKey(sub_space.EntityLabel))
                        {
                            sub_output = new HashSet<int>();
                            StructureToContent[sub_space.EntityLabel] = sub_output;
                            findSubStructures(sub_space, ref sub_output);
                        }
                        sub_output = StructureToContent[sub_space.EntityLabel];
                        output.UnionWith(sub_output);
                    }
                }
               

            }
        }

        private void findSubStructures(Xbim.Ifc2x3.Interfaces.IIfcObjectDefinition o, ref HashSet<int> output)
        {
            var spatialElement = o as Xbim.Ifc2x3.Interfaces.IIfcSpatialStructureElement;
            if (spatialElement != null)
            {
                HashSet<Xbim.Ifc2x3.Interfaces.IIfcObjectDefinition> elements = new HashSet<Xbim.Ifc2x3.Interfaces.IIfcObjectDefinition>();
                elements.UnionWith(spatialElement.ContainsElements.SelectMany(rel => rel.RelatedElements));
                elements.UnionWith(spatialElement.IsDecomposedBy.SelectMany(rel => rel.RelatedObjects));
                foreach (var element in elements)
                {
                    output.Add(element.EntityLabel);
                    if (element is Xbim.Ifc2x3.Interfaces.IIfcSpatialStructureElement sub_space)
                    {
                        HashSet<int> sub_output = null;
                        if (!StructureToContent.ContainsKey(sub_space.EntityLabel))
                        {
                            sub_output = new HashSet<int>();
                            StructureToContent[sub_space.EntityLabel] = sub_output;
                            findSubStructures(sub_space, ref sub_output);
                        }
                        sub_output = StructureToContent[sub_space.EntityLabel];
                        output.UnionWith(sub_output);
                    }
                }

            }
        }
        

        private Dictionary<int, (PointInt, PointInt)> _getIdAxisMap(IfcStore store)
        {
            //TODO
            return new Dictionary<int, (PointInt, PointInt)>();
        }

        private PointInt _getLocation(IPersistEntity ent)
        {
            //TODO
            return null;
        }

        private PointIntList _getLocationCurve(IPersistEntity ent, Dictionary<int, (PointInt, PointInt)> idAxisMap)
        {
            if (false)
            {
                //TODO: load from IFC
            }
            else if(idAxisMap.ContainsKey(ent.EntityLabel))
            {
                
                var s = idAxisMap[ent.EntityLabel].Item1;
                var e = idAxisMap[ent.EntityLabel].Item2;
                PointIntList locationCurve = new PointIntList();
                locationCurve.Add(s);
                locationCurve.Add(e);
                return locationCurve;
            }
            else
            {
                return null;
            }
        }




        private Dictionary<int, BBox> _loadGeom(IfcStore store, Dictionary<int, (PointInt, PointInt)> idAxisMap)
        {
            Dictionary<int, BBox> output = new Dictionary<int, BBox>();

            if (store.GeometryStore.IsEmpty)
            {
                // Create the geometry using the XBIM GeometryEngine
                try
                {
                    var context = new Xbim3DModelContext(store);
                    context.CreateContext(_loadFileBackgroundWorker.ReportProgress, true);
                }
                catch (Exception geomEx)
                {
                    Console.WriteLine($"Failed to create geometry for {store.FileName}");
                    Console.WriteLine(geomEx.GetType().Name + geomEx.Message);
                }
            }


            Dictionary<int, List<GeometryModel3D>> idGeomsMap = _MyBuildScene(store, XbimMatrix3D.Identity);

            foreach(var id in idGeomsMap.Keys)
            {
                ArrayDouble AxisA = null;
                if(idAxisMap != null && idAxisMap.ContainsKey(id))
                {
                    AxisA = (idAxisMap[id].Item2 - idAxisMap[id].Item1).ToArrayDouble();
                }


                var geoms = idGeomsMap[id];
                if (geoms.Count > 1)
                {
                    List<BBox> children = new List<BBox>();

                    foreach (var geom in geoms)
                    {
                        children.Add(_readBBox(geom, AxisA, null));
                    }
                    BBoxs bboxs = new BBoxs(children, id.ToString());
                    output.Add(id, bboxs);
                }
                else
                {
                    var geom = geoms[0];
                    var box = _readBBox(geom, AxisA, id.ToString());
                    output.Add(id, box);
                }

            }

            return output;




        }

        private BBox _readBBox(GeometryModel3D geom, ArrayDouble axisA, string id)
        {
            MeshGeometry3D geomNode = geom.Geometry as MeshGeometry3D;
            PointIntSet points = _readPoints(geomNode) as PointIntSet;
            var transform = _readTransform(geom.Transform);

            if (!transform.GetMatrix().IsIdentity4())
            {
                points = points.ApplyTransform(transform) as PointIntSet;
            }

            if (axisA != null)
            {
                return points.GetOBB(axisA, id);
            }
            else
            {
                return points.GetOBB(id);
            }
        }

        private IEnumerable<XbimShapeInstance> _getShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
        {
            var shapeInstances = geomReader.ShapeInstances
                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
                            &&
                            !excludedTypes.Contains(s.IfcTypeId));
            return shapeInstances;
        }

        private Transform _readTransform(Transform3D trans)
        {
            var m = trans.Value;
            double[] M_list =
            {m.M11,m.M21,m.M31,0,
            m.M12,m.M22,m.M32,0,
            m.M13,m.M23,m.M33,0,
            m.M14,m.M24,m.M34,m.M44};
            DenseMatrix mat = new DenseMatrix(4, 4, M_list);
            return new Transform(mat);

        }


        private PointIntSet _readPoints(MeshGeometry3D geomNode)
        {
            PointIntSet pset = new PointIntSet();
            foreach (var point in geomNode.Positions)
            {
                pset.Add(new PointInt(point.X, point.Y, point.Z));
            }
            return pset;
        }


    }
}
