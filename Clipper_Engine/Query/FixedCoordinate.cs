using BH.oM.Geometry;
using BH.oM.Clipper;
using System;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        public static double FixedCoordinate(this Point p, PrincipalPlane plane)
        {
            if (plane == PrincipalPlane.XY) return p.Z;
            if (plane == PrincipalPlane.XZ) return p.Y;
            if (plane == PrincipalPlane.YZ) return p.X;
            throw new ArgumentException("Invalid principal plane");
        }

        /***************************************************/
    }
}
