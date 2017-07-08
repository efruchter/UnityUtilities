using System.Reflection;
using UnityEngine;

/// <summary>
/// Improved replacements for some GeometryUtility functions.
/// -Eric
/// </summary>
public static class GeometryUtilityNoAlloc
{
#if UNITY_2017 || UNITY_5_6 // These functions might not exist in future Unity versions. Check the API and redirect as necessary.
    private static System.Action<Plane[], Matrix4x4> _calculateFrustumPlanesImp;
    private static readonly Plane[] CachedPlanes = new Plane[6];
    private static bool _methodFetched = false;

    /// <summary>
    /// Call an internal version of CalculateFrustumPlanes with reflection to avoid pointless GC allocation.
    /// The default call in GeometryUtility will alloc a Plane[].
    /// </summary>
    /// <param name="camera"></param>
    /// <returns>The updated Plane[]</returns>
    public static Plane[] CalculateFrustumPlanes(Camera camera)
    {
        if (!_methodFetched)
        {
            MethodInfo meth = typeof(GeometryUtility).GetMethod("Internal_ExtractPlanes",
                BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(Plane[]), typeof(Matrix4x4) }, null);

            if (meth == null)
            {
                throw new System.Exception(
                    "Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");
            }

            _calculateFrustumPlanesImp =
                System.Delegate.CreateDelegate(typeof(System.Action<Plane[], Matrix4x4>), meth) as
                    System.Action<Plane[], Matrix4x4>;

            if (_calculateFrustumPlanesImp == null)
            {
                throw new System.Exception(
                    "Failed to reflect internal method. Your Unity version may not contain the presumed named method in GeometryUtility.");
            }

            _methodFetched = _calculateFrustumPlanesImp != null;
        }

        _calculateFrustumPlanesImp(CachedPlanes, camera.projectionMatrix * camera.worldToCameraMatrix);

        return CachedPlanes;
    }
#endif
}
