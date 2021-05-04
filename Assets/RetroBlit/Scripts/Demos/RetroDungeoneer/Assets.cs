namespace RetroBlitDemoRetroDungeoneer
{
    /// <summary>
    /// Contains all game assets
    /// </summary>
    public class Assets
    {
        /// <summary>
        /// Sprite sheet
        /// </summary>
        public SpriteSheetAsset spriteSheet = new SpriteSheetAsset();

        /// <summary>
        /// Monster death sound
        /// </summary>
        public AudioAsset soundMonsterDeath = new AudioAsset();

        /// <summary>
        /// Player death sound
        /// </summary>
        public AudioAsset soundPlayerDeath = new AudioAsset();

        /// <summary>
        /// Player foot step sound
        /// </summary>
        public AudioAsset soundFootStep = new AudioAsset();

        /// <summary>
        /// Monster attack sound
        /// </summary>
        public AudioAsset soundMonsterAttack = new AudioAsset();

        /// <summary>
        /// Player attack sound
        /// </summary>
        public AudioAsset soundPlayerAttack = new AudioAsset();

        /// <summary>
        /// Inventory sound, used for pickup, drop, equip, de-equip
        /// </summary>
        public AudioAsset soundInventory = new AudioAsset();

        /// <summary>
        /// Drink sound
        /// </summary>
        public AudioAsset soundDrink = new AudioAsset();

        /// <summary>
        /// Menu open sound
        /// </summary>
        public AudioAsset soundMenuOpen = new AudioAsset();

        /// <summary>
        /// Menu close sound
        /// </summary>
        public AudioAsset soundMenuClose = new AudioAsset();

        /// <summary>
        /// Take stairs sound
        /// </summary>
        public AudioAsset soundStairs = new AudioAsset();

        /// <summary>
        /// Mouse pointer hovered selection changed
        /// </summary>
        public AudioAsset soundPointerSelect = new AudioAsset();

        /// <summary>
        /// Select option sound
        /// </summary>
        public AudioAsset soundSelectOption = new AudioAsset();

        /// <summary>
        /// Level up jingle sound
        /// </summary>
        public AudioAsset soundLevelUp = new AudioAsset();

        /// <summary>
        /// Fireball sound
        /// </summary>
        public AudioAsset soundFireBall = new AudioAsset();

        /// <summary>
        /// Lightning sound
        /// </summary>
        public AudioAsset soundLightning = new AudioAsset();

        /// <summary>
        /// Confusion sound
        /// </summary>
        public AudioAsset soundConfuse = new AudioAsset();

        /// <summary>
        /// Enter cheat mode sound
        /// </summary>
        public AudioAsset soundCheat = new AudioAsset();

        /// <summary>
        /// Aggro sound 1
        /// </summary>
        public AudioAsset soundAggro1 = new AudioAsset();

        /// <summary>
        /// Aggro sound 2
        /// </summary>
        public AudioAsset soundAggro2 = new AudioAsset();

        /// <summary>
        /// Player fall yell (for entrance effect)
        /// </summary>
        public AudioAsset soundPlayerFallYell = new AudioAsset();

        /// <summary>
        /// Portal teleport
        /// </summary>
        public AudioAsset soundPortal = new AudioAsset();

        /// <summary>
        /// Sound bow shoot
        /// </summary>
        public AudioAsset soundBowShoot = new AudioAsset();

        /// <summary>
        /// Sound bow hit
        /// </summary>
        public AudioAsset soundBowHit = new AudioAsset();

        /// <summary>
        /// Sound web
        /// </summary>
        public AudioAsset soundWeb = new AudioAsset();

        /// <summary>
        /// Portal teleport
        /// </summary>
        public AudioAsset soundJump = new AudioAsset();

        /// <summary>
        /// Teleport
        /// </summary>
        public AudioAsset soundTeleport = new AudioAsset();

        /// <summary>
        /// Slime
        /// </summary>
        public AudioAsset soundSlime = new AudioAsset();

        /// <summary>
        /// Music for main menu
        /// </summary>
        public AudioAsset musicMainMenu = new AudioAsset();

        /// <summary>
        /// Music for game play
        /// </summary>
        public AudioAsset musicGame = new AudioAsset();

        /// <summary>
        /// Music to play upon death
        /// </summary>
        public AudioAsset musicDeath = new AudioAsset();

        /// <summary>
        /// Music to play in forest
        /// </summary>
        public AudioAsset musicForest = new AudioAsset();

        /// <summary>
        /// Font
        /// </summary>
        public FontAsset fontRetroBlitDropShadow = new FontAsset();

        /// <summary>
        /// Small font to use
        /// </summary>
        public FontAsset fontSmall = new FontAsset();

        /// <summary>
        /// Shader to vignette effect
        /// </summary>
        public ShaderAsset shaderVignette = new ShaderAsset();

        /// <summary>
        /// Load all assets
        /// </summary>
        public void LoadAll()
        {
            spriteSheet.Load("Demos/RetroDungeoneer/SpritePack", SpriteSheetAsset.SheetType.SpritePack);
            spriteSheet.grid = new SpriteGrid(new Vector2i(16, 16));

            soundMonsterDeath.Load("Demos/RetroDungeoneer/Sounds/MonsterDeath");
            soundPlayerDeath.Load("Demos/RetroDungeoneer/Sounds/PlayerDeath");
            soundFootStep.Load("Demos/RetroDungeoneer/Sounds/FootStep");
            soundMonsterAttack.Load("Demos/RetroDungeoneer/Sounds/MonsterAttack");
            soundPlayerAttack.Load("Demos/RetroDungeoneer/Sounds/PlayerAttack");
            soundInventory.Load("Demos/RetroDungeoneer/Sounds/Inventory");
            soundDrink.Load("Demos/RetroDungeoneer/Sounds/Drink");
            soundMenuOpen.Load("Demos/RetroDungeoneer/Sounds/MenuOpen");
            soundMenuClose.Load("Demos/RetroDungeoneer/Sounds/MenuClose");
            soundStairs.Load("Demos/RetroDungeoneer/Sounds/Stairs");
            soundPointerSelect.Load("Demos/RetroDungeoneer/Sounds/PointerSelect");
            soundSelectOption.Load("Demos/RetroDungeoneer/Sounds/SelectOption");
            soundLevelUp.Load("Demos/RetroDungeoneer/Sounds/LevelUp");
            soundFireBall.Load("Demos/RetroDungeoneer/Sounds/Fireball");
            soundLightning.Load("Demos/RetroDungeoneer/Sounds/Lightning");
            soundConfuse.Load("Demos/RetroDungeoneer/Sounds/Confuse");
            soundCheat.Load("Demos/RetroDungeoneer/Sounds/CheatMode");
            soundAggro1.Load("Demos/RetroDungeoneer/Sounds/Aggro1");
            soundAggro2.Load("Demos/RetroDungeoneer/Sounds/Aggro2");
            soundPlayerFallYell.Load("Demos/RetroDungeoneer/Sounds/PlayerFallYell");
            soundPortal.Load("Demos/RetroDungeoneer/Sounds/Portal");
            soundJump.Load("Demos/RetroDungeoneer/Sounds/Jump");
            soundBowShoot.Load("Demos/RetroDungeoneer/Sounds/BowShoot");
            soundBowHit.Load("Demos/RetroDungeoneer/Sounds/BowHit");
            soundWeb.Load("Demos/RetroDungeoneer/Sounds/Web");
            soundTeleport.Load("Demos/RetroDungeoneer/Sounds/Teleport");
            soundSlime.Load("Demos/RetroDungeoneer/Sounds/Slime");

            musicMainMenu.Load("Demos/RetroDungeoneer/Music/ReturnToNowhere");
            musicGame.Load("Demos/RetroDungeoneer/Music/DungeonAmbience");
            musicDeath.Load("Demos/RetroDungeoneer/Music/DeathPiano");
            musicForest.Load("Demos/RetroDungeoneer/Music/ForestAmbience");

            RB.SpriteSheetSet(spriteSheet);
            var fontSprite = RB.PackedSpriteGet(S.FONT_RETROBLIT_DROPSHADOW);
            var fontPos = new Vector2i(fontSprite.SourceRect.x + 1, fontSprite.SourceRect.y + 1);

            fontRetroBlitDropShadow.Setup('!', (char)((int)'~' + 8), fontPos, spriteSheet, new Vector2i(6, 7), ((int)'~' + 8) - (int)'!' + 1, 1, 1, false);
            fontSmall = fontRetroBlitDropShadow;

            shaderVignette.Load("Demos/RetroDungeoneer/DrawVignette");
        }
    }
}
