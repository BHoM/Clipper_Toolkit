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

        public static Path64 ProjectTo2D(this Polyline pLine, PrincipalPlane plane, double scale = 1e6)
        {
            List<Point> points = new List<Point>(pLine.ControlPoints);

            //Clipper requires polygons to be implicitly closed (last point not identical to the first).
            if (pLine.IsClosed())
                points.RemoveAt(0);

            return new Path64(points.Select(p => ProjectTo2D(p, plane)).Select(p => new Point64((long)(p.X * scale), (long)(p.Y * scale))));
        }

        /***************************************************/

        public static Point ProjectTo2D(this Point point, PrincipalPlane plane, double scale = 1e6)
        {
            if (plane == PrincipalPlane.XY)
                return BH.Engine.Geometry.Create.Point(point.X, point.Y);
            if (plane == PrincipalPlane.XZ)
                return BH.Engine.Geometry.Create.Point(point.X, point.Z);
            if (plane == PrincipalPlane.YZ)
                return BH.Engine.Geometry.Create.Point(point.Y, point.Z);

            throw new ArgumentException("Invalid principal plane");
        }

        /***************************************************/

        public static List<Point64> ProjectTo2DAsPoints(this Polyline pLine, PrincipalPlane plane, double scale = 1e6)
        {
            List<Point> points = new List<Point>(pLine.ControlPoints);

            //Clipper requires polygons to be implicitly closed (last point not identical to the first).
            if (pLine.IsClosed())
                points.RemoveAt(0);

            List<Point64> points64 = points
                .Select(p => ProjectTo2D(p, plane))
                .Select(p => new Point64((long)(p.X * scale), (long)(p.Y * scale)))
                .ToList();

            return points64;
        }

        /***************************************************/
    }
}
