/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
        [Input("curvePlane", "Optional plane for the geometry. If null, will be fitted from the outer polyline.")]
        [Input("acceptOnEdge", "Whether to consider points exactly on the polygon edge as contained. Default is true.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("contains", "True if all points are contained within the outer polyline, false otherwise.")]
        public static bool IsContaining(this Polyline region, List<Point> points, Plane curvePlane = null, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (region == null || points == null || region.ControlPoints.Count < 3 || points.Count == 0)
                return false;

            if (curvePlane == null)
            {
                curvePlane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }
            }

            // Check if all points are coplanar with the outer polyline
            if (points.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                return false;

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = region.OrientationToGlobalXY(curvePlane, tolerance);
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

        [Description("Returns true if the reference polyline is fully contained within the region polyline. " +
            "Both polylines must be planar and coplanar. Optionally specify the plane and tolerance.")]
        [Input("region", "The outer polyline region to test for containment.")]
        [Input("refRegion", "The reference polyline to check if it is fully contained within the region.")]
        [Input("curvePlane", "Optional. The plane on which both polylines lie. If null, the plane is computed from the region polyline.")]
        [Input("tolerance", "Optional. The geometric tolerance for coplanarity and containment checks. Default is Tolerance.Distance.")]
        [Output("True if the reference polyline is fully contained within the region polyline; otherwise, false.")]
        public static bool IsContaining(this Polyline region, Polyline refRegion, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (region == null || refRegion == null || region.ControlPoints.Count < 3 || refRegion.ControlPoints.Count < 3)
                return false;

            if (curvePlane == null)
            {
                curvePlane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }
            }

            if (refRegion.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
            {
                Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                return false;
            }

            if (region.IsContaining(refRegion.ControlPoints, curvePlane, true, tolerance) == false)
            {
                return false;
            }

            //If refRegion sits fully inside the outer region, the boolean difference result should be empty
            return refRegion.BooleanDifference(new List<Polyline> { region }, curvePlane, tolerance)?.Count == 0;
        }

        /***************************************************/

        [Description("Check if a polyline contains a list of reference polylines using Clipper2 for efficient polygon-in-polygon testing.")]
        [Input("region", "The outer polyline to test against.")]
        [Input("refRegions", "List of polylines to check if they are contained within the outer polyline.")]
        [Input("curvePlane", "Optional plane for the geometry. If null, will be fitted from the outer polyline.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("contains", "True if all reference polylines are fully contained within the region polyline, false otherwise.")]
        public static bool IsContaining(this Polyline region, List<Polyline> refRegions, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (curvePlane == null)
            {
                curvePlane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper IsContaining method only works for planar polylines.");
                    return false;
                }
            }

            return refRegions.All(x => region.IsContaining(x, curvePlane, tolerance));
        }

        /***************************************************/
    }
}
