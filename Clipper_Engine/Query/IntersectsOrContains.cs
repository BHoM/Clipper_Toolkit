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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Checks if a line crosses the boundary of a closed polyline region or lies inside it.")]
        [Input("region", "Closed polyline region.")]
        [Input("line", "Line to test.")]
        [Input("plane", "If null, the plane is fitted from the region and a line that does not lie on it is ignored. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("acceptOnEdge", "If false, contact with the boundary is ignored and only a line passing through the interior of the region counts.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("intersectsOrContains", "True if the line intersects or is contained in the region.")]
        public static bool IntersectsOrContains(this Polyline region, Line line, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || line == null)
                return false;

            return region.IntersectsOrContains(new List<Line> { line }, plane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Checks if a polyline crosses the boundary of a closed polyline region or lies inside it. A region lying inside the polyline does not count.")]
        [Input("region", "Closed polyline region.")]
        [Input("polyline", "Open or closed polyline to test.")]
        [Input("plane", "If null, the plane is fitted from the region and a polyline that does not lie on it is ignored. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("acceptOnEdge", "If false, contact with the boundary is ignored and only a polyline passing through the interior of the region counts.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("intersectsOrContains", "True if the polyline intersects or is contained in the region.")]
        public static bool IntersectsOrContains(this Polyline region, Polyline polyline, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || polyline?.ControlPoints == null || polyline.ControlPoints.Count < 2)
                return false;

            return region.IntersectsOrContains(polyline.SubParts(), plane, acceptOnEdge, tolerance);
        }

        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static bool IntersectsOrContains(this Polyline region, List<Line> lines, Plane plane, bool acceptOnEdge, double tolerance)
        {
            if (region.ControlPoints == null || region.ControlPoints.Count < 3 || !region.IsClosed(tolerance))
                return false;

            if (plane == null)
            {
                plane = region.FitPlane(tolerance);
                if (plane == null || region.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("No plane was supplied and the region is not planar, so the Clipper method can't be computed.");
                    return false;
                }

                if (lines.Any(x => !x.Start.IsInPlane(plane, tolerance) || !x.End.IsInPlane(plane, tolerance)))
                    return false;
            }

            TransformMatrix orientation = region.OrientationToGlobalXY(plane, tolerance);
            if (orientation == null)
                return false;

            Polyline regionOnXY = region.FlattenedOnXY(orientation);
            List<Line> edgesOnXY = regionOnXY.SubParts();
            foreach (Line line in lines)
            {
                Line lineOnXY = line.FlattenedOnXY(orientation);
                if (acceptOnEdge)
                {
                    //Any contact with the boundary counts. Without it, the line is either fully inside or fully outside the region.
                    if (edgesOnXY.Any(x => x.LineIntersections(lineOnXY, false, tolerance).Count != 0))
                        return true;

                    if (regionOnXY.IsContainingOnXY(new List<Point> { lineOnXY.Start }, true, tolerance))
                        return true;
                }
                else
                {
                    //Only a piece of the line passing through the interior of the region counts.
                    List<double> parameters = regionOnXY.SplitParameters(lineOnXY, tolerance);
                    if (lineOnXY.PieceMidPoints(parameters, tolerance).Any(x => regionOnXY.IsContainingOnXY(new List<Point> { x }, false, tolerance)))
                        return true;
                }
            }

            return false;
        }

        /***************************************************/
    }
}
