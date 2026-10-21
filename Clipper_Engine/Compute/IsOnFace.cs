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
    public static partial class Compute
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Checks if a point is inside the boundary of a planar surface but not inside one of its holes.")]
        [Input("point", "A point that is potentially inside the boundary of a planar surface.")]
        [Input("surface", "A planar surface that can have internal boundaries/holes.")]
        [Input("tolerance", "Distance tolerance for the containment check.")]
        [Output("isOnFace", "True if the point is inside the outer boundary and not inside a hole.")]
        public static bool IsOnFace(this Point point, PlanarSurface surface, double tolerance = Tolerance.Distance)
        {
            if (point == null || surface?.ExternalBoundary == null)
                return false;

            if (!TryGetPolylineBoundaries(surface, out Polyline outer, out List<Polyline> inner))
            {
                BH.Engine.Base.Compute.RecordError("Clipper IsOnFace method only works for planar surfaces with polyline boundaries.");
                return false;
            }

            Plane plane = surface.FitPlane(tolerance);
            if (!point.IsInPlane(plane, tolerance))
                return false;

            return point.IsOnFace(outer, inner, plane, tolerance);

        }

        /***************************************************/

        //[Description("Checks if a point is inside the boundary of a planar surface but not inside one of its holes.")]
        //[Input("point", "A point that is potentially inside the boundary of a planar surface.")]
        //[Input("surface", "A planar surface that can have internal boundaries/holes.")]
        //[Input("tolerance", "Distance tolerance for the containment check.")]
        //[Output("isOnFace", "True if the point is inside the outer boundary and not inside a hole.")]
        public static bool IsOnFace(this Point point, Polyline outer, List<Polyline> inner, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (point == null || outer == null)
                return false;

            if (plane == null)
            {
                plane = outer.FitPlane(tolerance);
                if (outer.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance)) || inner.Any(y => y != null && y.ControlPoints.Any(x => !x.IsInPlane(plane, tolerance))))
                {
                    Base.Compute.RecordError("Clipper IsOnFace method only works for coplanar polylines.");
                    return false;
                }

                if (!point.IsInPlane(plane, tolerance))
                    return false;
            }

            return outer.IsContaining(point, plane, true, tolerance) && !inner.Any(x => x != null && x.IsContaining(point, plane, false, tolerance));
        }

        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static bool TryGetPolylineBoundaries(PlanarSurface surface, out Polyline outer, out List<Polyline> inner)
        {
            outer = null;
            inner = new List<Polyline>();

            Polyline externalPolyline = surface.ExternalBoundary as Polyline;
            if (externalPolyline == null)
                return false;

            outer = externalPolyline;

            if (surface.InternalBoundaries == null)
                return true;

            foreach (ICurve boundary in surface.InternalBoundaries)
            {
                Polyline innerPolyline = boundary as Polyline;
                if (innerPolyline == null)
                    return false;

                inner.Add(innerPolyline);
            }

            return true;
        }

        /***************************************************/
    }
}
