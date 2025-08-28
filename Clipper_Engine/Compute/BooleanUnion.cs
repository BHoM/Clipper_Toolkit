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

        [Description("Perform a boolean union operation between multiple polylines using Clipper2.")]
        [Input("polylines", "List of polylines to union together.")]
        [Input("curvePlane", "Optional plane for the geometry. If null, will be fitted from the first polyline.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("result", "List of polylines representing the union of all input polylines.")]
        public static List<Polyline> BooleanUnion(this List<Polyline> polylines, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (polylines == null || polylines.Count == 0)
                return new List<Polyline>();

            // Filter out null or invalid polylines
            List<Polyline> validPolylines = polylines.Where(x => x != null && x.ControlPoints.Count >= 3).ToList();
            if (validPolylines.Count == 0)
                return new List<Polyline>();

            if (validPolylines.Count == 1)
                return new List<Polyline> { validPolylines[0] };

            if (curvePlane == null)
            {
                curvePlane = validPolylines[0].FitPlane();
                if (validPolylines.Any(x => x.ControlPoints.Any(y => !y.IsInPlane(curvePlane))))
                {
                    Base.Compute.RecordError("Clipper BooleanUnion method only works for coplanar polylines.");
                    return new List<Polyline>();
                }
            }

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = validPolylines[0].OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return new List<Polyline>();

            // Transform all polylines to the global XY plane
            List<Polyline> polylinesOnXY = validPolylines.Select(x => x.OpenPolylineOnXY(orientation)).ToList();

            // Scale is set to run the union at a much larger scale for precision
            double scale = 1 / tolerance;

            // Convert all polylines to Paths64
            Paths64 inputPaths = new Paths64();
            foreach (Polyline polyline in polylinesOnXY)
            {
                inputPaths.Add(polyline.ToClipPath(scale));
            }

            // Perform union operation
            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(inputPaths);

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Union, FillRule.NonZero, solution);

            // Convert result back to 3D polylines
            List<Polyline> result = solution.ToPolylines(orientation, scale);
            return result;
        }

        /***************************************************/
    }
}
