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
namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Checks if the areas enclosed by two closed polylines overlap, including the case of one polyline containing the other. Polylines touching only along their boundaries do not overlap.")]
        [Input("poly1", "First closed polyline.")]
        [Input("poly2", "Second closed polyline.")]
        [Input("plane", "If null, the plane is fitted from the first polyline and both polylines must lie on it. If not null, all geometry is projected onto this plane along its normal before the check.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("overlaps", "True if the areas enclosed by the polylines overlap.")]
        public static bool DoesOverlap(this Polyline poly1, Polyline poly2, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            //Open polylines don't enclose any area, so they can't overlap.
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3 || !poly1.IsClosed(tolerance) || !poly2.IsClosed(tolerance))
                return false;

            List<Polyline> intersection = Compute.BooleanIntersection(poly1, poly2, plane, tolerance);
            return intersection != null && intersection.Count > 0;
        }

        /***************************************************/
    }
}
