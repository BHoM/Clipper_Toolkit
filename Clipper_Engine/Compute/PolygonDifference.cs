using BH.Engine.Geometry;
using BH.oM.Geometry;
using BH.oM.Clipper;
using Clipper2Lib;
using System.Collections.Generic;

namespace BH.Engine.Clipper
{
    public static partial class Compute
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public static List<Polyline> PolygonDifference(this Polyline region, List<Polyline> refRegions, PrincipalPlane plane)
        {
            if (region == null || refRegions == null || region.ControlPoints.Count < 3)
                return null;

            if (refRegions.Count == 0)
                return new List<Polyline> { region };

            double fixedCoord = region.ControlPoints[0].FixedCoordinate(plane);

            // Convert main region to Path64
            Path64 subjectPath = region.ProjectTo2D(plane);

            // Convert reference regions to Paths64
            Paths64 clipPaths = new Paths64();
            foreach (Polyline refRegion in refRegions)
            {
                if (refRegion == null || refRegion.ControlPoints.Count < 3)
                    continue;

                Path64 clipPath = refRegion.ProjectTo2D(plane);
                clipPaths.Add(clipPath);
            }

            if (clipPaths.Count == 0)
                return new List<Polyline> { region };

            // Perform difference operation
            Clipper64 clipper = new Clipper64();
            clipper.AddSubject(subjectPath);
            clipper.AddClip(clipPaths);

            Paths64 solution = new Paths64();
            clipper.Execute(ClipType.Difference, FillRule.NonZero, solution);

            if (solution.Count == 0)
                return new List<Polyline>();

            // Convert result back to 3D polylines
            List<Polyline> result = new List<Polyline>();
            foreach (Path64 path in solution)
            {
                result.Add(path.UnprojectFrom2D(plane, fixedCoord));
            }

            return result;
        }

        /***************************************************/
    }
}
