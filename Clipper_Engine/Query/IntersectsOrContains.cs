/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
 */

using BH.Engine.Geometry;
using BH.oM.Base.Attributes;
using BH.oM.Geometry;
using System;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("True if a line crosses a closed polyline region or lies fully inside it.")]
        [Input("region", "Closed polyline region.")]
        [Input("line", "Line to test.")]
        [Input("plane", "If null, the plane is fitted from the region when required by Clipper.")]
        [Input("acceptOnEdge", "If false, points and segments on the boundary are not treated as inside.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("intersectsOrContains", "True if the line intersects or is contained in the region.")]
        public static bool IntersectsOrContains(this Polyline region, Line line, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || line == null)
                return false;

            tolerance = Math.Abs(tolerance);

            if (region.ControlPoints == null || region.ControlPoints.Count < 3 || !region.IsClosed(tolerance))
                return false;

            if (Compute.IsContaining(region, line.Start, plane, acceptOnEdge, tolerance)
                || Compute.IsContaining(region, line.End, plane, acceptOnEdge, tolerance))
                return true;

            foreach (Line edge in region.SubParts())
            {
                if (edge.LineIntersections(line, false, tolerance)?.Count > 0)
                    return true;
            }

            return false;
        }

        /***************************************************/

        [Description("True if a polyline crosses a closed region, lies inside it, or fully contains the region.")]
        [Input("region", "Closed polyline region.")]
        [Input("polyline", "Line or polyline boundary to test (open or closed).")]
        [Input("plane", "If null, the plane is fitted from the region when required by Clipper.")]
        [Input("acceptOnEdge", "Passed through to line segment tests.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("intersectsOrContains", "True if the curves intersect or one contains the other.")]
        public static bool IntersectsOrContains(this Polyline region, Polyline polyline, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || polyline == null)
                return false;

            tolerance = Math.Abs(tolerance);

            if (polyline.ControlPoints == null || polyline.ControlPoints.Count < 2)
                return false;

            if (polyline.ControlPoints.Count >= 3 && polyline.IsClosed(tolerance)
                && Compute.IsContaining(region, polyline, plane, tolerance))
                return true;

            if (region.ControlPoints.Count >= 3 && region.IsClosed(tolerance)
                && polyline.ControlPoints.Count >= 3 && polyline.IsClosed(tolerance)
                && Compute.IsContaining(polyline, region, plane, tolerance))
                return true;

            foreach (Line segment in polyline.SubParts())
            {
                if (region.IntersectsOrContains(segment, plane, acceptOnEdge, tolerance))
                    return true;
            }

            return false;
        }

        /***************************************************/
    }
}
