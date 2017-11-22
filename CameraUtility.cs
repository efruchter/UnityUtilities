public static class CameraUtility
{
  // Generate a projection matrix such that the near clip plane is aligned with an arbitrary transform.
  protected static Matrix4x4 GetObliqueMatrix(this Camera inPortalCamera, Transform inClipPlane)
  {
      Vector4 clipPlaneWorldSpace = new Vector4(inClipPlane.forward.x, inClipPlane.forward.y, inClipPlane.forward.z, Vector3.Dot(inClipPlane.position, -inClipPlane.forward));
      Vector4 clipPlaneCameraSpace = Matrix4x4.Transpose(Matrix4x4.Inverse(inPortalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;
      return inPortalCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
  }
}
