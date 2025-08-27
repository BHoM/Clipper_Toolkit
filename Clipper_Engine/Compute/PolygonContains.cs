using BH.Engine.Geometry;
using BH.oM.Clipper;
using BH.oM.Geometry;
using Clipper2Lib;
using System;
using System.Collections.Generic;

namespace BH.Engine.Clipper
{
    public static partial class Compute
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public static bool PolygonContains(this Polyline outer, List<Point> points, PrincipalPlane plane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (outer == null || points == null || outer.ControlPoints.Count < 3 || points.Count == 0)
                return false;

            if (plane == PrincipalPlane.Undefined)
                return outer.IsContaining(points);

            double scale = 1.0 / tolerance;
            Path64 outerPath = outer.ProjectTo2D(plane);

            foreach (Point p in points)
            {
                Point p2d = p.ProjectTo2D(plane);
                Point64 pt = p2d.ToPoint64(scale);

                PointInPolygonResult pip = Clipper2Lib.Clipper.PointInPolygon(pt, outerPath);

                if (pip == PointInPolygonResult.IsOutside) // Outside
                    return false;

                if (!acceptOnEdge && pip == PointInPolygonResult.IsOn) // On edge, and not accepting edge hits
                    return false;
            }

            return true;
        }

        /***************************************************/

        private static Point64 ToPoint64(this Point point, double scale)
        {
            return new Point64((long)Math.Round(point.X * scale), (long)Math.Round(point.Y * scale));
        }

        /***************************************************/
    }
}
