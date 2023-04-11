using CBIMS.CommonGeom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public static class IFCGeomUtils_IFC4
    {


        public static PointIntList ParseExtruded(IIfcExtrudedAreaSolid solid, bool In2D)
        {
            PointIntList curve;
            var area = solid.SweptArea;
            
            
            if (area is IIfcRectangleProfileDef rec)
            {
                curve = ParseRect(rec);
            }
            else if (area is IIfcArbitraryClosedProfileDef arb)
            {
                curve = ParseCurve(arb.OuterCurve, In2D);
            }
            else
            {
                throw new NotImplementedException("Profile Define");
            }

            var placement = solid.Position;
            if (placement != null)
            {
                Transform trans = ParsePlacement(placement);
                curve = curve.ApplyTransform(trans) as PointIntList;
            }


            if (In2D && !(curve is PointIntList2D))
            {
                PointIntList2D output = new PointIntList2D();
                foreach (var p in curve)
                {
                    output.Add(p);
                }

                return output;
            }
            else
            {
                return curve;
            }

        }

        public static Transform ParsePlacement(IIfcPlacement placement)
        {
            var location = ParsePoint(placement.Location);



            ArrayDouble A = null, B = null, C = null;

            if (placement is IIfcAxis1Placement placement1d)
            {
                C = ParseDirection(placement1d.Axis);
                B = C ^ ArrayDouble.EX;
                A = B ^ C;
            }
            else if (placement is IIfcAxis2Placement2D placement2d)
            {
                A = IFCGeomUtils.ParseDirection(placement2d.P[0]);
                B = IFCGeomUtils.ParseDirection(placement2d.P[1]);
                C = A ^ B;
            }

            else if (placement is IIfcAxis2Placement3D placement3d)
            {
                A = IFCGeomUtils.ParseDirection(placement3d.P[0]);
                B = IFCGeomUtils.ParseDirection(placement3d.P[1]);
                C = IFCGeomUtils.ParseDirection(placement3d.P[2]);
            }
            Transform transform = new Transform(A.Normalize().ToArray(), 
                B.Normalize().ToArray(), C.Normalize().ToArray(), 
                location.ToArrayDouble().ToArray(), 1);

            return transform;
        }

        public static ArrayDouble ParseDirection(IIfcDirection axis)
        {
            return new ArrayDouble(axis.X, axis.Y, axis.Z);
        }

        public static PointIntList ParseCurve(IIfcCurve curve, bool In2D)
        {
            if (curve is IIfcBoundedCurve bounded)
            {
                PointIntList _curve = ParseBoundedCurve(bounded);

                if (In2D && !(_curve is PointIntList2D))
                {
                    PointIntList2D output = new PointIntList2D();
                    foreach (var p in _curve)
                    {
                        output.Add(p);
                    }

                    return output;
                }
                else
                {
                    return _curve;
                }
            }
            else
            {
                throw new NotImplementedException("ParseCurve "+curve.ExpressType.ExpressName);
            }


        }

        public static PointIntList ParseBoundedCurve(IIfcBoundedCurve curve)
        {
            if (curve is IIfcPolyline poly)
            {
                return ParsePolyline(poly);
            }
            else if(curve is IIfcIndexedPolyCurve indexedPoly)
            {
                return ParseIndexedPolyCurve(indexedPoly);
            }
            else
            {
                throw new NotImplementedException("ParseBoundedCurve " + curve.ExpressType.ExpressName);
            }
        }

        public static PointIntList ParseIndexedPolyCurve(IIfcIndexedPolyCurve indexedPoly)
        {
            //TODO: segments
            //TODO: TagList

            PointIntList output = new PointIntList();
            var points = indexedPoly.Points;
            if(points is IIfcCartesianPointList2D points2d)
            {
                foreach(var coord in points2d.CoordList)
                {
                    output.Add(new PointInt(coord[0], coord[1], 0));
                }
            }
            else if(points is IIfcCartesianPointList3D points3d)
            {
                foreach (var coord in points3d.CoordList)
                {
                    output.Add(new PointInt(coord[0], coord[1], coord[2]));
                }
            }
            
            return output;
        }

        public static PointIntList ParsePolyline(IIfcPolyline poly)
        {
            PointIntList output = new PointIntList();
            foreach (var p in poly.Points)
            {
                output.Add(ParsePoint(p));
            }
            return output;
        }

        public static PointIntList ParseRect(IIfcRectangleProfileDef rec)
        {
            var pos = rec.Position;
            PointInt center = ParsePoint(pos.Location);
            ArrayDouble dirX = IFCGeomUtils.ParseDirection(pos.P[0]);
            ArrayDouble dirY = IFCGeomUtils.ParseDirection(pos.P[1]);

            double x = rec.XDim / 2;
            double y = rec.YDim / 2;

            PointInt A = center - (x * dirX).ToPointInt() - (y * dirY).ToPointInt();
            PointInt B = center - (x * dirX).ToPointInt() + (y * dirY).ToPointInt();
            PointInt D = center + (x * dirX).ToPointInt() - (y * dirY).ToPointInt();
            PointInt C = center + (x * dirX).ToPointInt() + (y * dirY).ToPointInt();

            var output = new PointIntList();
            output.Add(A);
            output.Add(B);
            output.Add(C);
            output.Add(D);
            return output;
        }



        public static PointInt ParsePoint(IIfcCartesianPoint location)
        {
            return new PointInt(location.X, location.Y, location.Z);
        }


    }
}
