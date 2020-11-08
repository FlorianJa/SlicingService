using GcodeToMesh.MeshDecimator.Math;

namespace GcodeToMesh.MeshClasses
{
    public class MeshCreatorInput
    {
        public string meshname;
        public Vector3d[] newVertices;
        public Vector3[] newNormals;
        public Vector2[] newUV;
        public int[] newTriangles;
    }
}
