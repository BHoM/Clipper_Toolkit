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

using BH.Engine.Geometry;
using BH.oM.Base.Attributes;
using BH.oM.Geometry;
using Clipper2Lib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
        public static Polyline OpenPolylineOnXY(this Polyline pLine, TransformMatrix orientation, double tolerance = Tolerance.Distance)
        {
            Polyline pLineOnXY = pLine.Transform(orientation);

            // Exclude the last point because Clipper2 expects the polygon to be implicitly closed (without identical start & end points)
            if (pLineOnXY.IsClosed(tolerance))
                pLineOnXY.ControlPoints.RemoveAt(pLineOnXY.ControlPoints.Count - 1);

            return pLineOnXY;
        }

        /***************************************************/
        /****              Internal methods             ****/
        /***************************************************/

        //Geometry oriented to the global XY plane, with its Z coordinate dropped, i.e. projected onto the plane the orientation was computed for.
        internal static Point FlattenedOnXY(this Point point, TransformMatrix orientation)
        {
            Point pointOnXY = point.Transform(orientation);
            pointOnXY.Z = 0;
            return pointOnXY;
        }

        /***************************************************/

        internal static Line FlattenedOnXY(this Line line, TransformMatrix orientation)
        {
            return new Line { Start = line.Start.FlattenedOnXY(orientation), End = line.End.FlattenedOnXY(orientation) };
        }

        /***************************************************/

        internal static Polyline FlattenedOnXY(this Polyline pLine, TransformMatrix orientation)
        {
            return new Polyline { ControlPoints = pLine.ControlPoints.Select(x => x.FlattenedOnXY(orientation)).ToList() };
        }

        /***************************************************/

        //Point-in-polygon check of flattened geometry, see FlattenedOnXY.
        internal static bool IsContainingOnXY(this Polyline closedRegionOnXY, IEnumerable<Point> pointsOnXY, bool acceptOnEdge, double tolerance)
        {
            double scale = 1 / tolerance;
            List<Point> regionPoints = closedRegionOnXY.ControlPoints;
            Path64 regionPath = new Path64(regionPoints.Take(regionPoints.Count - 1).Select(x => x.ToPoint64(scale)));

            foreach (Point point in pointsOnXY)
            {
                PointInPolygonResult checkResult = Clipper2Lib.Clipper.PointInPolygon(point.ToPoint64(scale), regionPath);
                if (checkResult == PointInPolygonResult.IsOutside)
                    return false;

                if (!acceptOnEdge && checkResult == PointInPolygonResult.IsOn)
                    return false;
            }

            return true;
        }

        /***************************************************/

        //Sorted parameters along the line where it meets the boundary of the region, including both ends of the line (0 and 1).
        internal static List<double> SplitParameters(this Polyline closedRegionOnXY, Line lineOnXY, double tolerance)
        {
            Vector lineVector = lineOnXY.End - lineOnXY.Start;
            double squareLength = lineVector.SquareLength();

            List<double> parameters = new List<double> { 0, 1 };
            if (squareLength == 0)
                return parameters;

            foreach (Line edge in closedRegionOnXY.SubParts())
            {
                foreach (Point crossPoint in edge.LineIntersections(lineOnXY, false, tolerance))
                {
                    double t = (crossPoint - lineOnXY.Start).DotProduct(lineVector) / squareLength;
                    if (t > 0 && t < 1)
                        parameters.Add(t);
                }
            }

            parameters.Sort();
            return parameters;
        }

        /***************************************************/

        //Midpoints of the pieces of the line between consecutive split parameters, skipping pieces shorter than the tolerance.
        internal static List<Point> PieceMidPoints(this Line line, List<double> parameters, double tolerance)
        {
            double length = line.Length();
            Vector lineVector = line.End - line.Start;

            List<Point> midPoints = new List<Point>();
            for (int i = 0; i < parameters.Count - 1; i++)
            {
                if ((parameters[i + 1] - parameters[i]) * length <= tolerance)
                    continue;

                midPoints.Add(line.Start + lineVector * ((parameters[i] + parameters[i + 1]) * 0.5));
            }

            return midPoints;
        }

        /***************************************************/

        //Both ends of the line and the midpoints of its pieces between consecutive split parameters.
        internal static List<Point> SamplePoints(this Line line, List<double> parameters, double tolerance)
        {
            List<Point> points = new List<Point> { line.Start, line.End };
            points.AddRange(line.PieceMidPoints(parameters, tolerance));
            return points;
        }

        /***************************************************/
    }
}
