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

            Plane plane = surface.FitPlane(tolerance);
            if (!ContainsOnBoundary(surface.ExternalBoundary, point, plane, true, tolerance))
                return false;

            if (surface.InternalBoundaries == null)
                return true;

            return !surface.InternalBoundaries.Any(x => x != null && ContainsOnBoundary(x, point, plane, false, tolerance));
        }

        /***************************************************/

        [Description("Checks if a point lies on any of the input planar surfaces.")]
        [Input("point", "Point to test.")]
        [Input("surfaces", "Planar surfaces to test against.")]
        [Input("tolerance", "Distance tolerance for the containment check.")]
        [Output("isOnFace", "True if the point lies on at least one surface.")]
        public static bool IsOnFace(this Point point, IEnumerable<PlanarSurface> surfaces, double tolerance = Tolerance.Distance)
        {
            if (point == null || surfaces == null)
                return false;

            return surfaces.Any(x => point.IsOnFace(x, tolerance));
        }

        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static bool ContainsOnBoundary(ICurve boundary, Point point, Plane plane, bool acceptOnEdge, double tolerance)
        {
            if (boundary is Polyline polyline)
            {
                if (plane == null)
                    return false;

                return polyline.IsContaining(point, plane, acceptOnEdge, tolerance);
            }

            return boundary.IIsContaining(new List<Point> { point }, acceptOnEdge, tolerance);
        }

        /***************************************************/
    }
}
