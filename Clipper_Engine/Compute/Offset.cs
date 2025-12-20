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
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Geometry.Clipper
{
    public static partial class Compute
    {
        /***************************************************/
        /****               Public Methods              ****/
        /***************************************************/

        [Description("Offset a curve by the given distance (using Clipper http://www.angusj.com/delphi/clipper.php). Method only works for closed, planar polylines.")]
        [Input("polyline", "A BHoM Polyline representing the curve to offset.")]
        [Input("distance", "The distance by which to offset the curve (-Ve is inwards).")]
        [Input("tolerance", "Tolerance to be used for planarity and closedness checks as well as for offset computations.")]
        [Output("polylines", "Input polylines after offset.")]
        public static List<Polyline> Offset(this Polyline polyline, double distance = 0, double tolerance = Tolerance.Distance)
        {
            if (polyline == null)
                return null;

            if (distance == 0)
                return new List<Polyline> { polyline };

            if (!polyline.IsClosed(tolerance))
            {
                Base.Compute.RecordError("Clipper Offset method only works for closed polylines (polygons).");
                return null;
            }

            // Fit plane and check if the polyline is planar
            Plane curvePlane = polyline.FitPlane();
            if (polyline.ControlPoints.Any((Point x) => Math.Abs(curvePlane.Normal.DotProduct(x - curvePlane.Origin)) > tolerance))
            {
                Base.Compute.RecordError("Clipper Offset method only works for planar polylines.");
                return null;
            }

            // Find orientation matrix to the global XY plane
            TransformMatrix orientation = polyline.OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return null;

            // Exclude the last point because Clipper2 expects the polygon to be implicitly closed (without the last point identical to the first)
            List<Point> controlPoints = polyline.ControlPoints.Take(polyline.ControlPoints.Count - 1).Select(x => x.Transform(orientation)).ToList();

            // Scale is set to run the offset at a much larger scale, and then rescale back to original coordinates. This smooths the offset curve by making the offset more detailed.
            double scale = 1 / tolerance;

            Path64 inputPath = new Path64(controlPoints.Select(p => new Point64((long)(p.X * scale), (long)(p.Y * scale))));
            inputPath = Clipper2Lib.Clipper.SimplifyPath(inputPath, 1);

            Paths64 offsetPaths = new Paths64();
            ClipperOffset offsetter = new ClipperOffset();
            offsetter.AddPath(inputPath, JoinType.Miter, EndType.Polygon);
            offsetter.Execute(distance * scale, offsetPaths);
            offsetPaths = Clipper2Lib.Clipper.SimplifyPaths(offsetPaths, 1);

            List<Polyline> offsetCurves = new List<Polyline>();
            TransformMatrix inverseTransform = orientation.Invert();

            // Convert paths to polylines transformed back to the original plane
            foreach (Path64 path in offsetPaths)
            {
                List<Point> pointsOnXY = path.Select(x => BH.Engine.Geometry.Create.Point(x.X / scale, x.Y / scale)).ToList();
                List<Point> pointsOnCurvePlane = pointsOnXY.Select(x => x.Transform(inverseTransform)).ToList();
                offsetCurves.Add(new Polyline { ControlPoints = pointsOnCurvePlane }.Close());
            }

            return offsetCurves;
        }

        /***************************************************/
    }
}

