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
using BH.oM.Clipper;
using BH.oM.Geometry;
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.Clipper
{
    public static partial class Compute
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Check if a polyline contains a list of points using Clipper2 for efficient point-in-polygon testing.")]
        [Input("outer", "The outer polyline to test against.")]
        [Input("points", "List of points to check if they are contained within the outer polyline.")]
        [Input("plane", "The principal plane to project the geometry onto for 2D operations.")]
        [Input("acceptOnEdge", "Whether to consider points exactly on the polygon edge as contained. Default is true.")]
        [Input("tolerance", "Tolerance for numerical precision. Default is Tolerance.Distance.")]
        [Output("contains", "True if all points are contained within the outer polyline, false otherwise.")]
        public static bool IsContaining(this Polyline outer, List<Point> points, PrincipalPlane plane, bool acceptOnEdge = true, double tolerance = Tolerance.Distance)
        {
            if (outer == null || points == null || outer.ControlPoints.Count < 3 || points.Count == 0)
                return false;

            if (plane == PrincipalPlane.Undefined)
                return outer.IsContaining(points);

            double scale = 1.0 / tolerance;
            Path64 outerPath = outer.ProjectTo2D(plane);

            foreach (Point p in points)
            {
                Point p2d = p.ProjectTo2D(plane);
                Point64 pt = p2d.ToPoint64(scale);

                PointInPolygonResult pip = Clipper2Lib.Clipper.PointInPolygon(pt, outerPath);

                if (pip == PointInPolygonResult.IsOutside) // Outside
                    return false;

                if (!acceptOnEdge && pip == PointInPolygonResult.IsOn) // On edge, and not accepting edge hits
                    return false;
            }

            return true;
        }

        /***************************************************/
    }
}
