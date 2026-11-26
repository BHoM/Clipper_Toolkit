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

        [Description("Check if a polyline contains a list of points using Clipper2 for efficient point-in-polygon testing.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("points", "List of points to check if they are contained within the outer polyline.")]
        [Input("plane", "If null, the plane is fitted from the region and points that do not lie on it are not contained. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("acceptOnEdge", "Whether to consider points exactly on the polygon edge as contained.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision.")]
        [Output("contains", "True if all points are contained within the outer polyline, false otherwise.")]
        [PreviousVersion("10.0", "BH.Engine.Clipper.Compute.IsContaining(BH.oM.Geometry.Polyline, System.Collections.Generic.List<BH.oM.Geometry.Point>, BH.oM.Geometry.Plane, System.Boolean, System.Double)")]
        public static bool IsContaining(this Polyline region, List<Point> points, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || points == null || region.ControlPoints.Count < 3 || points.Count == 0 || !region.IsClosed(tolerance))
                return false;

            if (plane == null)
            {
                plane = region.FittedPlane(tolerance);
                if (plane == null || points.Any(x => !x.IsInPlane(plane, tolerance)))
                    return false;
            }

            TransformMatrix orientation = region.OrientationToGlobalXY(plane, tolerance);
            if (orientation == null)
                return false;

            Polyline regionOnXY = region.FlattenedOnXY(orientation);
            return regionOnXY.IsContainingOnXY(points.Select(x => x.FlattenedOnXY(orientation)), acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Check if a polyline contains a point using Clipper2 for efficient point-in-polygon testing.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("point", "Point to check if it is contained within the outer polyline.")]
        [Input("plane", "If null, the plane is fitted from the region and a point that does not lie on it is not contained. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("acceptOnEdge", "Whether to consider a point exactly on the polygon edge as contained.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision.")]
        [Output("contains", "True if the point is contained within the outer polyline, false otherwise.")]
        public static bool IsContaining(this Polyline region, Point point, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (point == null)
                return false;

            return region.IsContaining(new List<Point> { point }, plane, acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Check if a polyline contains a whole line. The line is split where it meets the boundary of the region and each resulting piece is tested with Clipper2 point-in-polygon.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("line", "Line to check if it is contained within the outer polyline.")]
        [Input("plane", "If null, the plane is fitted from the region and a line that does not lie on it is not contained. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("acceptOnEdge", "Whether to consider a line touching or running along the polygon edge as contained.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision.")]
        [Output("contains", "True if the whole line is contained within the outer polyline, false otherwise.")]
        public static bool IsContaining(this Polyline region, Line line, Plane plane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || line == null || region.ControlPoints.Count < 3 || !region.IsClosed(tolerance))
                return false;

            if (plane == null)
            {
                plane = region.FittedPlane(tolerance);
                if (plane == null || !line.Start.IsInPlane(plane, tolerance) || !line.End.IsInPlane(plane, tolerance))
                    return false;
            }

            TransformMatrix orientation = region.OrientationToGlobalXY(plane, tolerance);
            if (orientation == null)
                return false;

            Polyline regionOnXY = region.FlattenedOnXY(orientation);
            Line lineOnXY = line.FlattenedOnXY(orientation);

            List<double> parameters = regionOnXY.SplitParameters(lineOnXY, tolerance);
            if (!acceptOnEdge && parameters.Count > 2)
                return false;

            return regionOnXY.IsContainingOnXY(lineOnXY.SamplePoints(parameters, tolerance), acceptOnEdge, tolerance);
        }

        /***************************************************/

        [Description("Returns true if the reference polyline is fully contained within the region polyline.")]
        [Input("region", "The outer polyline region to test for containment.")]
        [Input("refRegion", "The reference polyline to check if it is fully contained within the region.")]
        [Input("plane", "If null, the plane is fitted from the region and both polylines must lie on it. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("tolerance", "The geometric tolerance for coplanarity and containment checks.")]
        [Input("acceptOnEdge", "If false, the reference polyline must lie strictly inside the region: any of its vertices on the boundary of the region makes the result false.")]
        [Output("contains", "True if the reference polyline is fully contained within the region polyline; otherwise, false.")]
        [PreviousVersion("10.0", "BH.Engine.Clipper.Compute.IsContaining(BH.oM.Geometry.Polyline, BH.oM.Geometry.Polyline, BH.oM.Geometry.Plane, System.Double)")]
        public static bool IsContaining(this Polyline region, Polyline refRegion, Plane plane = null, double tolerance = Tolerance.Distance, bool acceptOnEdge = true)
        {
            if (region == null || refRegion == null || region.ControlPoints.Count < 3 || refRegion.ControlPoints.Count < 3)
                return false;

            if (!region.IsClosed(tolerance) || !refRegion.IsClosed(tolerance))
                return false;

            if (plane == null)
            {
                plane = region.FittedPlane(tolerance);
                if (plane == null)
                    return false;

                if (refRegion.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for coplanar polylines.");
                    return false;
                }
            }

            if (!region.IsContaining(refRegion.ControlPoints, plane, acceptOnEdge, tolerance))
                return false;

            //If refRegion sits fully inside the outer region, the boolean difference result should be empty
            return refRegion.BooleanDifference(new List<Polyline> { region }, plane, tolerance)?.Count == 0;
        }

        /***************************************************/

        [Description("Check if a polyline contains a list of reference polylines using Clipper2 for efficient polygon-in-polygon testing.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("refRegions", "List of polylines to check if they are contained within the outer polyline.")]
        [Input("plane", "If null, the plane is fitted from the region and all polylines must lie on it. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision.")]
        [Output("contains", "True if all reference polylines are fully contained within the region polyline, false otherwise.")]
        [PreviousVersion("10.0", "BH.Engine.Clipper.Compute.IsContaining(BH.oM.Geometry.Polyline, System.Collections.Generic.List<BH.oM.Geometry.Polyline>, BH.oM.Geometry.Plane, System.Double)")]
        public static bool IsContaining(this Polyline region, List<Polyline> refRegions, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (region == null || refRegions == null)
                return false;

            if (plane == null)
            {
                plane = region.FittedPlane(tolerance);
                if (plane == null)
                    return false;
            }

            return refRegions.All(x => region.IsContaining(x, plane, tolerance));
        }

        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static Plane FittedPlane(this Polyline region, double tolerance)
        {
            Plane plane = region.FitPlane(tolerance);
            if (plane == null || region.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
            {
                Base.Compute.RecordError("No plane was supplied and the region is not planar, so the Clipper method can't be computed.");
                return null;
            }

            return plane;
        }

        /***************************************************/
    }
}
