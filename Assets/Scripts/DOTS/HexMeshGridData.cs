using Unity.Collections;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public struct HexMeshGridData
    {
        [ReadOnly] public TextureData TextureData;
        
        [WriteOnly] private NativeList<Vector3>.ParallelWriter _verticesWriter;
        [WriteOnly] private NativeList<int>.ParallelWriter _trianglesWriter;
        [WriteOnly] private NativeList<Color>.ParallelWriter _colorsWriter;
        
        //TODO:Probar con NativeStream para resize
        // private NativeStream _verticesStream;
        // private NativeStream _trianglesStream;
        // private NativeStream _colorsStream;
        
        public HexMeshGridData(NativeList<Vector3> vertices, NativeList<int> triangles, NativeList<Color> colors,TextureData textureData)
        {
            TextureData = textureData;
            
            _verticesWriter = vertices.AsParallelWriter();
            _trianglesWriter = triangles.AsParallelWriter();
            _colorsWriter = colors.AsParallelWriter();
        }
        
        private Vector3 Perturb(Vector3 position)
        {
            Vector4 sample = HexMetrics.SampleNoise(position, TextureData);
            position.x += (sample.x * 2f - 1f) * HexMetrics.CellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.CellPerturbStrength;
            return position;
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color)
        {
            int i0 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v1));
            int i1 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v2));
            int i2 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v3));

            // Triángulo: v1, v2, v3
            UnsafeHelper.Add(ref _trianglesWriter, i0);
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            UnsafeHelper.Add(ref _trianglesWriter, i2);

            UnsafeHelper.Add(ref _colorsWriter, color);
            UnsafeHelper.Add(ref _colorsWriter, color);
            UnsafeHelper.Add(ref _colorsWriter, color);
        }
        
        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3, Color color1, Color color2, Color color3)
        {
            int i0 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v1));
            int i1 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v2));
            int i2 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v3));

            // Triángulo: v1, v2, v3
            UnsafeHelper.Add(ref _trianglesWriter, i0);
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            UnsafeHelper.Add(ref _trianglesWriter, i2);
            
            UnsafeHelper.Add(ref _colorsWriter, color1);
            UnsafeHelper.Add(ref _colorsWriter, color2);
            UnsafeHelper.Add(ref _colorsWriter, color3);
        }
        
        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Color color1, Color color2, Color color3)
        {
            int i0 = UnsafeHelper.AddWithIndex(ref _verticesWriter, v1);
            int i1 = UnsafeHelper.AddWithIndex(ref _verticesWriter, v2);
            int i2 = UnsafeHelper.AddWithIndex(ref _verticesWriter, v3);

            // Triángulo: v1, v2, v3
            UnsafeHelper.Add(ref _trianglesWriter, i0);
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            UnsafeHelper.Add(ref _trianglesWriter, i2);
            
            UnsafeHelper.Add(ref _colorsWriter, color1);
            UnsafeHelper.Add(ref _colorsWriter, color2);
            UnsafeHelper.Add(ref _colorsWriter, color3);
        }
        
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2) 
        {
            int i0 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v1));
            int i1 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v2));
            int i2 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v3));
            int i3 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v4));
            
            UnsafeHelper.Add(ref _colorsWriter, c1);
            UnsafeHelper.Add(ref _colorsWriter, c1);
            UnsafeHelper.Add(ref _colorsWriter, c2);
            UnsafeHelper.Add(ref _colorsWriter, c2);
            
            // Agregar los dos triángulos que forman el Quad
            // Primer triángulo: v1, v3, v2
            UnsafeHelper.Add(ref _trianglesWriter, i0);
            UnsafeHelper.Add(ref _trianglesWriter, i2);
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            
            // Segundo triángulo: v2, v3, v4
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            UnsafeHelper.Add(ref _trianglesWriter, i2);
            UnsafeHelper.Add(ref _trianglesWriter, i3);
        }
        
        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Color c1, Color c2, Color c3, Color c4) 
        {
            int i0 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v1));
            int i1 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v2));
            int i2 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v3));
            int i3 = UnsafeHelper.AddWithIndex(ref _verticesWriter, Perturb(v4));
                
            UnsafeHelper.Add(ref _colorsWriter, c1);
            UnsafeHelper.Add(ref _colorsWriter, c2);
            UnsafeHelper.Add(ref _colorsWriter, c3);
            UnsafeHelper.Add(ref _colorsWriter, c4);
                
            // Agregar los dos triángulos que forman el Quad
            // Primer triángulo: v1, v3, v2
            UnsafeHelper.Add(ref _trianglesWriter, i0);
            UnsafeHelper.Add(ref _trianglesWriter, i2);
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            
            // Segundo triángulo: v2, v3, v4
            UnsafeHelper.Add(ref _trianglesWriter, i1);
            UnsafeHelper.Add(ref _trianglesWriter, i2);
            UnsafeHelper.Add(ref _trianglesWriter, i3);
        }
    }
}