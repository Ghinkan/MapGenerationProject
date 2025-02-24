using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MapGenerationProject.Base
{
    [System.Serializable]
    public struct HexCoordinates 
    { 
        private static StringBuilder _stringBuilder;
        
        public int X { get; private set; }
        public int Y { get { return -X - Z; } }
        public int Z { get; private set; }
        
        [ShowInInspector, ReadOnly] public Vector3 CoordinateVector { get { return new Vector3(X, Y, Z); } }
        
        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            _stringBuilder ??= new StringBuilder(32);
        }
        
        public HexCoordinates(int x, int z) 
        {
            X = x;
            Z = z;
        }
        
        public static HexCoordinates FromPosition(Vector3 position) 
        {
            // Dividir la posicion por el tamaño del hexagono
            float x = position.x / (HexMetrics.InnerRadius * 2f);
            float y = -x;
            float offset = position.z / (HexMetrics.OuterRadius * 3f);
            x -= offset;
            y -= offset;
        
            // Redondear las coordenadas
            int iX = Mathf.RoundToInt(x);
            int iY = Mathf.RoundToInt(y);
            int iZ = Mathf.RoundToInt(-x -y);
            
            // Ajustar si las coordenadas redondeadas no son validas
            if (iX + iY + iZ != 0) 
            {
                float dX = Mathf.Abs(x - iX);
                float dY = Mathf.Abs(y - iY);
                float dZ = Mathf.Abs(-x -y - iZ);
        
                // Correccion de la coordenada mas afectada
                if (dX > dY && dX > dZ) 
                {
                    iX = -iY - iZ;
                }
                else if (dZ > dY)
                {
                    iZ = -iX - iY;
                }
            }
        
            return new HexCoordinates(iX, iZ);
        }
        
        public static HexCoordinates FromOffsetCoordinates(int x, int z) 
        {
            return new HexCoordinates(x - z / 2, z);
        }
        
        public override string ToString() 
        {
            _stringBuilder.Clear();

            _stringBuilder.Append('(');
            _stringBuilder.Append(X);
            _stringBuilder.Append(", ");
            _stringBuilder.Append(Y);
            _stringBuilder.Append(", ");
            _stringBuilder.Append(Z);
            _stringBuilder.Append(')');

            return _stringBuilder.ToString();
        }

        public string ToStringOnSeparateLines() 
        {
            _stringBuilder.Clear();

            _stringBuilder.Append(X);
            _stringBuilder.Append('\n');
            _stringBuilder.Append(Y);
            _stringBuilder.Append('\n');
            _stringBuilder.Append(Z);

            return _stringBuilder.ToString();
        }
    }
}