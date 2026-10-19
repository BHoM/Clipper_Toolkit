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

            //TODO: check if all outlines are polyline
            Polyline outer = surface.ExternalBoundary as Polyline;
            if (outer == null)
            {
                BH.Engine.Base.Compute.RecordError("Clipper IsOnFace method only works for planar surfaces with polyline boundaries.");
                return false;
            }

            List<Polyline> inner = surface.InternalBoundaries?.Select(x => x as Polyline).Where(x => x != null).ToList() ?? new List<Polyline>();
            if (inner.Count != surface.InternalBoundaries?.Count)
            {
                BH.Engine.Base.Compute.RecordError("Clipper IsOnFace method only works for planar surfaces with polyline boundaries.");
                return false;
            }

            Plane plane = surface.FitPlane(tolerance);
            return point.IsOnFace(outer, inner, plane, tolerance);

        }

        /***************************************************/

        //[Description("Checks if a point is inside the boundary of a planar surface but not inside one of its holes.")]
        //[Input("point", "A point that is potentially inside the boundary of a planar surface.")]
        //[Input("surface", "A planar surface that can have internal boundaries/holes.")]
        //[Input("tolerance", "Distance tolerance for the containment check.")]
        //[Output("isOnFace", "True if the point is inside the outer boundary and not inside a hole.")]
        public static bool IsOnFace(this Point point, Polyline outer, List<Polyline> inner, Plane curvePlane = null, double tolerance = Tolerance.Distance)
        {
            if (curvePlane == null)
            {
                curvePlane = outer?.FitPlane(tolerance);
                if (outer != null && (outer.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)) || inner.Any(y => y.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))))
                {
                    Base.Compute.RecordError("Clipper IsOnFace method only works for coplanar polylines.");
                    return false;
                }

                if (!point.IsInPlane(curvePlane, tolerance) == false)
                    return false;
            }

            return outer.IsContaining(point, curvePlane, true, tolerance) && !inner.Any(x => x != null && x.IsContaining(point, curvePlane, false, tolerance));
        }

        /***************************************************/
    }
}
