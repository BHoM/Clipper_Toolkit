/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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
using System.ComponentModel;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Transform a polyline to the global XY plane and remove the last point for Clipper2 compatibility.")]
        [Input("pLine", "The polyline to transform and open.")]
        [Input("orientation", "The transformation matrix to apply.")]
        [Output("polyline", "The transformed polyline with the last point removed for Clipper2.")]
        public static Polyline OpenPolylineOnXY(this Polyline pLine, TransformMatrix orientation)
        {
            Polyline pLineOnXY = pLine.Transform(orientation);

            // Exclude the last point because Clipper2 expects the polygon to be implicitly closed (without identical start & end points)
            if (pLineOnXY.IsClosed())
                pLineOnXY.ControlPoints.RemoveAt(pLineOnXY.ControlPoints.Count - 1);

            return pLineOnXY;
        }

        /***************************************************/
    }
}
