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
using BH.oM.Base.Attributes;
using BH.oM.Geometry;
using BH.oM.Clipper;
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;

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
        [Input("plane", "The principal plane to project the geometry onto for 2D operations.")]
        [Output("result", "List of polylines representing the difference (main region minus reference regions).")]
        public static List<Polyline> PolygonDifference(this Polyline region, List<Polyline> refRegions, PrincipalPlane plane)
        {
            if (region == null || refRegions == null || region.ControlPoints.Count < 3)
                return null;

            if (refRegions.Count == 0)
                return new List<Polyline> { region };

            double fixedCoord = region.ControlPoints[0].FixedCoordinate(plane);

            // Convert main region to Path64
            Path64 subjectPath = region.ProjectTo2D(plane);

            // Convert reference regions to Paths64
            Paths64 clipPaths = new Paths64();
            foreach (Polyline refRegion in refRegions)
            {
                if (refRegion == null || refRegion.ControlPoints.Count < 3)
                    continue;

                Path64 clipPath = refRegion.ProjectTo2D(plane);
                clipPaths.Add(clipPath);
            }

            if (clipPaths.Count == 0)
                return new List<Polyline> { region };

            // Perform difference operation
            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(subjectPath);
            clipper.AddClip(clipPaths);

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Difference, FillRule.NonZero, solution);

            if (solution.Count == 0)
                return new List<Polyline>();

            // Convert result back to 3D polylines
            List<Polyline> result = new List<Polyline>();
            foreach (Path64 path in solution)
            {
                result.Add(path.UnprojectFrom2D(plane, fixedCoord));
            }

            return result;
        }

        /***************************************************/
    }
}
