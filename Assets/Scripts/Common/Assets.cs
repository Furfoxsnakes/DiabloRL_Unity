using System.Collections.Generic;
using Enums;

namespace Common
{
    public class Assets
    {
        public SpriteSheetAsset TerrainSprites = new SpriteSheetAsset();
        public SpriteSheetAsset CharacterSprites = new SpriteSheetAsset();
        public SpriteSheetAsset FontSprites = new SpriteSheetAsset();
        public FontAsset GameFont;

        public Assets()
        {
            TerrainSprites.Load("Sprites/Terrain/TerrainSprites", SpriteSheetAsset.SheetType.SpritePack);
            TerrainSprites.grid = new SpriteGrid(new Vector2i(C.TILE_SIZE,C.TILE_SIZE));
            RB.MapLayerSpriteSheetSet((int)MapLayers.TERRAIN, TerrainSprites);

            CharacterSprites.Load("Sprites/Characters/CharacterSprites", SpriteSheetAsset.SheetType.SpritePack);
            CharacterSprites.grid = new SpriteGrid(new Vector2i(C.TILE_SIZE, C.TILE_SIZE));
            RB.MapLayerSpriteSheetSet((int)MapLayers.ENEMIES, CharacterSprites);
            RB.MapLayerSpriteSheetSet((int)MapLayers.PLAYER, CharacterSprites);

            /*
            var gameFont = new FontAsset();
            FontSprites.Load("Sprites/Font/FontSprites", SpriteSheetAsset.SheetType.SpritePack);
            var chars = new List<char>()
            {
                {'A'},{'B'},{'C'}
            };
            var glyphs = new List<string>()
            {
                {"Unicode30FF"},{"Unicode30A0"},{"@"}
            };
            GameFont = new FontAsset();
            GameFont.Setup(chars, glyphs, FontSprites, 0, 0, false);
            */
        }
    }
}