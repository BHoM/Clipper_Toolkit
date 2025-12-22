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

        [Description("Perform a boolean intersection operation between two polylines using Clipper2.")]
        [Input("poly1", "The first polyline for intersection.")]
        [Input("poly2", "The second polyline for intersection.")]
        [Input("curvePlane", "Optional plane for the geometry. If null, will be fitted from the first polyline.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("result", "List of polylines representing the intersection of the two input polylines.")]
        public static List<Polyline> BooleanIntersection(this Polyline poly1, Polyline poly2, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3)
                return null;

            if (curvePlane == null)
            {
                curvePlane = poly1.FitPlane();
                if (poly1.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper BooleanIntersection method only works for coplanar polylines.");
                    return null;
                }
            }

            if (poly2.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
            {
                Base.Compute.RecordError("Clipper BooleanIntersection method only works for coplanar polylines.");
                return null;
            }

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = poly1.OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return null;

            // Transform both polylines to the global XY plane.
            Polyline pLine1OnXY = poly1.OpenPolylineOnXY(orientation);
            Polyline pLine2OnXY = poly2.OpenPolylineOnXY(orientation);

            // Scale is set to run the intersection at a much larger scale for precision
            double scale = 1 / tolerance;

            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(pLine1OnXY.ToClipPath(scale));
            clipper.AddClip(pLine2OnXY.ToClipPath(scale));

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Intersection, FillRule.NonZero, solution);

            // Convert result back to 3D polylines
            List<Polyline> result = solution.ToPolylines(orientation, scale);
            return result;
        }

        /***************************************************/
    }
}


