using System;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        private static StringBuilder _stringBuilder;
        
        public readonly int X;
        public readonly int Z;
        public readonly int Y => -X - Z;

        public Vector3 CoordinateVector => new Vector3(X, Y, Z);

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
        
        public static HexCoordinates FromPosition(float3 position) 
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
                float dX = math.abs(x - iX);
                float dY = math.abs(y - iY);
                float dZ = math.abs(-x -y - iZ);
        
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
        
        public readonly HexCoordinates Step(HexDirection direction)
        {
            return direction switch 
            {
                HexDirection.NE => new HexCoordinates(X, Z + 1),
                HexDirection.E => new HexCoordinates(X + 1, Z),
                HexDirection.SE => new HexCoordinates(X + 1, Z - 1),
                HexDirection.SW => new HexCoordinates(X, Z - 1),
                HexDirection.W => new HexCoordinates(X - 1, Z),
                _ => new HexCoordinates(X - 1, Z + 1),
            };
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

        public readonly string ToStringOnSeparateLines() 
        {
            _stringBuilder.Clear();

            _stringBuilder.Append(X);
            _stringBuilder.Append('\n');
            _stringBuilder.Append(Y);
            _stringBuilder.Append('\n');
            _stringBuilder.Append(Z);

            return _stringBuilder.ToString();
        }
        
        public bool Equals(HexCoordinates other)
        {
            return X == other.X && Z == other.Z;
        }
        
        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }
    }
}