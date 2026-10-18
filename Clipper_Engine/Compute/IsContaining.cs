/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.Engine.Geometry;
using BH.Engine.Geometry.Clipper;
using BH.oM.Base.Attributes;
using BH.oM.Geometry;
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Compute
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Checks if all input points lie inside a polyline on the given plane, using Clipper2 point-in-polygon.")]
        [Input("outer", "Closed polyline region.")]
        [Input("points", "Points to test.")]
        [Input("curvePlane", "Work plane. Geometry is projected onto this plane before the Clipper test.")]
        [Input("acceptOnEdge", "If true, points on the boundary are accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if every point is inside the polyline.")]
        public static bool IsContaining(this Polyline outer, List<Point> points, Plane curvePlane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (outer == null || points == null || curvePlane == null || outer.ControlPoints.Count < 3)
                return false;

            if (points.Count == 0)
                return true;

            return AllPointsInPolygon(outer, points, curvePlane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Checks if a point lies inside a polyline on the given plane, using Clipper2 point-in-polygon.")]
        [Input("outer", "Closed polyline region.")]
        [Input("point", "Point to test.")]
        [Input("curvePlane", "Work plane. Geometry is projected onto this plane before the Clipper test.")]
        [Input("acceptOnEdge", "If true, a point on the boundary is accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if the point is inside the polyline.")]
        public static bool IsContaining(this Polyline outer, Point point, Plane curvePlane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (point == null)
                return false;

            return AllPointsInPolygon(outer, new List<Point> { point }, curvePlane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Checks if a polyline is contained in another polyline on the given plane, using Clipper2 point-in-polygon on the inner control points.")]
        [Input("outer", "Closed polyline region.")]
        [Input("inner", "Polyline to test.")]
        [Input("curvePlane", "Work plane. Geometry is projected onto this plane before the Clipper test.")]
        [Input("acceptOnEdge", "If true, inner vertices on the outer boundary are accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if every control point of the inner polyline is inside the outer polyline.")]
        public static bool IsContaining(this Polyline outer, Polyline inner, Plane curvePlane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (outer == null || inner == null || curvePlane == null)
                return false;

            return AllPointsInPolygon(outer, inner.ControlPoints, curvePlane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Checks if a line is contained in a polyline on the given plane, using Clipper2 point-in-polygon on the line start, end, and midpoint.")]
        [Input("outer", "Closed polyline region.")]
        [Input("line", "Line to test.")]
        [Input("curvePlane", "Work plane. Geometry is projected onto this plane before the Clipper test.")]
        [Input("acceptOnEdge", "If true, points on the boundary are accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if the line start, end, and midpoint are inside the polyline.")]
        public static bool IsContaining(this Polyline outer, Line line, Plane curvePlane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (outer == null || line == null || curvePlane == null)
                return false;

            Point mid = (line.Start + line.End) * 0.5;
            return AllPointsInPolygon(outer, new List<Point> { line.Start, line.End, mid }, curvePlane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Checks if every input point is contained by at least one polyline on the given plane.")]
        [Input("regions", "Closed polyline regions.")]
        [Input("points", "Points to test.")]
        [Input("curvePlane", "Work plane. Geometry is projected onto this plane before the Clipper test.")]
        [Input("acceptOnEdge", "If true, points on a boundary are accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if every point lies inside at least one region.")]
        public static bool IsContaining(this List<Polyline> regions, List<Point> points, Plane curvePlane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (regions == null || points == null || curvePlane == null)
                return false;

            List<Point> remainingPoints = new List<Point>(points);
            foreach (Polyline pLine in regions)
            {
                if (!remainingPoints.Any())
                    break;

                for (int i = remainingPoints.Count - 1; i >= 0; i--)
                {
                    if (pLine.IsContaining(remainingPoints[i], curvePlane, acceptOnEdge, tolerance))
                        remainingPoints.RemoveAt(i);
                }
            }

            return remainingPoints.Count == 0;
        }

        /***************************************************/

        [Description("Checks if the input point is contained by at least one polyline on the given plane.")]
        [Input("regions", "Closed polyline regions.")]
        [Input("point", "Point to test.")]
        [Input("curvePlane", "Work plane. Geometry is projected onto this plane before the Clipper test.")]
        [Input("acceptOnEdge", "If true, a point on a boundary is accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if the point lies inside at least one region.")]
        public static bool IsContaining(this List<Polyline> regions, Point point, Plane curvePlane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (regions == null || point == null || curvePlane == null)
                return false;

            return regions.Any(x => x.IsContaining(point, curvePlane, acceptOnEdge, tolerance));
        }

        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static bool AllPointsInPolygon(Polyline outer, IEnumerable<Point> points, Plane curvePlane, bool acceptOnEdge, double tolerance)
        {
            TransformMatrix orientation = outer.OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return false;

            Polyline outerOnXY = outer.OpenPolylineOnXY(orientation, tolerance);
            if (outerOnXY.ControlPoints.Count < 3)
                return false;

            double scale = 1 / tolerance;
            Path64 path = outerOnXY.ToClipPath(scale);

            foreach (Point point in points)
            {
                if (point == null)
                    return false;

                // A point farther than tolerance from the face is outside. Projection is only for the 2D test.
                if (!point.IsInPlane(curvePlane, tolerance))
                    return false;

                Point ptOnXY = point.Project(curvePlane).Transform(orientation);
                PointInPolygonResult result = Clipper2Lib.Clipper.PointInPolygon(ptOnXY.ToPoint64(scale), path);
                if (result == PointInPolygonResult.IsOutside)
                    return false;

                if (result == PointInPolygonResult.IsOn && !acceptOnEdge)
                    return false;
            }

            return true;
        }

        /***************************************************/
    }
}
