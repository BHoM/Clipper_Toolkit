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
using BH.oM.Geometry.CoordinateSystem;
using System.ComponentModel;
using System.Linq;

namespace BH.Engine.Geometry.Clipper
{
    public static partial class Modify
    {
        /***************************************************/
        /****               Public Methods              ****/
        /***************************************************/

        [Description("Finds a transform matrix that orients a plane to the global XY plane. The in-plane X axis follows the first non-degenerate segment of the polyline projected onto the plane, so geometry lying off the plane is mapped as if it was projected onto the plane along its normal.")]
        [Input("planarCurve", "A BHoM Polyline used to pick the in-plane X axis of the orientation.")]
        [Input("plane", "A BHoM Plane to orient to the global XY plane.")]
        [Input("tolerance", "Tolerance to be used for computations.")]
        [Output("orientationMatrix", "A BHoM TransformMatrix that orients the plane to the global XY plane.")]
        public static TransformMatrix OrientationToGlobalXY(this Polyline planarCurve, Plane plane, double tolerance)
        {
            double sqTol = tolerance * tolerance;
            Vector normal = plane.Normal.Normalise();

            Vector x = null;
            Point first = planarCurve.ControlPoints[0];
            foreach (Point second in planarCurve.ControlPoints.Skip(1))
            {
                Vector segment = second - first;
                Vector inPlane = segment - normal * segment.DotProduct(normal);
                if (inPlane.SquareLength() > sqTol)
                {
                    x = inPlane.Normalise();
                    break;
                }
            }

            if (x == null)
            {
                Base.Compute.RecordError("Polyline is degenerate (all points project onto a single point of the plane).");
                return null;
            }

            Vector y = normal.CrossProduct(x);
            Cartesian cs = new Cartesian(plane.Origin, x, y, normal);
            return cs.OrientationMatrix(new Cartesian());
        }

        /***************************************************/
    }
}

