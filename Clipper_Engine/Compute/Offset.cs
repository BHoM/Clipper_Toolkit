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

using BH.oM.Base.Attributes;
using BH.oM.Geometry;
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Geometry.Offset
{
    public static partial class Compute
    {
        /***************************************************/
        /****      public Methods                       ****/
        /***************************************************/

        [Description("Offset a curve by the given distance (using Clipper http://www.angusj.com/delphi/clipper.php). Method only works for closed, planar polylines.")]
        [Input("polyline", "A BHoM Polyline representing the curve to offset")]
        [Input("distance", "The distance by which to offset the curve (-Ve is inwards)")]
        [Input("tolerance", "Tolerance to be used for planarity and closedness checks.")]
        [Output("polylines", "A list of BHoM Polylines")]
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

            if (!polyline.IsPlanar(tolerance))
            {
                Base.Compute.RecordError("Clipper Offset method only works for planar polylines.");
                return null;
            }

            Point referencePoint = polyline.IDiscontinuityPoints().Min();
            Point refPointOnXY = BH.Engine.Geometry.Create.Point(referencePoint.X, referencePoint.Y, 0);
            Vector translateVector = refPointOnXY - referencePoint;
            TransformMatrix translation = BH.Engine.Geometry.Create.TranslationMatrix(translateVector);

            Vector zVector = Vector.ZAxis;
            Plane curvePlane = polyline.IFitPlane();
            Vector curveNormal = curvePlane.Normal;

            double rotationAngle = curveNormal.Angle(zVector);
            Vector rotationVector = curveNormal.CrossProduct(zVector).Normalise();
            TransformMatrix rotation = BH.Engine.Geometry.Create.RotationMatrix(referencePoint, rotationVector, rotationAngle);

            // Transform Polyline to the XY plane at Z = 0
            TransformMatrix totalTransform = translation * rotation;
            Polyline transformedCurve = polyline.Transform(totalTransform);

            //Exclude the last point because Clipper2 expects the polygon to be implicitly closed (without the last point identical to the first)
            List<Point> controlPoints = transformedCurve.ControlPoints.Take(transformedCurve.ControlPoints.Count - 1).ToList();

            // Scale is set to run the offset at a much larger scale, and then rescale back to original coordinates. This smooths the offset curve by making the offset more detailed.
            double scale = 1024.0;

            Path64 inputPath = new Path64(controlPoints.Select(p => new Point64((long)(p.X * scale), (long)(p.Y * scale))));

            Paths64 offsetPaths = new Paths64();
            ClipperOffset offsetter = new ClipperOffset();
            offsetter.AddPath(inputPath, JoinType.Miter, EndType.Polygon);
            offsetter.Execute(distance * scale, offsetPaths);

            List<Polyline> offsetCurves = new List<Polyline>();
            TransformMatrix inverseTransform = totalTransform.Invert();

            // Convert paths to polylines transformed back to the original plane
            foreach (Path64 path in offsetPaths)
            {
                List<Point> pointsOnXY = path.Select(x => BH.Engine.Geometry.Create.Point(x.X / scale, x.Y / scale)).ToList();
                List<Point> pointsOnCurvePlane = pointsOnXY.Select(x => x.Transform(inverseTransform)).ToList();
                offsetCurves.Add(new Polyline { ControlPoints = pointsOnCurvePlane }.Close());
            }

            return offsetCurves;
        }
    }
}





