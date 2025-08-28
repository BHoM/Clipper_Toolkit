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

using BH.oM.Base.Attributes;
using BH.oM.Clipper;
using BH.oM.Geometry;
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

        [Description("Perform a boolean intersection operation between two polylines using Clipper2.")]
        [Input("poly1", "The first polyline for intersection.")]
        [Input("poly2", "The second polyline for intersection.")]
        [Input("plane", "The principal plane to project the geometry onto for 2D operations.")]
        [Output("result", "List of polylines representing the intersection of the two input polylines.")]
        public static List<Polyline> BooleanIntersection(this Polyline poly1, Polyline poly2, PrincipalPlane plane)
        {
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3)
                return null;

            double fixedCoord = poly1.ControlPoints[0].FixedCoordinate(plane);

            Path64 subjPaths = poly1.ProjectTo2D(plane);
            Path64 clipPaths = poly2.ProjectTo2D(plane);

            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(subjPaths);
            clipper.AddClip(clipPaths);

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Intersection, FillRule.NonZero, solution);

            if (solution.Count == 0)
                return null;

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
