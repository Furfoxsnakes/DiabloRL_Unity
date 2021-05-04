using Actors;
using Cartography;
using Enums;
using GoRogue.MapViews;
using UnityEngine;

namespace Common
{
    public class RenderFunctions
    {
        public static void RenderAll()
        {
            RB.Clear(Color.black);
            RenderMap(Engine.E.Map);
        }

        private static void RenderMap(GameMap map)
        {
            RB.SpriteSheetSet(Engine.A.TerrainSprites);
            RB.DrawMapLayer((int)MapLayers.TERRAIN);
            
            foreach (var pos in map.Positions())
            {
                if (!map.Explored[pos]) continue;
                
                var terrain = map.GetTerrain<MapTerrain>(pos);

                var alpha = map.FOV.BooleanFOV[pos] ? (byte)255 : (byte)127;
                RB.AlphaSet(alpha);
                RB.DrawSprite(terrain.SpriteID, new Vector2i(pos.X * C.TILE_SIZE, pos.Y * C.TILE_SIZE));
                RB.AlphaSet(255);
            }

            RB.SpriteSheetSet(Engine.A.CharacterSprites);
            foreach (var actor in map.Actors)
            {
                if (!map.Explored[actor.Position]) continue;

                var layerID = (actor is Enemy) ? (int)MapLayers.ENEMIES : (int)MapLayers.PLAYER;
                RB.DrawMapLayer(layerID);
                RB.DrawSprite(actor.SpriteID, new Vector2i(actor.Position.X * C.TILE_SIZE, actor.Position.Y * C.TILE_SIZE));

            }
            
            // foreach (var pos in map.Positions())
            // {
            //     if (!map.Explored[pos]) continue;
            //
            //
            //     var actor = map.GetEntity<Actor>(pos);
            //
            //     if (actor == null) continue;
            //
            //     //TODO: Will need to specify enemy or player layer in the future
            //     RB.DrawMapLayer((int)MapLayers.PLAYER);
            //     RB.DrawSprite(actor.SpriteID, new Vector2i(pos.X * C.TILE_SIZE, pos.Y * C.TILE_SIZE));
            // }
        }
    }
}