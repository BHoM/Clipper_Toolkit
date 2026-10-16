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
        public static bool PolygonIntersects(this Polyline poly1, Polyline poly2, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3)
                return false;

            if (HasContainmentRelationship(poly1, poly2, tolerance))
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
        public static bool PolygonOverlaps(this Polyline pLine1, Polyline pLine2, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (pLine1 == null || pLine2 == null)
                return false;

            if (pLine1.ControlPoints.Count != pLine2.ControlPoints.Count)
                return false;

            if (plane == null)
                return HasSameControlPoints(pLine1, pLine2, tolerance);

            return HasSameControlPointsOnPlane(pLine1, pLine2, plane, tolerance);
        }

        /***************************************************/

        [Description("Checks if a polyline is self-intersecting, optionally after projecting it onto a plane.")]
        [Input("pLine", "Polyline to check.")]
        [Input("plane", "Optional plane for the check. If null, the polyline is tested in 3D.")]
        [Output("isSelfIntersecting", "True if self-intersecting.")]
        public static bool IsSelfIntersectingPolygon(this Polyline pLine, Plane plane = null)
        {
            if (pLine == null || pLine.ControlPoints.Count < 3)
                return false;

            if (plane == null)
                return pLine.IsSelfIntersecting();

            Polyline projected = pLine.Project(plane);
            return projected?.IsSelfIntersecting() ?? pLine.IsSelfIntersecting();
        }

        /***************************************************/

        [Description("Checks if two polylines have the same control-point set within tolerance.")]
        [Input("pLine1", "First polyline.")]
        [Input("pLine2", "Second polyline.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("same", "True if every control point of the second polyline matches a control point of the first.")]
        public static bool HasSameControlPoints(this Polyline pLine1, Polyline pLine2, double tolerance = Tolerance.Distance)
        {
            if (pLine1 == null || pLine2 == null)
                return false;

            if (pLine1.ControlPoints.Count != pLine2.ControlPoints.Count)
                return false;

            HashSet<Point> pointSet = new HashSet<Point>(pLine1.ControlPoints);
            return pLine2.ControlPoints.All(point => pointSet.Any(p => p.IsEqual(point, tolerance)));
        }

        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static bool HasContainmentRelationship(Polyline poly1, Polyline poly2, double tolerance)
        {
            double extent1 = poly1.Bounds().DiagonalLength();
            double extent2 = poly2.Bounds().DiagonalLength();

            if (extent1 > extent2 && poly1.IsContaining(poly2, true, tolerance))
                return true;

            if (extent2 > extent1 && poly2.IsContaining(poly1, true, tolerance))
                return true;

            return false;
        }

        /***************************************************/

        private static bool HasSameControlPointsOnPlane(Polyline pLine1, Polyline pLine2, Plane plane, double tolerance)
        {
            List<Point> points1 = pLine1.ControlPoints.Select(p => p.Project(plane)).ToList();
            List<Point> points2 = pLine2.ControlPoints.Select(p => p.Project(plane)).ToList();
            return HasSameControlPoints(new Polyline { ControlPoints = points1 }, new Polyline { ControlPoints = points2 }, tolerance);
        }

        /***************************************************/
    }
}
