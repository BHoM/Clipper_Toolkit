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

        [Description("Perform a boolean difference operation between a main region and a list of reference regions using Clipper2. Reference regions that do not lie on the subject plane are skipped, and the subject is kept.")]
        [Input("region", "The main polyline region to subtract from.")]
        [Input("refRegions", "List of reference polylines to subtract from the main region. Regions off the subject plane are ignored.")]
        [Input("plane", "If null, processing is in 2D (the plane is fitted from the main region). If not null, 3D geometry is processed using this plane.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("result", "List of polylines representing the difference (main region minus reference regions).")]
        public static List<Polyline> BooleanDifference(this Polyline region, List<Polyline> refRegions, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (region == null || refRegions == null || region.ControlPoints.Count < 3)
                return null;

            List<Polyline> clipRegions = refRegions.Where(x => x != null && x.ControlPoints.Count >= 3).ToList();
            if (clipRegions.Count == 0)
                return new List<Polyline> { region };

            if (plane == null)
            {
                plane = region.FitPlane();
                if (region.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper BooleanDifference method only works for coplanar polylines.");
                    return null;
                }

                // An opening on another plane must not discard the subject. Subtract only the clips that lie on this plane.
                clipRegions = clipRegions.Where(x => x.ControlPoints.All(y => y != null && y.IsInPlane(plane, tolerance))).ToList();
                if (clipRegions.Count == 0)
                    return new List<Polyline> { region };
            }

            if (!region.IsClosed(tolerance) || clipRegions.Any(x => !x.IsClosed(tolerance)))
            {
                Base.Compute.RecordError("Boolean Difference works on closed regions.");
                return new List<Polyline> { region };
            }

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = region.OrientationToGlobalXY(plane, tolerance);
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

