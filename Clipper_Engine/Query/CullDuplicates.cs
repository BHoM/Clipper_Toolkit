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

        [Description("Removes duplicate polylines. Two polylines are duplicates when their control points are equal in the same order. Either list may be shifted, so a different starting point still counts as a duplicate. If plane is null, control points are compared directly. If plane is not null, they are projected onto the plane before comparison.")]
        [Input("polylines", "The collection of polylines to cull duplicates from.")]
        [Input("plane", "If null, control points are compared directly. If not null, control points are projected onto this plane before comparison.")]
        [Output("polylines", "The collection of polylines with duplicates removed.")]
        public static List<Polyline> CullDuplicates(this IEnumerable<Polyline> polylines, Plane plane = null)
        {
            if (polylines == null)
                return new List<Polyline>();

            List<Polyline> culled = new List<Polyline>();
            foreach (Polyline polyline in polylines)
            {
                if (polyline == null)
                    continue;

                if (!culled.Any(x => x.IsSame(polyline, plane)))
                    culled.Add(polyline);
            }

            return culled;
        }

        /***************************************************/
    }
}
