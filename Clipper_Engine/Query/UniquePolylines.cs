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
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Filters polylines to return only unique ones, optionally checking for self-intersections and overlaps on a plane.")]
        [Input("polylines", "The collection of polylines to filter.")]
        [Input("clipPlane", "Optional plane for overlap and self-intersection detection.")]
        [Input("checkSelfIntersection", "Whether to exclude self-intersecting polylines (defaults to false).")]
        [Output("unique", "A list of unique non-overlapping polylines.")]
        public static List<Polyline> UniquePolylines(this IEnumerable<Polyline> polylines, Plane clipPlane = null, bool checkSelfIntersection = false)
        {
            if (polylines == null)
                return new List<Polyline>();

            List<Polyline> uniquePolylines = new List<Polyline>();
            foreach (Polyline polyline in polylines)
            {
                if (polyline == null)
                    continue;

                if (!uniquePolylines.Any(x => x.IsSame(polyline)))
                    uniquePolylines.Add(polyline);
            }

            return uniquePolylines;
        }

        /***************************************************/
    }
}
