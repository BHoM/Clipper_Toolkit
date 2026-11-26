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

        [Description("Checks if a point is inside the outer boundary of a planar surface but not inside one of its holes. A point on the outer boundary or on the boundary of a hole counts as being on the face.")]
        [Input("point", "A point that is potentially on the planar surface.")]
        [Input("surface", "A planar surface with polyline boundaries that can have internal boundaries/holes.")]
        [Input("plane", "If null, the point needs to lie in the plane of the surface and within its boundaries. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("tolerance", "Distance tolerance for the planarity and containment checks.")]
        [Output("isOnFace", "True if the point is inside the outer boundary and not inside a hole.")]
        public static bool IsOnFace(this Point point, PlanarSurface surface, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (point == null || surface == null)
                return false;

            Polyline outer = surface.ExternalBoundary as Polyline;
            List<Polyline> inner = surface.InternalBoundaries?.Select(x => x as Polyline).ToList() ?? new List<Polyline>();
            if (outer == null || inner.Any(x => x == null))
            {
                Base.Compute.RecordError("Clipper IsOnFace method only works for planar surfaces with polyline boundaries.");
                return false;
            }

            return point.IsOnFace(outer, inner, plane, tolerance);
        }

        /***************************************************/

        [Description("Checks if a point is inside an outer region but not inside one of the inner regions (holes). A point on the outer boundary or on the boundary of a hole counts as being on the face.")]
        [Input("point", "A point that is potentially on the face.")]
        [Input("outer", "Closed polyline defining the outer boundary of the face.")]
        [Input("inner", "Closed polylines defining the holes of the face.")]
        [Input("plane", "If null, the point needs to lie in the plane of the outer region and within its boundaries. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("tolerance", "Distance tolerance for the planarity and containment checks.")]
        [Output("isOnFace", "True if the point is inside the outer region and not inside a hole.")]
        public static bool IsOnFace(this Point point, Polyline outer, List<Polyline> inner, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (point == null || outer == null)
                return false;

            if (plane == null)
            {
                plane = outer.FittedPlane(tolerance);
                if (plane == null || !point.IsInPlane(plane, tolerance))
                    return false;
            }

            if (!outer.IsContaining(point, plane, true, tolerance))
                return false;

            return inner == null || inner.All(x => x == null || !x.IsContaining(point, plane, false, tolerance));
        }

        /***************************************************/
    }
}
