using Enums;

namespace Common
{
    public class Assets
    {
        public SpriteSheetAsset TerrainSprites = new SpriteSheetAsset();
        public SpriteSheetAsset CharacterSprites = new SpriteSheetAsset();

        public Assets()
        {
            TerrainSprites.Load("Sprites/Terrain/TerrainSprites", SpriteSheetAsset.SheetType.SpritePack);
            TerrainSprites.grid = new SpriteGrid(new Vector2i(C.TILE_SIZE,C.TILE_SIZE));
            RB.MapLayerSpriteSheetSet((int)MapLayers.TERRAIN, TerrainSprites);

            CharacterSprites.Load("Sprites/Characters/CharacterSprites", SpriteSheetAsset.SheetType.SpritePack);
            CharacterSprites.grid = new SpriteGrid(new Vector2i(C.TILE_SIZE, C.TILE_SIZE));
            RB.MapLayerSpriteSheetSet((int)MapLayers.ENEMIES, CharacterSprites);
            RB.MapLayerSpriteSheetSet((int)MapLayers.PLAYER, CharacterSprites);
        }
    }
}