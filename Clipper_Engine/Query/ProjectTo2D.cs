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

        [Description("Project a polyline to 2D coordinates on the specified principal plane for Clipper2 operations.")]
        [Input("pLine", "The polyline to project to 2D.")]
        [Input("plane", "The principal plane to project onto (XY, XZ, or YZ).")]
        [Input("scale", "Scale factor for coordinate precision. Default is 1e6.")]
        [Output("path", "Clipper2 Path64 representing the projected polyline.")]
        public static Path64 ProjectTo2D(this Polyline pLine, PrincipalPlane plane, double scale = 1e6)
        {
            List<Point> points = new List<Point>(pLine.ControlPoints);

            //Clipper requires polygons to be implicitly closed (last point not identical to the first).
            if (pLine.IsClosed())
                points.RemoveAt(0);

            return new Path64(points.Select(p => ProjectTo2D(p, plane)).Select(p => new Point64((long)(p.X * scale), (long)(p.Y * scale))));
        }

        /***************************************************/

        [Description("Project a point to 2D coordinates on the specified principal plane.")]
        [Input("point", "The point to project to 2D.")]
        [Input("plane", "The principal plane to project onto (XY, XZ, or YZ).")]
        [Input("scale", "Scale factor for coordinate precision. Default is 1e6.")]
        [Output("projectedPoint", "The projected 2D point.")]
        public static Point ProjectTo2D(this Point point, PrincipalPlane plane, double scale = 1e6)
        {
            if (plane == PrincipalPlane.XY)
                return BH.Engine.Geometry.Create.Point(point.X, point.Y);
            if (plane == PrincipalPlane.XZ)
                return BH.Engine.Geometry.Create.Point(point.X, point.Z);
            if (plane == PrincipalPlane.YZ)
                return BH.Engine.Geometry.Create.Point(point.Y, point.Z);

            throw new ArgumentException("Invalid principal plane");
        }

        /***************************************************/

        [Description("Project a polyline to a list of 2D Point64 coordinates on the specified principal plane for Clipper2 operations.")]
        [Input("pLine", "The polyline to project to 2D.")]
        [Input("plane", "The principal plane to project onto (XY, XZ, or YZ).")]
        [Input("scale", "Scale factor for coordinate precision. Default is 1e6.")]
        [Output("points", "List of Clipper2 Point64 representing the projected polyline points.")]
        public static List<Point64> ProjectTo2DAsPoints(this Polyline pLine, PrincipalPlane plane, double scale = 1e6)
        {
            List<Point> points = new List<Point>(pLine.ControlPoints);

            //Clipper requires polygons to be implicitly closed (last point not identical to the first).
            if (pLine.IsClosed())
                points.RemoveAt(0);

            List<Point64> points64 = points
                .Select(p => ProjectTo2D(p, plane))
                .Select(p => new Point64((long)(p.X * scale), (long)(p.Y * scale)))
                .ToList();

            return points64;
        }

        /***************************************************/
    }
}
