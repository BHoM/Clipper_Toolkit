using BH.Engine.Geometry;
using BH.oM.Geometry;
using BH.oM.Clipper;
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        public static Polyline UnprojectFrom2D(this Path64 path, PrincipalPlane plane, double fixedCoord, double scale = 1e6)
        {
            List<Point> points = path.Select(pt =>
            {
                Point projected2D = BH.Engine.Geometry.Create.Point(pt.X / scale, pt.Y / scale);
                return UnprojectFrom2D(projected2D, plane, fixedCoord);
            }).ToList();

            // BHoM requires polygons to be explicitly closed (last point identical to the first).
            return new Polyline { ControlPoints = points }.Close();
        }

        /***************************************************/

        public static Polyline UnprojectFrom2D(this List<Point64> points64, PrincipalPlane plane, double fixedCoord, double scale = 1e6)
        {
            List<Point> points = points64.Select(pt =>
            {
                Point projected2D = BH.Engine.Geometry.Create.Point(pt.X / scale, pt.Y / scale);
                return UnprojectFrom2D(projected2D, plane, fixedCoord);
            }).ToList();

            // BHoM requires polygons to be explicitly closed (last point identical to the first).
            return new Polyline { ControlPoints = points }.Close();
        }

        /***************************************************/

        public static Point UnprojectFrom2D(this Point p, PrincipalPlane plane, double fixedCoord)
        {
            if (plane == PrincipalPlane.XY)
                return new Point { X = p.X, Y = p.Y, Z = fixedCoord };
            if (plane == PrincipalPlane.XZ)
                return new Point { X = p.X, Y = fixedCoord, Z = p.Y };
            if (plane == PrincipalPlane.YZ)
                return new Point { X = fixedCoord, Y = p.X, Z = p.Y };

            throw new ArgumentException("Invalid principal plane");
        }

        /***************************************************/
    }
}
