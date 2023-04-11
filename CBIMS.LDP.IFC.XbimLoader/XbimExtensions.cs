using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Ifc2x3.Interfaces;

namespace CBIMS.LDP.IFC.XbimLoader
{
    internal static class XbimExtensions
    {
        public static IEnumerable<T> FindAll<T>(this IModel model)
        {
            return model.Instances.Where(t=>t is T).Cast<T>();
        }

        public static object UnWrap(this IExpressValueType value)
        {
            if(value==null) 
                return null;
            return value.Value;
        }

    }
}
