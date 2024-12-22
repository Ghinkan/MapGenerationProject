using UnityEngine;
namespace MapGenerationProject.DOTS
{
    public interface IHexSelectable
    {
        void ColorCell(HexCoordinates coordinates, Color color);
    }
}