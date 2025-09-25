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

        [Description("Perform a boolean difference operation between a main region and a list of reference regions using Clipper2.")]
        [Input("region", "The main polyline region to subtract from.")]
        [Input("refRegions", "List of reference polylines to subtract from the main region.")]
        [Input("curvePlane", "Optional plane for the geometry. If null, will be fitted from the main region.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("result", "List of polylines representing the difference (main region minus reference regions).")]
        public static List<Polyline> BooleanDifference(this Polyline region, List<Polyline> refRegions, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (region == null || refRegions == null || region.ControlPoints.Count < 3)
                return null;

            List<Polyline> clipRegions = refRegions.Where(x => x != null && x.ControlPoints.Count >= 3).ToList();
            if (clipRegions.Count == 0)
                return new List<Polyline> { region };

            if (curvePlane == null)
            {
                curvePlane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper BooleanDifference method only works for coplanar polylines.");
                    return null;
                }
            }

            if (clipRegions.Any(x => x.ControlPoints.Any(y => !y.IsInPlane(curvePlane, tolerance))))
            {
                Base.Compute.RecordError("Clipper BooleanDifference method only works for coplanar polylines.");
                return null;
            }

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = region.OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return null;

            // Transform main region to the global XY plane
            Polyline regionOnXY = region.OpenPolylineOnXY(orientation);

            // Scale is set to run the difference at a much larger scale for precision
            double scale = 1 / tolerance;

            // Convert reference regions to Paths64
            Paths64 clipPaths = new Paths64();
            foreach (Polyline refRegion in clipRegions)
            {
                Polyline refRegionOnXY = refRegion.OpenPolylineOnXY(orientation);
                clipPaths.Add(refRegionOnXY.ToClipPath(scale));
            }

            // Perform difference operation
            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(regionOnXY.ToClipPath(scale));
            clipper.AddClip(clipPaths);

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Difference, FillRule.NonZero, solution);

            // Convert result back to 3D polylines
            List<Polyline> result = solution.ToPolylines(orientation, scale);
            return result;
        }

        /***************************************************/
    }
}
