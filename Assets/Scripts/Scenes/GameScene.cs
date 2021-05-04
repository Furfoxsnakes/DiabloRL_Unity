using Actors;
using Cartography;
using Common;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapViews;

namespace Scenes
{
    public class GameScene : Scene
    {
        public GameMap GameMap => _gameMap;
        private GameMap _gameMap;

        public bool RenderRequired => _renderRequired;
        private bool _renderRequired;
        private GameState _gameState;
        
        public override void Enter()
        {
            Engine.E.Map = GenerateMap(100,100);
            _renderRequired = true;
            _gameState = GameState.PLAYER_TURN;
        }

        public override void Exit()
        {
            
        }

        public override void Update()
        {
            if (_gameState == GameState.PLAYER_TURN)
            {
                if (!RB.AnyKeyPressed()) return;

                var keyPressed = Helpers.GetPressedKeyCode();

                var didAct = Engine.I.HandleInput(keyPressed);

                if (didAct)
                {
                    _renderRequired = true;
                    _gameState = GameState.ENEMY_TURN;
                }
            }

            if (_gameState == GameState.ENEMY_TURN)
            {
                foreach (var actor in Engine.E.Map.Actors)
                {
                    if (actor is Enemy enemy)
                        enemy.Behaviour.Do(Engine.E.Player);
                }

                _gameState = GameState.PLAYER_TURN;
            }
        }

        public override void Render()
        {
            if (_renderRequired)
            {
                CenterCameraOnActor(Engine.E.Player);
                RenderFunctions.RenderAll();
                _renderRequired = false;
            }
        }

        private GameMap GenerateMap(int width, int height)
        {
            var tempMap = new ArrayMap<bool>(width, height);
            // QuickGenerators.GenerateRectangleMap(tempMap);
            QuickGenerators.GenerateRandomRoomsMap(tempMap, 20, 7, 12);
            
            var map = new GameMap(width, height);
            map.ApplyTerrainOverlay(tempMap, SpawnTerrain);

            var spawnPos = Coord.NONE;
            // spawn some enemies
            for (var i = 0; i < 40; i++)
            {
                spawnPos = map.WalkabilityView.RandomPosition(true);
                var enemy = Skeleman.Create(spawnPos, 1);
                map.AddActor(enemy);
            }

            // spawn the player
            spawnPos = map.WalkabilityView.RandomPosition(validValue: true);
            var player = new Player(spawnPos, "Player");
            map.AddActor(player);
            Engine.E.Player = player;

            map.CalculateFOV(player.Position, player.Stats[StatTypes.AWARENESS]);
            
            return map;
        }

        private void CenterCameraOnActor(Actor actor)
        {
            var pos = new Vector2i(actor.Position.X * C.TILE_SIZE, actor.Position.Y * C.TILE_SIZE);
            pos.x -= RB.DisplaySize.width / 2 - C.TILE_SIZE / 2;
            pos.y -= RB.DisplaySize.height / 2 - C.TILE_SIZE / 2;
            RB.CameraSet(pos);
        }

        private IGameObject SpawnTerrain(Coord pos, bool isWalkable) 
            => isWalkable ? TerrainFactory.Floor(pos) : TerrainFactory.Wall(pos);

    }
}