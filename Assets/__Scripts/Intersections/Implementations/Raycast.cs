using System;
using UnityEngine;

/// <summary>
///     A custom class that offers a super-fast way of checking intersections against a ray without using Physics.Raycast.
/// </summary>
public static partial class Intersections
{
    /// <summary>
    ///     Cast a ray against all custom colliders, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, out IntersectionHit hit) => Raycast(ray, -1, out hit, out _);

    /// <summary>
    ///     Cast a ray against all custom colliders, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">
    ///     The ray to cast against all colliders.<</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <param name="distance">
    ///     If the ray intersects, this is the distance from the ray origin to the intersection point,
    ///     and <see cref="float.PositiveInfinity" /> otherwise.
    /// </param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, out IntersectionHit hit, out float distance) =>
        Raycast(ray, -1, out hit, out distance);

    /// <summary>
    ///     Cast a ray against all custom colliders in the provided layer, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="layer">GameObject layer to raycast against. An input of <c>-1</c> will cast against all layers.</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, int layer, out IntersectionHit hit) => Raycast(ray, layer, out hit, out _);

    /// <summary>
    ///     Cast a ray against all custom colliders in the provided layer, and returns the closest intersecting collider.
    /// </summary>
    /// <param name="ray">The ray to cast against all colliders.</param>
    /// <param name="layer">GameObject layer to raycast against. An input of <c>-1</c> will cast against all layers.</param>
    /// <param name="hit">Information on the object that was hit.</param>
    /// <param name="distance">
    ///     If the ray intersects, this is the distance from the ray origin to the intersection point,
    ///     and <see cref="float.PositiveInfinity" /> otherwise.
    /// </param>
    /// <returns>Returns <c>true</c> if the ray successfully intersected a collider, and <c>false</c> if not.</returns>
    public static bool Raycast(Ray ray, int layer, out IntersectionHit hit, out float distance)
    {
        hit = new IntersectionHit();
        distance = float.PositiveInfinity;
        var foundAny = false;

        var rayDirection = ray.direction;
        var rayOrigin = ray.origin;
        var localRayOrigin = rayOrigin;
        var localRayDirection = rayDirection;

        var layerMin = layer == -1 ? 0 : layer;
        var layerMax = layer == -1 ? 32 : layer + 1;

        for (var currentLayer = layerMin; currentLayer < layerMax; currentLayer++)
        {
            var groupedCollidersInLayer = groupedColliders[currentLayer];

            if (groupedCollidersInLayer.Count <= 0) continue;

            var groupKeys = groupedCollidersInLayer.Keys;
            int lowestKey = int.MaxValue, highestKey = int.MinValue;
            foreach (var groupKey in groupKeys)
            {
                lowestKey = Math.Min(lowestKey, groupKey);
                highestKey = Math.Max(highestKey, groupKey);
            }

            var groupID = Mathf.Clamp(CurrentGroup, lowestKey, highestKey);
            var rounds = (Math.Max(groupID - lowestKey, highestKey - groupID) * 2) + 1;

            for (var k = 0; k < rounds; k++)
                //while (groupedCollidersInLayer.TryGetValue(startingGroup, out var collidersInLayer))
            {
                if (groupID < lowestKey || groupID > highestKey)
                {
                    groupID = NextGroupSearchFunction(groupID);
                    continue;
                }

                if (groupedCollidersInLayer.TryGetValue(groupID, out var collidersInLayer)
                    && collidersInLayer.Count > 0)
                {
                    var count = collidersInLayer.Count;

                    for (var i = 0; i < count; i++)
                    {
                        var collider = collidersInLayer[i];

                        if (layer == -1 || collider.CollisionLayer == layer)
                        {
                            var worldToLocalMatrix = collider.transform.worldToLocalMatrix;
                            worldToLocalMatrix.FastMultiplyPoint3x4(in rayOrigin, ref localRayOrigin);
                            worldToLocalMatrix.FastMultiplyDirection(in rayDirection, ref localRayDirection);

                            var localRay = new Ray(localRayOrigin, localRayDirection);

                            var bounds = collider.CollisionBounds;

                            // The first pass checks if the ray even intersects the bounds of the collider.
                            // If not, the collider is considered unnecessary, and no further work is done on it.
                            // See the RaycastIndividual_Internal method for more information on the second pass.
                            if (bounds.IntersectRay(localRay)
                                && RaycastIndividual_Internal(
                                    collider,
                                    in localRayDirection,
                                    in localRayOrigin,
                                    out var dist))
                            {
                                foundAny = true;

                                // If we reach this point, the ray has successfully intersected the collider.
                                // We then check if it's the closest one so far.
                                if (dist < distance)
                                {
                                    distance = dist;
                                    hit = new IntersectionHit(collider.gameObject, bounds, ray, dist);
                                }
                            }
                        }
                    }
                }

                groupID = NextGroupSearchFunction(groupID);
            }
        }

        return foundAny;
    }
}
