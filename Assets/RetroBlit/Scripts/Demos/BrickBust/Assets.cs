namespace RetroBlitDemoBrickBust
{
    /// <summary>
    /// Contains all game assets
    /// </summary>
    public class Assets
    {
        /// <summary>
        /// Ball hits brick sound
        /// </summary>
        public AudioAsset soundHitBrick = new AudioAsset();

        /// <summary>
        /// Ball hits wall sound
        /// </summary>
        public AudioAsset soundHitWall = new AudioAsset();

        /// <summary>
        /// Ball "dies" sound
        /// </summary>
        public AudioAsset soundDeath = new AudioAsset();

        /// <summary>
        /// Brick explodes sound
        /// </summary>
        public AudioAsset soundExplode = new AudioAsset();

        /// <summary>
        /// Game started sound
        /// </summary>
        public AudioAsset soundStart = new AudioAsset();

        /// <summary>
        /// Powerup collected sound
        /// </summary>
        public AudioAsset soundPowerUp = new AudioAsset();

        /// <summary>
        /// Laser shot sound
        /// </summary>
        public AudioAsset soundLaserShot = new AudioAsset();

        /// <summary>
        /// Laser hit sound
        /// </summary>
        public AudioAsset soundLaserHit = new AudioAsset();

        /// <summary>
        /// Main menu music
        /// </summary>
        public AudioAsset musicMenu = new AudioAsset();

        /// <summary>
        /// Level music
        /// </summary>
        public AudioAsset musicLevel = new AudioAsset();

        /// <summary>
        /// Game sprite sheet
        /// </summary>
        public SpriteSheetAsset spriteSheet = new SpriteSheetAsset();

        /// <summary>
        /// Shader for drop shadows
        /// </summary>
        public ShaderAsset shaderShadow = new ShaderAsset();

        /// <summary>
        /// Load all assets
        /// </summary>
        public void LoadAll()
        {
            soundHitBrick.Load("Demos/BrickBust/Sounds/hit");
            soundHitWall.Load("Demos/BrickBust/Sounds/hit2");
            soundExplode.Load("Demos/BrickBust/Sounds/explode");
            soundDeath.Load("Demos/BrickBust/Sounds/death");
            soundStart.Load("Demos/BrickBust/Sounds/start");
            soundPowerUp.Load("Demos/BrickBust/Sounds/powerup");
            soundLaserShot.Load("Demos/BrickBust/Sounds/lasershot");
            soundLaserHit.Load("Demos/BrickBust/Sounds/laserhit");

            musicMenu.Load("Demos/BrickBust/Music/BossFight");
            musicLevel.Load("Demos/BrickBust/Music/Stage2");

            // You can load a spritesheet here
            spriteSheet.Load("Demos/BrickBust/Sprites");
            spriteSheet.grid = new SpriteGrid(new Vector2i(10, 10));
            RB.SpriteSheetSet(spriteSheet);

            shaderShadow.Load("Demos/BrickBust/DrawShaderShadow");
            shaderShadow.SpriteSheetTextureSet("_SpritesTexture", spriteSheet);
        }
    }
}
