﻿namespace RetroBlitDemoSuperFlagRun
{
    /// <summary>
    /// Contains all game assets
    /// </summary>
    public class Assets
    {
        /// <summary>
        /// Jump sound
        /// </summary>
        public AudioAsset soundJump = new AudioAsset();

        /// <summary>
        /// Flag pickup sound
        /// </summary>
        public AudioAsset soundPickupFlag = new AudioAsset();

        /// <summary>
        /// Flag drop sound
        /// </summary>
        public AudioAsset soundDropFlag = new AudioAsset();

        /// <summary>
        /// Start game sound
        /// </summary>
        public AudioAsset soundStartGame = new AudioAsset();

        /// <summary>
        /// Foot step sound
        /// </summary>
        public AudioAsset soundFootStep = new AudioAsset();

        /// <summary>
        /// Game music
        /// </summary>
        public AudioAsset musicGame = new AudioAsset();

        /// <summary>
        /// SpriteSheet sprites
        /// </summary>
        public SpriteSheetAsset spriteSheetSprites = new SpriteSheetAsset();

        /// <summary>
        /// SpriteSheet title
        /// </summary>
        public SpriteSheetAsset spriteSheetTitle = new SpriteSheetAsset();

        /// <summary>
        /// SpriteSheet terrain
        /// </summary>
        public SpriteSheetAsset spriteSheetTerrain = new SpriteSheetAsset();

        /// <summary>
        /// SpriteSheet deco
        /// </summary>
        public SpriteSheetAsset spriteSheetDeco = new SpriteSheetAsset();

        /// <summary>
        /// Game font
        /// </summary>
        public FontAsset gameFont = new FontAsset();

        /// <summary>
        /// Load all assets
        /// </summary>
        public void LoadAll()
        {
            soundJump.Load("Demos/SuperFlagRun/Jump");
            soundPickupFlag.Load("Demos/SuperFlagRun/Pickup");
            soundDropFlag.Load("Demos/SuperFlagRun/DropFlag");
            soundStartGame.Load("Demos/SuperFlagRun/StartGame");
            soundFootStep.Load("Demos/SuperFlagRun/FootStep");

            musicGame.Load("Demos/SuperFlagRun/Music/GoLucky");

            spriteSheetSprites.Load("Demos/SuperFlagRun/Sprites");
            spriteSheetSprites.grid = new SpriteGrid(new Vector2i(16, 16));

            spriteSheetTitle.Load("Demos/SuperFlagRun/SpritesTitle");
            spriteSheetTitle.grid = new SpriteGrid(new Vector2i(16, 16));

            spriteSheetTerrain.Load("Demos/SuperFlagRun/TilemapTerrain");
            spriteSheetTerrain.grid = new SpriteGrid(new Vector2i(16, 16));

            spriteSheetDeco.Load("Demos/SuperFlagRun/TilemapDeco");
            spriteSheetDeco.grid = new SpriteGrid(new Vector2i(16, 16));

            RB.SpriteSheetSet(spriteSheetSprites);

            gameFont.Setup('A', 'Z', new Vector2i(0, 130), spriteSheetSprites, new Vector2i(12, 12), 6, 1, -1, false);
        }
    }
}
