using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.VisualBasic;
using System.Threading.Tasks;
using System.Threading;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Net.Mime;

// Game written by Alex Rooth, Tilesheet by Ivan Voirol

namespace monogame
{
    public class map{
        public string[][] tilePath = new string[10][];
        public int[][] mapObject = new int[10][];
        public string[][] decoBack = new string[10][];
        public string[][] decoFront = new string[10][];
        public int[][] enemyMap = new int[10][];
        public List<System.Numerics.Vector2>[][] path = new List<System.Numerics.Vector2>[10][];
        public System.Numerics.Vector2 playerStart;
    }
    public class enemy{
        public SoundEffectInstance wanderInstance;
        public SoundEffectInstance fireInstance;
        public SoundEffectInstance explosionInstance;
        public System.Numerics.Vector2 Position;
        public System.Numerics.Vector2 pastPosition;
        public int wheelTextureSlider;
        public int shootCooldown;
        public int canonHot;
        public float rotation;
        public float canonRotation;
        public int movementCounter;
        public List<System.Numerics.Vector2> trails = new List<System.Numerics.Vector2>();
        public List<float> rotationTrails = new List<float>();
        public Texture2D topTexture;
        public Texture2D topTextureNormal;
        public Texture2D hullTexture;
        public Texture2D explosionTexture;
        public int invicibilityFrames;
        public int health;
        public float speed;
        public int maxShootCooldown;
        public float bulletSpeed;
        public bool hit = false;
        public double closestDistanceToBullet;
        public int[][] invisibleObjects;
        public System.Numerics.Vector2 PlayerPosition;
        public float distanceFromPlayer;
        public int type;
        public bool dead = false;
        public int explodedTimer = -10;
        public int playerSeeingDistance;
        public int playerForgettingDistance;
        public List<System.Numerics.Vector2> path = new List<System.Numerics.Vector2>();
        public List<double[]> bullets = new List<double[]>();
        public List<double[]> playerBullets = new List<double[]>();
        public int pathStage = 0;
        public bool seesPlayer = false;
        public void move(List<System.Numerics.Vector2> path){
            if(canonHot>0) canonHot--;
            if(invicibilityFrames>0) invicibilityFrames--;

            // Distance from player
            distanceFromPlayer = (float)Math.Sqrt(Math.Pow(Position.X - PlayerPosition.X, 2) + Math.Pow(Position.Y - PlayerPosition.Y, 2));
            if(distanceFromPlayer < playerSeeingDistance && invisibleObjects[(int)(PlayerPosition.X / 20)][(int)(PlayerPosition.Y / 20)] != 3) seesPlayer = true;
            if(distanceFromPlayer > playerForgettingDistance) seesPlayer = false;

            // Distance from nearest player bullet
            closestDistanceToBullet = 1000;
            for(int i = 0; i < playerBullets.Count; i++){
                double distanceToBullet = Math.Sqrt(Math.Pow(Position.X - playerBullets[i][0], 2) + Math.Pow(Position.Y - playerBullets[i][1], 2));
                if(distanceToBullet < closestDistanceToBullet) closestDistanceToBullet = distanceToBullet;
            }

            if(closestDistanceToBullet < 10 && invicibilityFrames == 0){
                health--;
                invicibilityFrames = 25;
                explosionInstance.Volume = 0.5f; // Adjust the volume (0.0f to 1.0f)
                explosionInstance.Play();
                explodedTimer = 10;
                topTextureNormal = topTexture;
                topTexture = explosionTexture;
            } 

            for(int i = 0; i < bullets.Count; i++){
                bullets[i][0] += bulletSpeed * (float)Math.Sin(bullets[i][2]);
                bullets[i][1] -= bulletSpeed * (float)Math.Cos(bullets[i][2]);
                if(bullets[i][0] > 200 || bullets[i][0] < 0 || bullets[i][1] > 200 || bullets[i][1] < 0){
                    bullets.RemoveAt(i);
                }
                try{
                    if(invisibleObjects[(int)(bullets[i][0] / 20)][(int)(bullets[i][1] / 20)] == 2){
                        bullets.RemoveAt(i);
                    }
                } catch {}
            }

            if(health <= 0 && !dead){
                dead = true;
                topTexture = explosionTexture;
                type = 0;
                path = new List<System.Numerics.Vector2>();
                return;
            }

            if(explodedTimer > 0) {
                canonRotation = 0;
                explodedTimer--;
                return;
            }

            if(explodedTimer == 0) topTexture = topTextureNormal;

            if(dead && explodedTimer == 0){
                Position = new System.Numerics.Vector2(-100, -100);
            }

            if(!seesPlayer) canonRotation = rotation * (float)(Math.PI / 180);
            else {
                canonRotation = (float)Math.Atan2(PlayerPosition.Y - Position.Y, PlayerPosition.X - Position.X) + (float)Math.PI / 2;
                if(shootCooldown>0) shootCooldown--;
                if(shootCooldown==0){
                    bullets.Add(new double[]{(double)Position.X, (double)Position.Y, (double)canonRotation});
                    canonHot = 10;
                    fireInstance.Volume = 0.5f; // Adjust the volume (0.0f to 1.0f)
                    fireInstance.Play();
                    shootCooldown = maxShootCooldown;
                }
            }

            if(path.Count < 2) return;
            System.Numerics.Vector2 destination = path[pathStage + 1];
            System.Numerics.Vector2 enemyToDestination;
            enemyToDestination = destination - Position;

            float rotationRequired = (float)Math.Atan2(enemyToDestination.Y, enemyToDestination.X) + (float)Math.PI / 2;
            float rotationRequiredDegrees = rotationRequired * (float)(180 / Math.PI);
            
            if(rotationRequiredDegrees -(2* (speed * 2)) > rotation){
                rotation += 2 * (speed * 2);
            } else if(rotationRequiredDegrees +(2 * (speed * 2)) < rotation){
                rotation -= 2 * (speed * 2);
            }

            if(rotationRequiredDegrees -(5 * (speed * 2)) < rotation && rotationRequiredDegrees +(5 * (speed * 2)) > rotation){
                rotation = rotationRequiredDegrees;
                movementCounter++;
                if(movementCounter * speed % 4==0){
                    trails.Add(Position);
                    // rotation to radians
                    rotationTrails.Add((float)(rotation * (Math.PI / 180)));
                }

                if(trails.Count > 150){
                    trails.RemoveAt(0);
                }

                wheelTextureSlider++;
                if (wheelTextureSlider > 2) wheelTextureSlider = 0;
                // Move forward based on the current rotation angle
                double rotationRadians = rotation * (Math.PI / 180); // Convert rotation to radians

                pastPosition = Position;

                Position.X += speed * (float)Math.Sin(rotationRadians);
                Position.Y -= speed * (float)Math.Cos(rotationRadians);

                wanderInstance.Volume = 0.2f; // Adjust the volume (0.0f to 1.0f)
                if(wanderInstance.State != SoundState.Playing) wanderInstance.Play();

                if(Position.X>200 || Position.X<0){
                    Position.X = pastPosition.X;
                }
                if(Position.Y>200 || Position.Y<0){
                    Position.Y = pastPosition.Y;
                }

                if(Position.X < destination.X + 2 && Position.X > destination.X - 2 && Position.Y < destination.Y + 2 && Position.Y > destination.Y - 2){
                    pathStage++;
                    Position = destination;
                }

                if(pathStage + 2 > path.Count){
                    pathStage = -1;
                }
            }
        }
    }
    public class Game1 : Game
    {
        // Map making
        const bool mapMakerMode = false;

        // Audio
        Song[] music = new Song[22];
        private SoundEffect fireSoundEffect;
        private SoundEffect wanderSoundEffect;
        private SoundEffect winSoundEffect;
        private SoundEffect explosionSoundEffect;
        SoundEffectInstance wanderInstance;
        SoundEffectInstance winInstance;
        SoundEffectInstance fireInstance;
        SoundEffectInstance explosionInstance;

        // Player variables
        private System.Numerics.Vector2 playerPosition = new System.Numerics.Vector2(80, 80);
        private System.Numerics.Vector2 pastPlayerPosition = new System.Numerics.Vector2(80, 80);
        private int playerWheelTextureslider = 0;
        private int playerShootCooldown = 0;
        private double distanceFromNearestBullet = 1000;
        private int canonHot = 0;
        private int rotation = 0;
        private float canonRotation = 0;
        private int movementCounter = 0;

        private List<System.Numerics.Vector2> playerTrails = new List<System.Numerics.Vector2>();
        private List<double[]> playerBullets = new List<double[]>();
        private List<double> playerRotationTrails = new List<double>();

        // Map maker variables
        int tile;
        int invisibleObjectTile;
        int enemyTile;
        int decorationLayer = 0;
        bool tileSelectorMode;
        int page;
        bool invisibleObjectMode = false;

        bool enemyPlacementMode = false;

        System.Numerics.Vector2 selectedTank;

        // Game variables
        int level = 0;
        int dangerLevel = 0;
        int musicDangerlevel = 0;
        int lives = 3;
        public List<enemy> enemies = new List<enemy>();

        bool levelWon = false;
        bool dead = false;
        
        // Maps
        map currentMap;

        // Enemy type data
        float[] enemySpeeds = new float[]{0.5f, 0.75f, 1.25f, 0.5f, 0.2f};
        int[] enemyHealths = new int[]{1, 2, 1, 2, 2};
        int[] enemyShootCooldowns = new int[]{150, 100, 100, 200, 15};
        int[] enemyPlayerSeeingDistance = new int[]{75, 100, 75, 500, 100};
        int[] enemyPlayerForgettingDistance = new int[]{100, 150, 100, 500, 150};
        int[] enemyBulletSpeeds = new int[]{2, 2, 2, 5, 2};
        const int enemyTypes = 5;

        // Framework
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Textures
        public Texture2D[][] mapTexture = new Texture2D[10][]; // Create an array of Texture2D
        public Texture2D[][] mapDecoFrontTexture = new Texture2D[10][]; // Create an array of Texture2D
        public Texture2D[][] mapDecoBackTexture = new Texture2D[10][]; // Create an array of Texture2D
        public Texture2D[][] tileSelectorTiles = new Texture2D[10][]; // Create an array of Texture2D
        private Texture2D[] playerWheelTextures = new Texture2D[3]; // Create an array of Texture2D
        private Texture2D playerHullTexture; // Create a Texture2D
        private Texture2D GhostTileTexture; // Create a Texture2D
        private Texture2D playerCanonTexture; // Create a Texture2D
        public Texture2D explosionTexture; // Create a Texture2D
        private Texture2D playerHotCanonTexture; // Create a Texture2D
        private Texture2D playerTopTexture; // Create a Texture2D
        private Texture2D trailTexture; // Create a Texture2D
        private Texture2D bulletTexture; // Create a Texture2D
        private Texture2D[] signs = new Texture2D[10]; // Create an array of Texture2D
        private Texture2D[] enemyIcons = new Texture2D[10]; // Create an array of Texture2D

        public void reloadMap()
        {
            enemies.Clear();

            // Try to load the map from a JSON file
            if (System.IO.File.Exists($"Content/maps/{level}.json"))
            {
                string json = System.IO.File.ReadAllText($"Content/maps/{level}.json");
                currentMap = JsonConvert.DeserializeObject<map>(json);

                // Load map textures based on the loaded map data
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 10; j++)
                    {
                        if(currentMap.tilePath[i][j] == null){
                            mapTexture[i][j] = null;
                            continue;
                        }
                        mapTexture[i][j] = Content.Load<Texture2D>(currentMap.tilePath[i][j]);
                    }
                }

                // Load map frontal decoration textures based on the loaded map data
                for (int i = 0; i < 10; i++)
                {
                    if(currentMap.decoFront[i] == null){
                        currentMap.decoFront[i] = new string[10];
                        for(int j = 0; j < 10; j++){
                            currentMap.decoFront[i][j] = null;
                        }
                    }
                    for (int j = 0; j < 10; j++)
                    {
                        if(currentMap.decoFront[i][j] == null) {
                            mapDecoFrontTexture[i][j] = null;
                            continue;
                        }
                        mapDecoFrontTexture[i][j] = Content.Load<Texture2D>(currentMap.decoFront[i][j]);
                    }
                }

                // Load map background decoration textures based on the loaded map data
                for (int i = 0; i < 10; i++)
                {
                    if(currentMap.decoBack[i] == null){
                        currentMap.decoBack[i] = new string[10];
                        for(int j = 0; j < 10; j++){
                            currentMap.decoBack[i][j] = null;
                        }
                    }
                    for (int j = 0; j < 10; j++){
                        if(currentMap.decoBack[i][j] == null){
                            mapDecoBackTexture[i][j] = null;
                            continue;
                        }
                        mapDecoBackTexture[i][j] = Content.Load<Texture2D>(currentMap.decoBack[i][j]);
                    }
                }

                playerPosition = currentMap.playerStart;
                playerTrails.Clear();
                playerRotationTrails.Clear();

                // Load enemies
                for (int i = 0; i < 10; i++){
                    if(currentMap.path[i] == null) {
                        currentMap.path[i] = new List<System.Numerics.Vector2>[10];
                        for(int j = 0; j < 10; j++){
                            currentMap.path[i][j] = new List<System.Numerics.Vector2>();
                        }
                    }
                    {
                        if(currentMap.enemyMap[i]==null){
                            currentMap.enemyMap[i] = new int[10];
                            for(int j = 0; j < 10; j++){
                                currentMap.enemyMap[i][j] = 0;
                            }
                        }
                        for (int j = 0; j < 10; j++){
                            if(currentMap.enemyMap[i][j] == 0) continue;
                            enemy newEnemy = new enemy();
                            newEnemy.Position = new System.Numerics.Vector2(i * 20 + 10, j * 20 + 10);
                            newEnemy.pastPosition = new System.Numerics.Vector2(i * 20 + 10, j * 20 + 10);
                            newEnemy.wheelTextureSlider = 0;
                            newEnemy.shootCooldown = enemyShootCooldowns[currentMap.enemyMap[i][j] - 1];
                            newEnemy.maxShootCooldown = enemyShootCooldowns[currentMap.enemyMap[i][j] - 1];
                            newEnemy.canonHot = 0;
                            newEnemy.rotation = 0;
                            newEnemy.canonRotation = 0;
                            newEnemy.movementCounter = 0;
                            newEnemy.type = currentMap.enemyMap[i][j];
                            newEnemy.hullTexture = Content.Load<Texture2D>($"tank/enemies/{newEnemy.type}/hull");
                            newEnemy.topTexture = Content.Load<Texture2D>($"tank/enemies/{newEnemy.type}/top");
                            newEnemy.health = enemyHealths[currentMap.enemyMap[i][j] - 1];
                            newEnemy.speed = enemySpeeds[currentMap.enemyMap[i][j] - 1];
                            newEnemy.path = new List<System.Numerics.Vector2>(currentMap.path[i][j]);
                            newEnemy.path.Insert(0, new System.Numerics.Vector2((i * 20) + 10, (j * 20) + 10));
                            newEnemy.wanderInstance = wanderSoundEffect.CreateInstance();
                            newEnemy.fireInstance = fireSoundEffect.CreateInstance();
                            newEnemy.explosionInstance = explosionSoundEffect.CreateInstance();
                            newEnemy.explosionTexture = explosionTexture;
                            newEnemy.playerSeeingDistance = enemyPlayerSeeingDistance[currentMap.enemyMap[i][j] - 1];
                            newEnemy.playerForgettingDistance = enemyPlayerForgettingDistance[currentMap.enemyMap[i][j] - 1];
                            newEnemy.bulletSpeed = enemyBulletSpeeds[currentMap.enemyMap[i][j] - 1];
                            enemies.Add(newEnemy);
                        }
                    }
                }
            }
        }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 800;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load player textures
            playerWheelTextures[0] = Content.Load<Texture2D>("tank/wheels");
            playerWheelTextures[1] = Content.Load<Texture2D>("tank/wheels2");
            playerWheelTextures[2] = Content.Load<Texture2D>("tank/wheels3");
            playerHullTexture = Content.Load<Texture2D>("tank/hull");
            playerCanonTexture = Content.Load<Texture2D>("tank/canon");
            playerTopTexture = Content.Load<Texture2D>("tank/top");
            trailTexture = Content.Load<Texture2D>("tank/trail");
            bulletTexture = Content.Load<Texture2D>("tank/bullet");
            explosionTexture = Content.Load<Texture2D>("tank/misc/explosion");
            playerHotCanonTexture = Content.Load<Texture2D>("tank/hotcanon");

            // Load music
            for(int i = 0; i < 22; i++){
                music[i] = Content.Load<Song>($"sound/music/{i+1}");
            }

            // Load SFX
            fireSoundEffect = Content.Load<SoundEffect>("sound/sfx/fire");
            wanderSoundEffect = Content.Load<SoundEffect>("sound/sfx/wander");
            winSoundEffect = Content.Load<SoundEffect>("sound/sfx/win");
            explosionSoundEffect = Content.Load<SoundEffect>("sound/sfx/explosion");
            winInstance = winSoundEffect.CreateInstance();
            wanderInstance = wanderSoundEffect.CreateInstance();
            fireInstance = fireSoundEffect.CreateInstance();
            explosionInstance = explosionSoundEffect.CreateInstance();

            // Load signs
            signs[0] = Content.Load<Texture2D>("signs/0");
            signs[1] = Content.Load<Texture2D>("signs/1");
            signs[2] = Content.Load<Texture2D>("signs/2");
            signs[3] = Content.Load<Texture2D>("signs/3");

            // Load enemy icons
            enemyIcons[0] = Content.Load<Texture2D>("signs/0");
            enemyIcons[1] = Content.Load<Texture2D>("tank/icons/red");
            enemyIcons[2] = Content.Load<Texture2D>("tank/icons/yellow");
            enemyIcons[3] = Content.Load<Texture2D>("tank/icons/green");
            enemyIcons[4] = Content.Load<Texture2D>("tank/icons/white");
            enemyIcons[5] = Content.Load<Texture2D>("tank/icons/black");

            page = 1;
            selectedTank = new System.Numerics.Vector2(-150, -150);

            // Load map
            currentMap = new map();
            
            for (int i = 0; i < 10; i++)
            {
                mapTexture[i] = new Texture2D[10];  // Initialize the mapTexture array
                mapDecoFrontTexture[i] = new Texture2D[10];  // Initialize the mapTexture array
                mapDecoBackTexture[i] = new Texture2D[10];  // Initialize the mapTexture array
                tileSelectorTiles[i] = new Texture2D[10];
                currentMap.tilePath[i] = new string[10];
                currentMap.mapObject[i] = new int[10];
                currentMap.enemyMap[i] = new int[10];
            }

            reloadMap();

            for (int i = 1; i <= 10; i++){
                for (int j = 1; j <= 10; j++){
                    if(!System.IO.File.Exists($"Content/maps/textures/{j + ((i-1) * 10)}.png")) continue;
                    tileSelectorTiles[i-1][j-1] = Content.Load<Texture2D>($"maps/textures/{j + ((i-1) * 10)}");
                }
            }

            GhostTileTexture = Content.Load<Texture2D>("maps/textures/1");

            for(int i = 0; i < enemies.Count; i++){
                dangerLevel += enemies[i].type;
            }
            if(dangerLevel==0) MediaPlayer.Play(music[dangerLevel]);
            else MediaPlayer.Play(music[dangerLevel-1]);
            MediaPlayer.IsRepeating = true;
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Calculate the vector from the player's position to the mouse cursor
            System.Numerics.Vector2 mousePosition = new System.Numerics.Vector2(Mouse.GetState().X / 4, Mouse.GetState().Y / 4);
            System.Numerics.Vector2 playerToMouse;
            playerToMouse = mousePosition - playerPosition;
            MouseState currentMouseState = Mouse.GetState();
            if(playerShootCooldown>0) playerShootCooldown--;
            if(canonHot>0) canonHot--;

            if(dead){
                Thread.Sleep(10);
                playerPosition = new System.Numerics.Vector2(-100, -100);
                Thread.Sleep(750);
                if(lives>0){
                    lives--;
                    dead = false;
                    playerPosition = currentMap.playerStart;
                    playerTopTexture = Content.Load<Texture2D>("tank/top");
                    reloadMap();
                    if(dangerLevel==0) MediaPlayer.Play(music[dangerLevel]);
                    else MediaPlayer.Play(music[dangerLevel-1]);
                    MediaPlayer.IsRepeating = true;
                } else
                Exit();
            }

            dangerLevel = 0;
            for(int i = 0; i < enemies.Count; i++){
                dangerLevel += enemies[i].type;
            }

            if (dangerLevel==0 && levelWon != true && !mapMakerMode){
                MediaPlayer.Stop();
                winInstance.Volume = 0.5f;
                winInstance.Play();
                levelWon = true;
            }

            if(levelWon==true&&winInstance.State!=SoundState.Playing){
                Thread.Sleep(1000);
                level++;
                enemies.Clear();
                levelWon = false;
                // Try to load the map from a JSON file
                if (System.IO.File.Exists($"Content/maps/{level}.json"))
                {
                    reloadMap();

                    for(int i = 0; i < enemies.Count; i++){
                        dangerLevel += enemies[i].type;
                    }
                    if(dangerLevel==0) MediaPlayer.Play(music[dangerLevel]);
                    else MediaPlayer.Play(music[dangerLevel-1]);
                    MediaPlayer.IsRepeating = true;
                } else {
                    Exit();
                }
            }

            if(dangerLevel != musicDangerlevel && dangerLevel != 0){
                TimeSpan musicTimespan = MediaPlayer.PlayPosition;
                MediaPlayer.Play(music[dangerLevel-1], musicTimespan);
                MediaPlayer.IsRepeating = true;
            }

            musicDangerlevel = dangerLevel;

            // Calculate the angle in radians to make the cannon rotate from 0 to 360 degrees
            canonRotation = (float)Math.Atan2(playerToMouse.Y, playerToMouse.X) + (float)Math.PI / 2;
            if(!mapMakerMode){
                // Enemy game logic
                for(int i = 0; i < enemies.Count; i++){
                    enemies[i].PlayerPosition = playerPosition;
                    enemies[i].playerBullets = playerBullets;
                    enemies[i].invisibleObjects = currentMap.mapObject;
                    enemies[i].move(enemies[i].path);
                } 
                if (Keyboard.GetState().IsKeyDown(Keys.W))
                {
                    // Move forward based on the current rotation angle
                    float moveSpeed = 1.0f; // Adjust the speed as needed
                    double rotationRadians = rotation * (Math.PI / 180); // Convert rotation to radians

                    pastPlayerPosition = playerPosition;

                    playerPosition.X += moveSpeed * (float)Math.Sin(rotationRadians);
                    playerPosition.Y -= moveSpeed * (float)Math.Cos(rotationRadians);

                    wanderInstance.Volume = 0.5f; // Adjust the volume (0.0f to 1.0f)
                    if(wanderInstance.State != SoundState.Playing) wanderInstance.Play();

                    if(playerPosition.X>200 || playerPosition.X<0){
                        playerPosition.X = pastPlayerPosition.X;
                    }
                    if(playerPosition.Y>200 || playerPosition.Y<0){
                        playerPosition.Y = pastPlayerPosition.Y;
                    }

                    try {
                        if(currentMap.mapObject[(int)(playerPosition.X / 20)][(int)(playerPosition.Y / 20)] == 1||currentMap.mapObject[(int)(playerPosition.X / 20)][(int)(playerPosition.Y / 20)] == 2){
                            playerPosition.X = pastPlayerPosition.X;
                            playerPosition.Y = pastPlayerPosition.Y;
                        }
                    } catch {}

                    movementCounter++;
                    if(movementCounter % 4==0){
                        playerTrails.Add(playerPosition);
                        playerRotationTrails.Add(rotationRadians);
                    }

                    if(playerTrails.Count > 150){
                        playerTrails.RemoveAt(0);
                        playerRotationTrails.RemoveAt(0);
                    }

                    playerWheelTextureslider++;
                    if (playerWheelTextureslider > 2) playerWheelTextureslider = 0;
                }

                for(int i = 0; i < playerBullets.Count; i++){
                        playerBullets[i][0] += 2 * (float)Math.Sin(playerBullets[i][2]);
                        playerBullets[i][1] -= 2 * (float)Math.Cos(playerBullets[i][2]);
                        if(playerBullets[i][0] > 200 || playerBullets[i][0] < 0 || playerBullets[i][1] > 200 || playerBullets[i][1] < 0){
                            playerBullets.RemoveAt(i);
                        }
                        try{
                            if(currentMap.mapObject[(int)(playerBullets[i][0] / 20)][(int)(playerBullets[i][1] / 20)] == 2){
                                playerBullets.RemoveAt(i);
                            }
                        } catch {}
                    }

                if (Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    // Move backward based on the current rotation angle
                    float moveSpeed = 1.0f; // Adjust the speed as needed
                    double rotationRadians = rotation * (Math.PI / 180); // Convert rotation to radians

                    pastPlayerPosition = playerPosition;

                    playerPosition.X -= moveSpeed * (float)Math.Sin(rotationRadians);
                    playerPosition.Y += moveSpeed * (float)Math.Cos(rotationRadians);

                    wanderInstance.Volume = 0.5f; // Adjust the volume (0.0f to 1.0f)
                    if(wanderInstance.State == SoundState.Stopped) wanderInstance.Play();
                
                    if(playerPosition.X>200 || playerPosition.X<0){
                        playerPosition.X = pastPlayerPosition.X;
                    }
                    if(playerPosition.Y>200 || playerPosition.Y<0){
                        playerPosition.Y = pastPlayerPosition.Y;
                    }

                    try {
                        if(currentMap.mapObject[(int)(playerPosition.X / 20)][(int)(playerPosition.Y / 20)] == 1||currentMap.mapObject[(int)(playerPosition.X / 20)][(int)(playerPosition.Y / 20)] == 2){
                            playerPosition.X = pastPlayerPosition.X;
                            playerPosition.Y = pastPlayerPosition.Y;
                        }
                    } catch {}
                    

                    movementCounter++;
                    if(movementCounter % 4==0){
                        playerTrails.Add(playerPosition);
                        playerRotationTrails.Add(rotationRadians);
                    }

                    if(playerTrails.Count > 150){
                        playerTrails.RemoveAt(0);
                        playerRotationTrails.RemoveAt(0);
                    }

                    playerWheelTextureslider--;
                    if (playerWheelTextureslider < 0) playerWheelTextureslider = 2;
                }
                if(!Keyboard.GetState().IsKeyDown(Keys.W)&&!Keyboard.GetState().IsKeyDown(Keys.S))
                {
                    wanderInstance.Stop();
                }

                if(Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    rotation -= 2;
                }
                if(Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    rotation += 2;
                }
                if(Keyboard.GetState().IsKeyDown(Keys.O)){
                    if(Keyboard.GetState().IsKeyDown(Keys.P)){
                        if(Keyboard.GetState().IsKeyDown(Keys.Tab)){
                            level++;
                            Console.WriteLine($"Loaded level {level}");
                            reloadMap();
                            Thread.Sleep(100);
                        }
                    }
                }

                if(currentMouseState.LeftButton == ButtonState.Pressed && playerShootCooldown == 0){
                    playerBullets.Add(new double[]{(double)playerPosition.X, (double)playerPosition.Y, (double)canonRotation});
                    canonHot = 10;
                    fireInstance.Volume = 0.5f; // Adjust the volume (0.0f to 1.0f)
                    fireInstance.Play();
                    playerShootCooldown = 70;
                }

                // Set nearest enemy bullet
                // Distance from nearest player bullet
                distanceFromNearestBullet = 1000;
                for(int i = 0; i < enemies.Count; i++){
                    for(int j = 0; j < enemies[i].bullets.Count; j++){
                        if(playerPosition.X < enemies[i].bullets[j][0] + 2 && playerPosition.X > enemies[i].bullets[j][0] - 2 && playerPosition.Y < enemies[i].bullets[j][1] + 2 && playerPosition.Y > enemies[i].bullets[j][1] - 2){
                            double distanceToBullet = Math.Sqrt(Math.Pow(playerPosition.X - enemies[i].bullets[j][0], 2) + Math.Pow(playerPosition.Y - enemies[i].bullets[j][1], 2));
                            if(distanceToBullet < distanceFromNearestBullet) distanceFromNearestBullet = distanceToBullet;
                        }
                    }
                }

                if(distanceFromNearestBullet < 5){
                    dead = true;
                    playerTopTexture = explosionTexture;
                    canonRotation = 0;
                    explosionSoundEffect.Play();
                    MediaPlayer.Stop();
                }
            } else {
                try{
                    if(Keyboard.GetState().IsKeyDown(Keys.Insert)){
                        Console.Write("Enter tile > ");
                        tile = int.Parse(Console.ReadLine());
                        if(tile > 970||tile < 1) tile = 1;
                        Console.WriteLine($"Tile: {tile}");
                        GhostTileTexture = Content.Load<Texture2D>($"maps/textures/{tile}");
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.C)){
                        float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                        float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                        int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                        int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                        if(!tileSelectorMode){
                            GhostTileTexture = Content.Load<Texture2D>(currentMap.tilePath[gridX][gridY]);
                            tile = int.Parse(currentMap.tilePath[gridX][gridY].Substring(14));
                        } else {
                            GhostTileTexture = Content.Load<Texture2D>($"maps/textures/{(gridY+1 + ((gridX) * 10)) + ((page-1) * 100)}");
                            tile = ((gridY+1 + ((gridX) * 10)) + ((page-1) * 100));
                        }
                        
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.F)){
                        tileSelectorMode = !tileSelectorMode;
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.L)){
                        level++;
                        Console.WriteLine($"Loaded level {level}");
                        reloadMap();
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.G)){
                        reloadMap();
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.O)){
                        invisibleObjectMode = !invisibleObjectMode;
                        if(invisibleObjectMode){
                            GhostTileTexture = signs[1];
                            invisibleObjectTile = 1;
                        } else {
                            GhostTileTexture = Content.Load<Texture2D>($"maps/textures/1");
                        }
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.E)){
                        enemyPlacementMode = !enemyPlacementMode;
                        if(enemyPlacementMode){
                            GhostTileTexture = enemyIcons[1];
                            enemyTile = 1;
                        } else {
                            GhostTileTexture = Content.Load<Texture2D>($"maps/textures/1");
                        }
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.P)){
                        float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                        float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                        int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                        int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                        currentMap.playerStart = new System.Numerics.Vector2(gridX * 20 + 10, gridY * 20 + 10);
                        Console.WriteLine($"Player start position is now {currentMap.playerStart.X}, {currentMap.playerStart.Y}");
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.Right))
                    {
                        if(!enemyPlacementMode){
                            if(!invisibleObjectMode){
                                if(!tileSelectorMode){
                                    tile++;
                                    Console.WriteLine($"Tile: {tile}");
                                    if(tile > 970) tile = 1;
                                    GhostTileTexture = Content.Load<Texture2D>($"maps/textures/{tile}");
                                } else {
                                    page++;
                                    if(page > 11) page = 1;
                                    for (int i = 1; i <= 10; i++){
                                        for (int j = 1; j <= 10; j++){
                                            if(!System.IO.File.Exists($"Content/maps/textures/{(j + ((i-1) * 10)) + ((page-1) * 100)}.png")) continue;
                                            tileSelectorTiles[i-1][j-1] = Content.Load<Texture2D>($"maps/textures/{(j + ((i-1) * 10)) + ((page-1) * 100)}");
                                        }
                                    }
                                }
                            } else {
                                invisibleObjectTile++;
                                if(invisibleObjectTile > 3) invisibleObjectTile = 0;
                                GhostTileTexture = signs[invisibleObjectTile];
                            }
                        } else {
                            enemyTile++;
                            if(enemyTile > enemyTypes) enemyTile = 0;
                            GhostTileTexture = enemyIcons[enemyTile];
                        }
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.Left))
                    {
                        if(!enemyPlacementMode){
                            if(!invisibleObjectMode){
                                if(!tileSelectorMode){
                                tile--;
                                if(tile < 1) tile = 970;
                                Console.WriteLine($"Tile: {tile}");
                                GhostTileTexture = Content.Load<Texture2D>($"maps/textures/{tile}");
                                } else {
                                    page--;
                                    if(page < 1) page = 11;
                                    for (int i = 1; i <= 10; i++){
                                        for (int j = 1; j <= 10; j++){
                                            if(!System.IO.File.Exists($"Content/maps/textures/{(j + ((i-1) * 10)) + ((page-1) * 100)}.png")) continue;
                                            tileSelectorTiles[i-1][j-1] = Content.Load<Texture2D>($"maps/textures/{(j + ((i-1) * 10)) + ((page-1) * 100)}");
                                        }
                                    }
                                }
                            } else {
                                invisibleObjectTile--;
                                if(invisibleObjectTile < 0) invisibleObjectTile = 3;
                                GhostTileTexture = signs[invisibleObjectTile];
                            }
                        } else {
                            enemyTile--;
                            if(enemyTile < 0) enemyTile = enemyTypes;
                            GhostTileTexture = enemyIcons[enemyTile];
                        }
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.Space))
                    {
                        if(!enemyPlacementMode){
                            if(!invisibleObjectMode){
                                float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                                float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                                int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                                int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                                switch(decorationLayer){
                                    case 0:
                                        mapTexture[gridX][gridY] = Content.Load<Texture2D>($"maps/textures/{tile}");
                                        currentMap.tilePath[gridX][gridY] = $"maps/textures/{tile}";
                                        break;
                                    case 1:
                                        mapDecoBackTexture[gridX][gridY] = Content.Load<Texture2D>($"maps/textures/{tile}");
                                        currentMap.decoBack[gridX][gridY] = $"maps/textures/{tile}";
                                        break;
                                    case 2:
                                        mapDecoFrontTexture[gridX][gridY] = Content.Load<Texture2D>($"maps/textures/{tile}");
                                        currentMap.decoFront[gridX][gridY] = $"maps/textures/{tile}";
                                        break;
                                }
                            } else {
                                float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                                float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                                int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                                int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                                currentMap.mapObject[gridX][gridY] = invisibleObjectTile;
                            }
                        } else {
                            float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                            float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                            int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                            int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                            currentMap.enemyMap[gridX][gridY] = enemyTile;
                            currentMap.path[gridX][gridY] = new List<System.Numerics.Vector2>();
                        }
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        string json = JsonConvert.SerializeObject(currentMap);
                        string filePath = $"Content/maps/{level}.json";
                        System.IO.File.WriteAllText(filePath, json);
                        Console.WriteLine($"Saved map to {filePath}");
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.R)){
                        if(enemyPlacementMode){
                            float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                            float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                            int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                            int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                            if(selectedTank==new System.Numerics.Vector2(-150, -150)){
                                selectedTank = new System.Numerics.Vector2(gridX, gridY);
                                Console.WriteLine($"Selected tank {selectedTank}");
                                Thread.Sleep(100);
                            } else {
                                if(selectedTank==new System.Numerics.Vector2(gridX, gridY)){
                                    selectedTank = new System.Numerics.Vector2(-150, -150);
                                    Console.WriteLine($"Deselected tank");
                                    Thread.Sleep(100);
                                } else {
                                    currentMap.path[(int)selectedTank.X][(int)selectedTank.Y].Add(new System.Numerics.Vector2((gridX * 20 ) + 10, (gridY * 20) + 10));
                                    Console.WriteLine($"Added tile {gridX}, {gridY} to tank {selectedTank.X}, {selectedTank.Y}, full path is now");
                                    Console.WriteLine("{Starting Position}");
                                    for(int i = 0; i < currentMap.path[(int)selectedTank.X][(int)selectedTank.Y].Count; i++){
                                        Console.WriteLine($"{currentMap.path[(int)selectedTank.X][(int)selectedTank.Y][i]}");
                                    }
                                    Thread.Sleep(100);
                                }
                            }
                        }
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.D)){
                        decorationLayer++;
                        if(decorationLayer > 2) decorationLayer = 0;
                        Console.WriteLine($"Decoration layer set to {decorationLayer}");
                        Thread.Sleep(100);
                    }
                    if(Keyboard.GetState().IsKeyDown(Keys.Delete)){
                        float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
                        float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;
                        int gridX = (int)(Mouse.GetState().X / (20 * scaleX));
                        int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));
                        switch(decorationLayer){
                            case 0:
                                mapTexture[gridX][gridY] = null;
                                currentMap.tilePath[gridX][gridY] = null;
                                break;
                            case 1:
                                mapDecoBackTexture[gridX][gridY] = null;
                                currentMap.decoBack[gridX][gridY] = null;
                                break;
                            case 2:
                                mapDecoFrontTexture[gridX][gridY] = null;
                                currentMap.decoFront[gridX][gridY] = null;
                                break;
                        }
                    }
                } catch {
                    Console.WriteLine("Invalid input");
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            float scaleX = 4 * (float)_graphics.PreferredBackBufferWidth / GraphicsDevice.Viewport.Width;
            float scaleY = 4 * (float)_graphics.PreferredBackBufferHeight / GraphicsDevice.Viewport.Height;

            // Calculate the scaled destination rectangle
            Rectangle playerRectangle = new Rectangle(
                (int)(playerPosition.X * scaleX),
                (int)(playerPosition.Y * scaleY),
                (int)(playerWheelTextures[playerWheelTextureslider].Width * scaleX),
                (int)(playerWheelTextures[playerWheelTextureslider].Height * scaleY)
            );

            // Calculate the rotation origin point as the center of the texture
            System.Numerics.Vector2 origin = new System.Numerics.Vector2(playerWheelTextures[playerWheelTextureslider].Width / 2, playerWheelTextures[playerWheelTextureslider].Height / 2);

            // Convert rotation to radians
            float rotationRadians = MathHelper.ToRadians(rotation);

            // Start drawing
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, null);
            if(!tileSelectorMode){
                for(int i = 0; i < 10; i++){
                    for(int j = 0; j < 10; j++){
                        if(mapTexture[i][j] == null) continue;
                        Rectangle mapRectangle = new Rectangle(
                            (int)(i * 20 * scaleX) + 32,
                            (int)(j * 20 * scaleY) + 32,
                            (int)(20 * scaleX),
                            (int)(20 * scaleY)
                        );
                        _spriteBatch.Draw(
                            mapTexture[i][j],
                            mapRectangle,
                            null,
                            Color.White,
                            0, // Pass the rotation angle in radians
                            origin, // Set the rotation origin point
                            SpriteEffects.None,
                            0
                        );
                    }
                }
                for(int i = 0; i < 10; i++){
                    if(mapDecoFrontTexture[i] == null) continue;
                    for(int j = 0; j < 10; j++){
                        if(mapDecoBackTexture[i][j] == null) continue;
                        Rectangle mapRectangle = new Rectangle(
                            (int)(i * 20 * scaleX) + 32,
                            (int)(j * 20 * scaleY) + 32,
                            (int)(20 * scaleX),
                            (int)(20 * scaleY)
                        );
                        _spriteBatch.Draw(
                            mapDecoBackTexture[i][j],
                            mapRectangle,
                            null,
                            Color.White,
                            0, // Pass the rotation angle in radians
                            origin, // Set the rotation origin point
                            SpriteEffects.None,
                            0
                        );
                    }
                }
            } else {
                for(int i = 0; i < 10; i++){
                    for(int j = 0; j < 10; j++){
                        Rectangle mapRectangle = new Rectangle(
                            (int)(i * 20 * scaleX) + 32,
                            (int)(j * 20 * scaleY) + 32,
                            (int)(20 * scaleX),
                            (int)(20 * scaleY)
                        );
                        if(tileSelectorTiles[i][j] == null) continue;
                        _spriteBatch.Draw(
                            tileSelectorTiles[i][j],
                            mapRectangle,
                            null,
                            Color.White,
                            0, // Pass the rotation angle in radians
                            origin, // Set the rotation origin point
                            SpriteEffects.None,
                            0
                        );
                    }
                }
            }
            if (mapMakerMode)
            {
                // Calculate the grid position based on mouse coordinates
                int gridX = (int)(Mouse.GetState().X / (20 * scaleX)); // Adjust the 20 based on your grid size
                int gridY = (int)(Mouse.GetState().Y / (20 * scaleY));

                // Calculate the position for the ghost tile
                int ghostX = gridX * (int)(20 * scaleX) + 32;
                int ghostY = gridY * (int)(20 * scaleY) + 32;

                Rectangle ghostRectangle = new Rectangle(ghostX, ghostY, (int)(20 * scaleX), (int)(20 * scaleY));

                if(invisibleObjectMode){
                    // Draw invisible objects 
                    for(int i = 0; i < 10; i++){
                        for(int j = 0; j < 10; j++){
                            if(currentMap.mapObject[i][j] == 0) continue;
                            Rectangle mapRectangle = new Rectangle(
                                (int)(i * 20 * scaleX) + 32,
                                (int)(j * 20 * scaleY) + 32,
                                (int)(20 * scaleX),
                                (int)(20 * scaleY)
                            );
                            _spriteBatch.Draw(
                                signs[currentMap.mapObject[i][j]],
                                mapRectangle,
                                null,
                                Color.White,
                                0, // Pass the rotation angle in radians
                                origin, // Set the rotation origin point
                                SpriteEffects.None,
                                0
                            );
                        }
                    }
                }

                _spriteBatch.Draw(
                    GhostTileTexture,
                    ghostRectangle,
                    null,
                    Color.White,
                    0, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );
            } else {
                for(int i = 0; i < playerTrails.Count; i++){
                    Rectangle trailRectangle = new Rectangle(
                        (int)(playerTrails[i].X * scaleX),
                        (int)(playerTrails[i].Y * scaleY),
                        (int)(trailTexture.Width * scaleX),
                        (int)(trailTexture.Height * scaleY)
                    );
                    _spriteBatch.Draw(
                        trailTexture,
                        trailRectangle,
                        null,
                        Color.White,
                        (float)playerRotationTrails[i], // Pass the rotation angle in radians
                        origin, // Set the rotation origin point
                        SpriteEffects.None,
                        0
                    );
                }
                for(int i = 0; i < playerBullets.Count; i++){
                    Rectangle bulletRectangle = new Rectangle(
                        (int)(playerBullets[i][0] * scaleX),
                        (int)(playerBullets[i][1] * scaleY),
                        (int)(bulletTexture.Width * scaleX),
                        (int)(bulletTexture.Height * scaleY)
                    );
                    _spriteBatch.Draw(
                        bulletTexture,
                        bulletRectangle,
                        null,
                        Color.White,
                        (float)playerBullets[i][2], // Pass the rotation angle in radians
                        origin, // Set the rotation origin point
                        SpriteEffects.None,
                        0
                    );
                }
                _spriteBatch.Draw(
                    playerWheelTextures[playerWheelTextureslider],
                    playerRectangle,
                    null,
                    Color.White,
                    rotationRadians, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );
                _spriteBatch.Draw(
                    playerHullTexture,
                    playerRectangle,
                    null,
                    Color.White,
                    rotationRadians, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );
                if(canonHot == 0){
                    _spriteBatch.Draw(
                    playerCanonTexture,
                    playerRectangle,
                    null,
                    Color.White,
                    canonRotation, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                    );
                } else {
                    _spriteBatch.Draw(
                    playerHotCanonTexture,
                    playerRectangle,
                    null,
                    Color.White,
                    canonRotation, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                    );
                }
                
                _spriteBatch.Draw(
                    playerTopTexture,
                    playerRectangle,
                    null,
                    Color.White,
                    canonRotation, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );

                // Draw enemy bullets
                for(int i = 0; i < enemies.Count; i++){
                    for(int j = 0; j < enemies[i].bullets.Count; j++){
                        Rectangle bulletRectangle = new Rectangle(
                            (int)(enemies[i].bullets[j][0] * scaleX),
                            (int)(enemies[i].bullets[j][1] * scaleY),
                            (int)(bulletTexture.Width * scaleX),
                            (int)(bulletTexture.Height * scaleY)
                        );
                        _spriteBatch.Draw(
                            bulletTexture,
                            bulletRectangle,
                            null,
                            Color.White,
                            (float)enemies[i].bullets[j][2], // Pass the rotation angle in radians
                            origin, // Set the rotation origin point
                            SpriteEffects.None,
                            0
                        );
                    }
                }
            }
            
            // Draw enemies
            for(int i = 0; i < enemies.Count; i++){ 
                //degrees to radians
                double enemyRotationRadians = enemies[i].rotation * (Math.PI / 180);

                Rectangle enemyRectangle = new Rectangle(
                    (int)(enemies[i].Position.X * scaleX),
                    (int)(enemies[i].Position.Y * scaleY),
                    (int)(playerWheelTextures[enemies[i].wheelTextureSlider].Width * scaleX),
                    (int)(playerWheelTextures[enemies[i].wheelTextureSlider].Height * scaleY)
                );
                for(int j = 0; j < enemies[i].trails.Count; j++){
                    Rectangle trailRectangle = new Rectangle(
                        (int)(enemies[i].trails[j].X * scaleX),
                        (int)(enemies[i].trails[j].Y * scaleY),
                        (int)(trailTexture.Width * scaleX),
                        (int)(trailTexture.Height * scaleY)
                    );

                    _spriteBatch.Draw(
                        trailTexture,
                        trailRectangle,
                        null,
                        Color.White,
                        (float)enemies[i].rotationTrails[j], // Pass the rotation angle in radians
                        origin, // Set the rotation origin point
                        SpriteEffects.None,
                        0
                    );
                }
                _spriteBatch.Draw(
                    playerWheelTextures[enemies[i].wheelTextureSlider],
                    enemyRectangle,
                    null,
                    Color.White,
                    (float)enemyRotationRadians, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );
                _spriteBatch.Draw(
                    enemies[i].hullTexture,
                    enemyRectangle,
                    null,
                    Color.White,
                    (float)enemyRotationRadians, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );
                if(enemies[i].canonHot == 0){
                    _spriteBatch.Draw(
                    playerCanonTexture,
                    enemyRectangle,
                    null,
                    Color.White,
                    enemies[i].canonRotation, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                    );
                } else {
                    _spriteBatch.Draw(
                    playerHotCanonTexture,
                    enemyRectangle,
                    null,
                    Color.White,
                    enemies[i].canonRotation, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                    );
                }
                
                _spriteBatch.Draw(
                    enemies[i].topTexture,
                    enemyRectangle,
                    null,
                    Color.White,
                    enemies[i].canonRotation, // Pass the rotation angle in radians
                    origin, // Set the rotation origin point
                    SpriteEffects.None,
                    0
                );
            }

            if(!tileSelectorMode && !invisibleObjectMode && !enemyPlacementMode){
                for(int i = 0; i < 10; i++){
                    if(mapDecoFrontTexture[i] == null) continue;
                    for(int j = 0; j < 10; j++){
                        if(mapDecoFrontTexture[i][j] == null) continue;
                        Rectangle mapRectangle = new Rectangle(
                            (int)(i * 20 * scaleX) + 32,
                            (int)(j * 20 * scaleY) + 32,
                            (int)(20 * scaleX),
                            (int)(20 * scaleY)
                        );
                        _spriteBatch.Draw(
                            mapDecoFrontTexture[i][j],
                            mapRectangle,
                            null,
                            Color.White,
                            0, // Pass the rotation angle in radians
                            origin, // Set the rotation origin point
                            SpriteEffects.None,
                            0
                        );
                    }
                }
            }
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}