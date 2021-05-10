using Actors;
using Cartography;
using Enums;
using GoRogue.MapViews;
using UnityEngine;

namespace Common
{
    public class RenderFunctions
    {
        public static void RenderAll(Player player, Menu menu = null)
        {
            RB.Clear(Color.black);
            RenderMap(Engine.E.Map);
            RenderUI(player, menu);
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
        }

        private static void RenderMessageLog()
        {
            var messageLog = Engine.E.MessageLog;
            var yPos = RB.DisplaySize.height - 80;
            var xPos = 4;
            for (var i = 0; i < messageLog.Lines.Length; i++)
            {
                var message = messageLog.Lines[i];
                // dropshadow
                RB.Print(new Vector2i(xPos + 1, yPos + i * 8 + 1), Color.black, RB.NO_INLINE_COLOR, message);
                // actual message
                RB.Print(new Vector2i(xPos, yPos + i * 8), Color.white, message);
            }
        }
        
        private static void RenderUI(Player player, Menu menu)
        {
            var cameraPos = RB.CameraGet();
            RB.CameraReset();
            
            var barSize = new Vector2i(96, 12);
            var barPos = new Vector2i(4, 4);
            RenderBar(barPos, barSize, player.Stats[StatTypes.HEALTH], player.Stats[StatTypes.MAX_HEALTH], C.HealthBarFG, C.HealthBarBG);
            RB.Print(new Rect2i(barPos.x, barPos.y, barSize.width, barSize.height), Color.white, RB.ALIGN_H_CENTER | RB.ALIGN_V_CENTER, $"HP: {player.Stats[StatTypes.HEALTH]}/{player.Stats[StatTypes.MAX_HEALTH]}");
            
            RenderMessageLog();
            
            menu?.Render();

            RB.CameraSet(cameraPos);
        }

        private static void RenderBar(Vector2i pos, Vector2i size, int value, int maxvalue, Color32 barColour,
            Color32 backColor)
        {
            var barWidth = (int)((float) value / maxvalue * size.width);
            // draw background bar
            RB.DrawRectFill(new Rect2i(pos, size), backColor);
            // draw foreground bar
            RB.DrawRectFill(new Rect2i(pos, barWidth, size.height), barColour);
        }
    }
}