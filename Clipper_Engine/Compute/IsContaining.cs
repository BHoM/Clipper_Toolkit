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

        [Description("Check if a polyline contains a list of points using Clipper2 for efficient point-in-polygon testing.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("points", "List of points to check if they are contained within the outer polyline.")]
        [Input("plane", "If null, processing is in 2D (the plane is fitted from the outer polyline). If not null, 3D geometry is processed using this plane.")]
        [Input("acceptOnEdge", "Whether to consider points exactly on the polygon edge as contained.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision.")]
        [Output("contains", "True if all points are contained within the outer polyline, false otherwise.")]
        public static bool IsContaining(this Polyline region, List<Point> points, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || points == null || region.ControlPoints.Count < 3 || points.Count == 0)
                return false;

            if (plane == null)
            {
                plane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }

                if (points.Any(x => !x.IsInPlane(plane, tolerance)))
                    return false;
            }

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = region.OrientationToGlobalXY(plane, tolerance);
            if (orientation == null)
                return false;

            // Transform outer polyline to the global XY plane
            Polyline regionOnXY = region.OpenPolylineOnXY(orientation);

            // Scale is set to run the containment test at a much larger scale for precision
            double scale = 1 / tolerance;
            Path64 regionPath = regionOnXY.ToClipPath(scale);

            // Transform and test each point
            foreach (Point pnt in points)
            {
                Point64 checkPoint = pnt.Transform(orientation).ToPoint64(scale);
                PointInPolygonResult checkResult = Clipper2Lib.Clipper.PointInPolygon(checkPoint, regionPath);

                if (checkResult == PointInPolygonResult.IsOutside)
                    return false;

                // Check if point is on edge, but we don't accept edge hits
                if (!acceptOnEdge && checkResult == PointInPolygonResult.IsOn)
                    return false;
            }

            return true;
        }

        /***************************************************/

        [Description("Checks if a point lies inside a polyline on the given plane, using Clipper2 point-in-polygon.")]
        [Input("outer", "Closed polyline region.")]
        [Input("point", "Point to test.")]
        [Input("plane", "If not null, 3D geometry is projected onto this plane for the Clipper test (required for this overload).")]
        [Input("acceptOnEdge", "If true, a point on the boundary is accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if the point is inside the polyline.")]
        public static bool IsContaining(this Polyline outer, Point point, Plane plane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (point == null)
                return false;

            return outer.IsContaining(new List<Point> { point }, plane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Checks if a line is contained in a polyline on the given plane, using Clipper2 point-in-polygon on the line start, end, and midpoint.")]
        [Input("outer", "Closed polyline region.")]
        [Input("line", "Line to test.")]
        [Input("plane", "If not null, 3D geometry is projected onto this plane for the Clipper test (required for this overload).")]
        [Input("acceptOnEdge", "If true, points on the boundary are accepted.")]
        [Input("tolerance", "Distance tolerance used for orientation and scaling.")]
        [Output("isContaining", "True if the line start, end, and midpoint are inside the polyline.")]
        public static bool IsContaining(this Polyline outer, Line line, Plane plane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (outer == null || line == null || plane == null)
                return false;

            Point mid = (line.Start + line.End) * 0.5;
            return outer.IsContaining(new List<Point> { line.Start, line.End, mid }, plane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Returns true if the reference polyline is fully contained within the region polyline. " +
            "Both polylines must be planar and coplanar. Optionally specify the plane and tolerance.")]
        [Input("region", "The outer polyline region to test for containment.")]
        [Input("refRegion", "The reference polyline to check if it is fully contained within the region.")]
        [Input("plane", "If null, processing is in 2D (the plane is fitted from the region polyline). If not null, 3D geometry is processed using this plane.")]
        [Input("tolerance", "Optional. The geometric tolerance for coplanarity and containment checks. Default is Tolerance.Distance.")]
        [Output("True if the reference polyline is fully contained within the region polyline; otherwise, false.")]
        public static bool IsContaining(this Polyline region, Polyline refRegion, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (region == null || refRegion == null || region.ControlPoints.Count < 3 || refRegion.ControlPoints.Count < 3)
                return false;

            if (plane == null)
            {
                plane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }

                if (refRegion.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }
            }

            if (region.IsContaining(refRegion.ControlPoints, plane, true, tolerance) == false)
                return false;

            //If refRegion sits fully inside the outer region, the boolean difference result should be empty
            return refRegion.BooleanDifference(new List<Polyline> { region }, plane, tolerance)?.Count == 0;
        }

        /***************************************************/

        [Description("Check if a polyline contains a list of reference polylines using Clipper2 for efficient polygon-in-polygon testing.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("refRegions", "List of polylines to check if they are contained within the outer polyline.")]
        [Input("plane", "If null, processing is in 2D (the plane is fitted from the outer polyline). If not null, 3D geometry is processed using this plane.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("contains", "True if all reference polylines are fully contained within the region polyline, false otherwise.")]
        public static bool IsContaining(this Polyline region, List<Polyline> refRegions, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (plane == null)
            {
                plane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }
            }

            foreach (Polyline refRegion in refRegions)
            {
                if (refRegion.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }

                if (!region.IsContaining(refRegion, plane, tolerance))
                    return false;
            }

            return true;
        }

        /***************************************************/
    }
}

