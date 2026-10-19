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

        [Description("Checks if two polylines intersect on a plane without one fully containing the other.")]
        [Input("poly1", "First polyline.")]
        [Input("poly2", "Second polyline.")]
        [Input("plane", "Optional plane for the Clipper boolean intersection. If null, the plane is fitted from the first polyline.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("intersects", "True if the polylines intersect.")]
        public static bool DoesIntersect(this Polyline poly1, Polyline poly2, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3)
                return false;

            List<Polyline> intersection = Compute.BooleanIntersection(poly1, poly2, plane, tolerance);
            return intersection != null && intersection.Count > 0;
        }

        /***************************************************/

        [Description("Checks if two polylines have the same control points, optionally after projecting both onto a plane.")]
        [Input("pLine1", "First polyline.")]
        [Input("pLine2", "Second polyline.")]
        [Input("plane", "Optional plane for comparison. If null, control points are compared in 3D.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("overlaps", "True if the polylines overlap.")]
        public static bool IsSame(this Polyline pLine1, Polyline pLine2, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (pLine1 == null || pLine2 == null)
                return false;

            if (pLine1.ControlPoints.Count != pLine2.ControlPoints.Count)
                return false;

            List<Point> points1 = pLine1.ControlPoints;
            List<Point> points2 = pLine2.ControlPoints;

            if (plane != null)
            {
                points1 = points1.Select(p => p.Project(plane)).ToList();
                points2 = points2.Select(p => p.Project(plane)).ToList();
            }

            //TODO: in points2, find all indices of points1[0]
            //TODO: for each index, check if the sequence of points matches points1, considering wrap-around and tolerance
        }

        /***************************************************/
    }
}
