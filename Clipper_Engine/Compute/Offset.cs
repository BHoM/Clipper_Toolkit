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
using BH.Engine.Geometry.Clipper;
using BH.oM.Base.Attributes;
using BH.oM.Geometry;
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Clipper
{
    public static partial class Compute
    {
        /***************************************************/
        /****               Public Methods              ****/
        /***************************************************/

        [Description("Offset a curve by the given distance (using Clipper http://www.angusj.com/delphi/clipper.php). Method only works for closed, planar polylines.")]
        [Input("polyline", "A BHoM Polyline representing the curve to offset.")]
        [Input("distance", "The distance by which to offset the curve (-Ve is inwards).")]
        [Input("curvePlane", "Optional plane for the geometry. If null, will be fitted from the polyline.")]
        [Input("tolerance", "Tolerance to be used for planarity and closedness checks as well as for offset computations.")]
        [Output("polylines", "Input polylines after offset.")]
        public static List<Polyline> Offset(this Polyline polyline, double distance = 0, Plane curvePlane = null, double tolerance = Tolerance.Distance)
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

            if (curvePlane == null)
            {
                curvePlane = polyline.FitPlane();
                if (polyline.ControlPoints.Any(x => !x.IsInPlane(curvePlane, tolerance)))
                {
                    Base.Compute.RecordError("Clipper Offset method only works for planar polylines.");
                    return null;
                }
            }

            // Find orientation matrix to the global XY plane
            TransformMatrix orientation = polyline.OrientationToGlobalXY(curvePlane, tolerance);
            if (orientation == null)
                return null;

            // Transform polyline to XY plane and prepare for Clipper2
            Polyline polylineOnXY = polyline.OpenPolylineOnXY(orientation);

            // Scale is set to run the offset at a much larger scale for precision
            double scale = 1 / tolerance;

            // Convert to Path64 and simplify
            Path64 inputPath = polylineOnXY.ToClipPath(scale);
            inputPath = Clipper2Lib.Clipper.SimplifyPath(inputPath, 1);

            // Perform offset operation
            Paths64 offsetPaths = new Paths64();
            ClipperOffset offsetter = new ClipperOffset();
            offsetter.AddPath(inputPath, JoinType.Miter, EndType.Polygon);
            offsetter.Execute(distance * scale, offsetPaths);
            offsetPaths = Clipper2Lib.Clipper.SimplifyPaths(offsetPaths, 1);

            // Convert result back to 3D polylines
            List<Polyline> result = offsetPaths.ToPolylines(orientation, scale);
            return result;
        }

        /***************************************************/
    }
}
