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

        [Description("Perform a boolean intersection operation between two polylines using Clipper2.")]
        [Input("poly1", "The first polyline for intersection.")]
        [Input("poly2", "The second polyline for intersection.")]
        [Input("tolerance", "Tolerance for planarity checks and numerical precision. Default is Tolerance.Distance.")]
        [Output("result", "List of polylines representing the intersection of the two input polylines.")]
        public static List<Polyline> BooleanIntersection(this Polyline poly1, Polyline poly2, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3)
                return null;

            if (curvePlane == null)
            {
                curvePlane = poly1.FitPlane();
                if (poly1.ControlPoints.Any(x => !x.IsInPlane(curvePlane)) || poly2.ControlPoints.Any(x => !x.IsInPlane(curvePlane)))
                {
                    Base.Compute.RecordError("Clipper BooleanIntersection method only works for coplanar polylines.");
                    return null;
                }
            }

            // Find the orientation matrix to the global XY plane
            TransformMatrix orientation = poly1.OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return null;

            // Transform both polylines to the global XY plane.
            Polyline pLine1OnXY = poly1.Transform(orientation);
            Polyline pLine2OnXY = poly2.Transform(orientation);

            // Exclude the last point because Clipper2 expects the polygon to be implicitly closed (without identical start & end points)
            if (pLine1OnXY.IsClosed())
                pLine1OnXY.ControlPoints.RemoveAt(pLine1OnXY.ControlPoints.Count - 1);

            if (pLine2OnXY.IsClosed())
                pLine2OnXY.ControlPoints.RemoveAt(pLine2OnXY.ControlPoints.Count - 1);

            // Scale is set to run the intersection at a much larger scale for precision
            double scale = 1 / tolerance;
            Path64 subject = new Path64(pLine1OnXY.ControlPoints.Select(p => p.ToPoint64(scale)));
            Path64 clippers = new Path64(pLine2OnXY.ControlPoints.Select(p => p.ToPoint64(scale)));

            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(subject);
            clipper.AddClip(clippers);

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Intersection, FillRule.NonZero, solution);

            if (solution.Count == 0)
                return null;

            // Convert result back to 3D polylines
            List<Polyline> result = new List<Polyline>();
            TransformMatrix inverseTransform = orientation.Invert();

            foreach (Path64 path in solution)
            {
                List<Point> pointsOnXY = path.Select(x => BH.Engine.Geometry.Create.Point(x.X / scale, x.Y / scale)).ToList();
                List<Point> pointsOnCurvePlane = pointsOnXY.Select(x => x.Transform(inverseTransform)).ToList();
                result.Add(new Polyline { ControlPoints = pointsOnCurvePlane }.Close());
            }

            return result;
        }

        /***************************************************/
    }
}

