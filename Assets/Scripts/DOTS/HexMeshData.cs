using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct HexMeshData
    {
        public NativeList<Vector3> Vertices;
        public NativeList<int> Triangles;
        public NativeList<Color> Colors;
        public TextureData TextureData;
        
        private Vector3 Perturb(Vector3 position)
        {
            Vector4 sample = HexMetrics.SampleNoise(position, TextureData);
            position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
            return position;
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            int baseIndex = Vertices.Length;

            Vertices.Add(Perturb(v1));
            Vertices.Add(Perturb(v2));
            Vertices.Add(Perturb(v3));

            // Triángulo: v1, v2, v3
            Triangles.Add(baseIndex);
            Triangles.Add(baseIndex + 1);
            Triangles.Add(baseIndex + 2);

            Colors.Add(color);
            Colors.Add(color);
            Colors.Add(color);
        }
        
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color1, Color color2, Color color3)
        {
            int baseIndex = Vertices.Length;

            Vertices.Add(Perturb(v1));
            Vertices.Add(Perturb(v2));
            Vertices.Add(Perturb(v3));

            // Triángulo: v1, v2, v3
            Triangles.Add(baseIndex);
            Triangles.Add(baseIndex + 1);
            Triangles.Add(baseIndex + 2);

            Colors.Add(color1);
            Colors.Add(color2);
            Colors.Add(color3);
        }
        
        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Color color1, Color color2, Color color3)
        {
            int baseIndex = Vertices.Length;
            
            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);

            // Triángulo: v1, v2, v3
            Triangles.Add(baseIndex);
            Triangles.Add(baseIndex + 1);
            Triangles.Add(baseIndex + 2);

            Colors.Add(color1);
            Colors.Add(color2);
            Colors.Add(color3);
        }
        
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2) 
        {
            int baseIndex = Vertices.Length;
            
            Vertices.Add(Perturb(v1));
            Vertices.Add(Perturb(v2));
            Vertices.Add(Perturb(v3));
            Vertices.Add(Perturb(v4));
                
            Colors.Add(c1);
            Colors.Add(c1);
            Colors.Add(c2);
            Colors.Add(c2);
            
            // Agregar los dos triángulos que forman el Quad
            // Primer triángulo: v1, v3, v2
            Triangles.Add(baseIndex);
            Triangles.Add(baseIndex + 2);
            Triangles.Add(baseIndex + 1);
            
            // Segundo triángulo: v2, v3, v4
            Triangles.Add(baseIndex + 1);
            Triangles.Add(baseIndex + 2);
            Triangles.Add(baseIndex + 3);
        }
        
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2, Color c3, Color c4) 
        {
            int baseIndex = Vertices.Length;
            
            Vertices.Add(Perturb(v1));
            Vertices.Add(Perturb(v2));
            Vertices.Add(Perturb(v3));
            Vertices.Add(Perturb(v4));
                
            Colors.Add(c1);
            Colors.Add(c2);
            Colors.Add(c3);
            Colors.Add(c4);
                
            // Agregar los dos triángulos que forman el Quad
            // Primer triángulo: v1, v3, v2
            Triangles.Add(baseIndex);
            Triangles.Add(baseIndex + 2);
            Triangles.Add(baseIndex + 1);
            
            // Segundo triángulo: v2, v3, v4
            Triangles.Add(baseIndex + 1);
            Triangles.Add(baseIndex + 2);
            Triangles.Add(baseIndex + 3);
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Triangles.Dispose();
            Colors.Dispose();
        }
    }
}