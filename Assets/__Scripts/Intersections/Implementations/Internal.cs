using UnityEngine;

/// <summary>
///     A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    // (These vectors are moved outside of the Ray-Triangle intersection algorithm to keep runtime allocations at bay)
    private static Vector3 e1;
    private static Vector3 e2;
    private static Vector3 p;
    private static Vector3 q;

    private static Vector3 t;

    // Once we've determined that the ray intersects the bounding box of the collider,
    // we loop through all triangles until we find one that intersects the ray.
    // Doing things this way loses a little bit of speed, but increases accuracy on non-cube meshes.
    private static bool RaycastIndividual_Internal(IntersectionCollider collider, in Vector3 rayDirection,
        in Vector3 rayOrigin, out float distance)
    {
        var success = false;
        distance = 0;

        var worldToLocalMatrix = collider.transform.worldToLocalMatrix;

        // The triangles/vertices arrays are cached as to not allocate garbage every frame.
        var meshTriangles = collider.MeshTriangles;
        var meshVertices = collider.MeshVertices;

        // Transform rayDirection and rayOrigin into local space of the collider
        var localRayDirection = worldToLocalMatrix.FastMultiplyDirection(in rayDirection);
        var localRayOrigin = worldToLocalMatrix.FastMultiplyPoint3x4(in rayOrigin);

        for (var i = 0; i < meshTriangles.Length; i += 3)
        {
            // Calculate world-space positions of triangle vertices
            ref var vert1 = ref meshVertices[meshTriangles[i]];
            ref var vert2 = ref meshVertices[meshTriangles[i + 1]];
            ref var vert3 = ref meshVertices[meshTriangles[i + 2]];

            // If our ray intersects this triangle, the entire collider intersects, no more work to be done.
            if (RayTriangleIntersect(in vert1, in vert2, in vert3, in localRayDirection, in localRayOrigin,
                out var localDistance) && (!success || localDistance < distance))
            {
                success = true;
                distance = localDistance;
            }
        }

        // The ray did not intersect any triangles; the ray did not collide.
        return success;
    }

    // Fast Möller–Trumbore intersection algorithm
    // Variables passed by-reference to prevent copying
    private static bool RayTriangleIntersect(in Vector3 p1, in Vector3 p2, in Vector3 p3, in Vector3 rayDirection,
        in Vector3 rayOrigin, out float distance)
    {
        distance = 0;

        //Find vectors for two edges sharing vertex/point p1
        VectorUtils.FastSubtraction(ref e1, in p2, in p1);
        VectorUtils.FastSubtraction(ref e2, in p3, in p1);

        // calculating determinant 
        VectorUtils.FastCross(ref p, in rayDirection, in e2);

        //Calculate determinat
        var det = VectorUtils.FastDot(in e1, in p);

        //if determinant is near zero, ray lies in plane of triangle otherwise not
        if (det > -intersectionEpsilon && det < intersectionEpsilon) return false;

        var invDet = 1.0f / det;

        //calculate distance from p1 to ray origin
        VectorUtils.FastSubtraction(ref t, in rayOrigin, in p1);

        //Calculate u parameter
        var u = VectorUtils.FastDot(in t, in p) * invDet;

        //Check for ray hit
        if (u < 0 || u > 1) return false;

        //Prepare to test v parameter
        VectorUtils.FastCross(ref q, in t, in e1);

        //Calculate v parameter
        var v = VectorUtils.FastDot(in rayDirection, in q) * invDet;

        //Check for ray hit
        if (v < 0 || u + v > 1) return false;

        // If this dot product is within our epsilon, a hit is confirmed.
        if ((distance = VectorUtils.FastDot(in e2, in q) * invDet) > intersectionEpsilon) return true;

        // No hit at all
        return false;
    }
}
