using Actors;
using Cartography;
using Common;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapViews;
using UnityEngine;

namespace Scenes
{
    public class GameScene : Scene
    {
        public bool RenderRequired => _renderRequired;
        private bool _renderRequired;

        public GameState GameState
        {
            get => _gameState;
            set
            {
                _previousState = _gameState;
                _gameState = value;
            }
        }
        private GameState _gameState;
        private GameState _previousState;
        private Menu _playerMenu;
        private Menu _currentMenu;
        private Player _player => Engine.E.Player;

        public GameScene()
        {
        }
        
        public override void Enter()
        {
            Engine.E.Map = GenerateMap(100,100);
            // _renderRequired = true;
            Engine.E.MessageLog.AddMessage($"Player has entered the dungeon!");
            Engine.E.MessageLog.AddMessage($"Another message.");
            _playerMenu = new PlayerStatsMenu(_player);
            _gameState = GameState.PLAYER_TURN;
        }

        public override void Exit()
        {
            
        }

        public override void Update()
        {
            switch (GameState)
            {
                case GameState.PLAYER_TURN:
                {
                    var result = HandleKeyboard();

                    switch (result)
                    {
                        case Results.MOVED:
                        {
                            // _renderRequired = true;
                            // RenderFunctions.RenderAll(_player);
                            GameState = GameState.ENEMY_TURN;
                            break;
                        }
                        case Results.WAIT:
                        {
                            Engine.E.MessageLog.AddMessage($"Player has waited a turn.");
                            // _renderRequired = true;
                            GameState = GameState.ENEMY_TURN;
                            break;
                        }
                        case Results.CHARACTER_MENU:
                        {
                            GameState = GameState.PLAYER_MENU;
                            break;
                        }
                    }

                    break;
                }
                case GameState.ENEMY_TURN:
                {
                    foreach (var actor in Engine.E.Map.Actors)
                    {
                        if (actor is Enemy enemy)
                            enemy.Behaviour.Do(Engine.E.Player);
                    }

                    GameState = GameState.PLAYER_TURN;
                    break;
                }
                case GameState.PLAYER_MENU:
                {
                    var result = HandleKeyboard();

                    switch (result)
                    {
                        case Results.ESCAPE:
                        {
                            // if (GameState != GameState.PLAYER_MENU) break;
                            _currentMenu = null;
                            GameState = _previousState;
                            break;
                        }
                    }

                    break;
                }
            }
        }

        public override void Render()
        {
            CenterCameraOnActor(Engine.E.Player);

            switch (GameState)
            {
                case GameState.PLAYER_MENU:
                {
                    _currentMenu = _playerMenu;
                    break;
                }
            }
            
            RenderFunctions.RenderAll(Engine.E.Player, _currentMenu);
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
            var player = new Player(spawnPos, "Chorfee");
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

        private Results HandleKeyboard()
        {
            if (!RB.AnyKeyPressed()) return Results.NONE;

            var keyPressed = Helpers.GetPressedKeyCode();

            return Engine.I.HandleInput(keyPressed, GameState);
        }

    }
}