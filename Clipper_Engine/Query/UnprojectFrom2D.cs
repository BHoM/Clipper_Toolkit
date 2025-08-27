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
using BH.oM.Clipper;
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Query
    {
        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        [Description("Unproject a Clipper2 Path64 back to a 3D polyline on the specified principal plane.")]
        [Input("path", "The Clipper2 Path64 to unproject.")]
        [Input("plane", "The principal plane to unproject from (XY, XZ, or YZ).")]
        [Input("fixedCoord", "The fixed coordinate value for the third dimension.")]
        [Input("scale", "Scale factor for coordinate precision. Default is 1e6.")]
        [Output("polyline", "The unprojected 3D polyline.")]
        public static Polyline UnprojectFrom2D(this Path64 path, PrincipalPlane plane, double fixedCoord, double scale = 1e6)
        {
            List<Point> points = path.Select(pt =>
            {
                Point projected2D = BH.Engine.Geometry.Create.Point(pt.X / scale, pt.Y / scale);
                return UnprojectFrom2D(projected2D, plane, fixedCoord);
            }).ToList();

            // BHoM requires polygons to be explicitly closed (last point identical to the first).
            return new Polyline { ControlPoints = points }.Close();
        }

        /***************************************************/

        [Description("Unproject a list of Clipper2 Point64 back to a 3D polyline on the specified principal plane.")]
        [Input("points64", "The list of Clipper2 Point64 to unproject.")]
        [Input("plane", "The principal plane to unproject from (XY, XZ, or YZ).")]
        [Input("fixedCoord", "The fixed coordinate value for the third dimension.")]
        [Input("scale", "Scale factor for coordinate precision. Default is 1e6.")]
        [Output("polyline", "The unprojected 3D polyline.")]
        public static Polyline UnprojectFrom2D(this List<Point64> points64, PrincipalPlane plane, double fixedCoord, double scale = 1e6)
        {
            List<Point> points = points64.Select(pt =>
            {
                Point projected2D = BH.Engine.Geometry.Create.Point(pt.X / scale, pt.Y / scale);
                return UnprojectFrom2D(projected2D, plane, fixedCoord);
            }).ToList();

            // BHoM requires polygons to be explicitly closed (last point identical to the first).
            return new Polyline { ControlPoints = points }.Close();
        }

        /***************************************************/

        [Description("Unproject a 2D point back to a 3D point on the specified principal plane.")]
        [Input("p", "The 2D point to unproject.")]
        [Input("plane", "The principal plane to unproject from (XY, XZ, or YZ).")]
        [Input("fixedCoord", "The fixed coordinate value for the third dimension.")]
        [Output("point", "The unprojected 3D point.")]
        public static Point UnprojectFrom2D(this Point p, PrincipalPlane plane, double fixedCoord)
        {
            if (plane == PrincipalPlane.XY)
                return new Point { X = p.X, Y = p.Y, Z = fixedCoord };
            if (plane == PrincipalPlane.XZ)
                return new Point { X = p.X, Y = fixedCoord, Z = p.Y };
            if (plane == PrincipalPlane.YZ)
                return new Point { X = fixedCoord, Y = p.X, Z = p.Y };

            throw new ArgumentException("Invalid principal plane");
        }

        /***************************************************/
    }
}
