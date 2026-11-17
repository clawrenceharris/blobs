using Blobs.Content;
using UnityEngine;

namespace _Game.Scripts.Presentation
{
    public sealed class ColorSchemeProvider : MonoBehaviour
    {
        private LevelColorPaletteAsset _levelColorPalette;
        [SerializeField] private LevelColorPaletteAsset defaultLevelColorPalette;
        public LevelColorPaletteAsset LevelColorPalette => _levelColorPalette;

        public void Initialize(LevelColorPaletteAsset levelColorPalette)
        {
            _levelColorPalette = levelColorPalette;
        }

    }
}