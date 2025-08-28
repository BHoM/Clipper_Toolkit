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
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Convert
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Convert Clipper2 Paths64 back to BHoM Polylines with inverse transformation.")]
        [Input("solution", "The Clipper2 Paths64 solution to convert.")]
        [Input("orientation", "The transformation matrix used to orient the geometry.")]
        [Input("scale", "Scale factor for coordinate precision. Default is 1e6.")]
        [Output("polylines", "List of BHoM Polylines transformed back to the original coordinate system.")]
        public static List<Polyline> ToPolylines(this Paths64 solution, TransformMatrix orientation, double scale = 1e6)
        {
            if (solution.Count == 0)
                return new List<Polyline>();

            List<Polyline> result = new List<Polyline>();
            TransformMatrix inverseTransform = orientation.Invert();

            foreach (Path64 path in solution)
            {
                List<Point> pointsOnXY = path.Select(x => BH.Engine.Geometry.Create.Point(x.X / scale, x.Y / scale)).ToList();
                List<Point> pointsOnCurvePlane = pointsOnXY.Select(x => x.Transform(inverseTransform)).ToList();
                result.Add(new Polyline { ControlPoints = pointsOnCurvePlane }.Close());
            }

            return result;
        }

        /***************************************************/
    }
}
