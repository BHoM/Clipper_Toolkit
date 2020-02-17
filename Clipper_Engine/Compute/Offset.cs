/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2019, the respective contributors. All rights reserved.
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

using BH.oM.Geometry;
using BH.oM.Reflection.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

namespace BH.Engine.Geometry.Offset
{
    public static partial class Compute
    {
        /***************************************************/
        /****      public Methods                       ****/
        /***************************************************/

        [Description("Offset a curve by the given distance (using Clipper http://www.angusj.com/delphi/clipper.php)")]
        [Input("curve", "A BHoM Polyline representing the curve to offset")]
        [Input("distance", "The distance by which to offset the curve (-Ve is inwards)")]
        [Output("curve", "A list of BHoM Polylines")]
        public static List<Polyline> Offset(this Polyline curve, double distance = 0)
        {
            if (distance == 0) { return new List<Polyline> { curve }; }

            // Transform Polyline to XY plane at Z = 0
            Vector zVector = BH.Engine.Geometry.Create.Vector(0, 0, 1);
            Plane curvePlane = curve.IFitPlane();
            Vector curvePlaneNormal = curvePlane.Normal;
            List<Point> vertices = curve.IDiscontinuityPoints();

            Point referencePoint = vertices.Min();
            Point xyReferencePoint = BH.Engine.Geometry.Create.Point(referencePoint.X, referencePoint.Y, 0);
            Vector translateVector = xyReferencePoint - referencePoint;

            Vector rotationVector = curvePlaneNormal.CrossProduct(zVector).Normalise();
            double rotationAngle = curvePlaneNormal.Angle(zVector);
            TransformMatrix transformMatrix = BH.Engine.Geometry.Create.RotationMatrix(vertices.Min(), rotationVector, rotationAngle);

            Polyline transformedCurve = curve.Transform(transformMatrix).Translate(translateVector);

            double scale = 1024.0; // Scale is set to run the offset at a much larger scale, and then rescale back to original coordinates. This smooths the offset curve by making the offset more detailed.

            // Convert transformed Polyline into geometry suitable for offsetting using Clipper
            List<ClipperLib.IntPoint> path = new List<ClipperLib.IntPoint>();
            foreach (Point pt in transformedCurve.IDiscontinuityPoints())
            {
                path.Add(new ClipperLib.IntPoint((long)(pt.X * scale), (long)(pt.Y * scale)));
            }
            List<List<ClipperLib.IntPoint>> paths = new List<List<ClipperLib.IntPoint>> { path };

            // Offset curve
            List<List<ClipperLib.IntPoint>> offsetPaths = ClipperLib.Clipper.OffsetPolygons(paths, distance * scale, ClipperLib.JoinType.jtMiter);

            // Convert offset curve/s back to BHoM geometry
            List<Polyline> transformedOffsetCurves = new List<Polyline>();
            foreach (List<ClipperLib.IntPoint> pth in offsetPaths)
            {
                List<Point> offsetPts = new List<Point>();
                foreach (ClipperLib.IntPoint ppt in pth)
                {
                    offsetPts.Add(BH.Engine.Geometry.Create.Point(ppt.X / scale, ppt.Y / scale, 0));
                }
                offsetPts.Add(BH.Engine.Geometry.Create.Point(pth[0].X / scale, pth[0].Y / scale, 0));
                transformedOffsetCurves.Add(BH.Engine.Geometry.Create.Polyline(offsetPts));
            }

            // Convert transformed offset line/s back to original plane
            List<Polyline> offsetCurves = new List<Polyline>();
            foreach (Polyline transformedOffsetCurve in transformedOffsetCurves)
            {
                offsetCurves.Add(transformedOffsetCurve.Translate(translateVector.Reverse()).Transform(transformMatrix.Invert()));
            }

            return offsetCurves;
        }
    }
}
