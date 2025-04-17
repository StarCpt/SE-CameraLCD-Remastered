using VRageMath;

namespace VRageRenderAccessor.VRageRender
{
    public class MyEnvironmentMatrices
    {
        public Vector3D CameraPosition;
        public Matrix ViewAt0;
        public Matrix InvViewAt0;
        public Matrix ViewProjectionAt0;
        public Matrix InvViewProjectionAt0;
        public Matrix Projection;
        public Matrix ProjectionForSkybox;
        public Matrix InvProjection;
        public MatrixD ViewD;
        public MatrixD InvViewD;
        public Matrix OriginalProjection;
        public Matrix OriginalProjectionFar;
        public MatrixD ViewProjectionD;
        public MatrixD InvViewProjectionD;
        public BoundingFrustumD ViewFrustumClippedD;
        public BoundingFrustumD ViewFrustumClippedFarD;
        public float NearClipping;
        public float LargeDistanceFarClipping;
        public float FarClipping;
        public float FovH;
        public float FovV;
        public bool LastUpdateWasSmooth;
    }
}
