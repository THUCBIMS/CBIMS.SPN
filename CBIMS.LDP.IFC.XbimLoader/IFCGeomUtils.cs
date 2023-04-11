using CBIMS.CommonGeom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace CBIMS.LDP.IFC.XbimLoader
{
    public static class IFCGeomUtils
    {
        public static ArrayDouble ParseDirection(XbimVector3D vec)
        {
            return new ArrayDouble(vec.X, vec.Y, vec.Z);
        }
    }
}
