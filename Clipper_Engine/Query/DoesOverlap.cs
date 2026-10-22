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

        [Description("Checks if two polylines intersect on a plane without one fully containing the other.")]
        [Input("poly1", "First polyline.")]
        [Input("poly2", "Second polyline.")]
        [Input("plane", "If null, processing is in 2D (the plane is fitted from the first polyline). If not null, 3D geometry is processed using this plane.")]
        [Input("tolerance", "Distance tolerance.")]
        [Output("intersects", "True if the polylines intersect.")]
        public static bool DoesOverlap(this Polyline poly1, Polyline poly2, Plane plane = null, double tolerance = Tolerance.Distance)
        {
            if (poly1 == null || poly2 == null || poly1.ControlPoints.Count < 3 || poly2.ControlPoints.Count < 3)
                return false;

            List<Polyline> intersection = Compute.BooleanIntersection(poly1, poly2, plane, tolerance);
            return intersection != null && intersection.Count > 0;
        }

        /***************************************************/
    }
}
