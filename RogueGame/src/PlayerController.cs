using System;
using libtcod;
using RogueGame.Components;
using RogueGame.Interface;

namespace RogueGame {
    
    /// <summary>
    /// Sends events and controlls the player entity via inputs.
    /// Players actions drive the rest of simulation
    /// </summary>
    public class PlayerController {
        
        /// <summary>
        /// Tile cursor represents where the cursor where the player is 'aiming' 
        /// </summary>
        public struct TileCursor {
            private PlayerController owner;
            public Vector2 position;
            public bool hasLOS;

            public bool IsActive {
                get { return owner.inputState == EPlayerInputState.TileSelect; }
            }

            public TileCursor(PlayerController owner) {
                this.owner = owner;
                position = new Vector2();
                hasLOS = false;
            }
        }

        public enum EPlayerInputState {
            Normal,             // Normal moving around
            TileSelect,         // Selecting a tile from the map
            UseHUD,             // Inputs fed to the active HUD element
        }
        


        // Key controls: TODO: move to some configuration 
        public const TCODKeyCode KEY_UP = TCODKeyCode.Up;
        public const char Char_UP = 'w';
        public const TCODKeyCode KEY_LEFT = TCODKeyCode.Left;
        public const char Char_LEFT = 'a';
        public const TCODKeyCode KEY_RIGHT = TCODKeyCode.Right;
        public const char Char_RIGHT = 'd';
        public const TCODKeyCode KEY_DOWN = TCODKeyCode.Down;
        public const char Char_DOWN = 's';
        public const TCODKeyCode KEY_SELECT = TCODKeyCode.Enter;
        public const TCODKeyCode KEY_INSPECT = TCODKeyCode.Space;
        public const char KEY_GRAB_ITEM = 'g';

        
        public Entity player;
        public Camera camera;
        private Engine engine;

        private bool canMove = true;
        private bool doMouseLook = true;
        private EPlayerInputState inputState;
        public TileCursor cursor;
        public HUD hud;

        public PlayerController(Entity player) {
            this.player = player;
            engine = Engine.instance;
            camera = engine.mainCamera;

            inputState = EPlayerInputState.Normal;
            cursor = new TileCursor(this);
            hud = new HUD(this);            
        }
        
        public void ReInitController(Entity newPlayer) {
            player = newPlayer;
            foreach(var window in hud.windows) {
                window.CleanupWindow();
            }

            hud = new HUD(this);
            inputState = EPlayerInputState.Normal;
        }


        /// <summary>
        /// Called at whetever the frame rate is
        /// </summary>
        public void Update() {
            switch(inputState) {
                case EPlayerInputState.Normal:
                    NormalUpdate();
                    break;
                case EPlayerInputState.TileSelect:
                    TileSelectUpdate();
                    break;
                case EPlayerInputState.UseHUD:
                    UseHUDUpdate();
                    break;
            }

            hud.Update();
        }

        /// <summary>
        /// Normal player update, for moving around attacking and interacting with gameplay objects
        /// </summary>
        private void NormalUpdate() {
            Vector2 moveVector = InputVector();
            bool didAction = false;

            // only attempts to move when there is a non zero input
            if(!(moveVector.x == 0 && moveVector.y == 0) && canMove) {
                Vector2 prevPosition = player.position;
                
                player.FireEvent(new EMove() { direction = moveVector });

                // in case the player moved, update the maps cached LOS
                if(player.position != prevPosition) {
                    camera.position = player.position;
                }

                didAction = true;
            }
            
            // switch to inspect state
            if(engine.lastKey.KeyCode == KEY_INSPECT) {
                GotoInputState(EPlayerInputState.TileSelect);
            }

            // pickup an item: if there is one item its easy and is auto grabbed
            if(engine.lastKey.Character == KEY_GRAB_ITEM) {
                Entity[] objects = engine.world.currentMap.GetAllObjectsAt(player.position.X, player.position.Y);

                // one item (other than player)
                if(objects.Length == 2 && (objects[0] != player || objects[1] != player)) {
                    int idx = objects[0] != player ? 0 : 1;

                    player.FireEvent(new EAquireItem() { item = objects[idx] });
                    didAction = true;
                }
                else if(objects.Length > 2) {
                    // If there is multiple items, go to the pickup item window
                    hud.OpenPickupWindow(GrabItemActionCompleted);
                    GotoInputState(EPlayerInputState.UseHUD);
                }
            }

            // selection key looks for activator objects to react
            if(engine.lastKey.KeyCode == KEY_SELECT) {
                Entity[] objects = engine.world.currentMap.GetAllObjectsAt(player.position.X, player.position.Y);

                // tell all the objects that the player wants to activate them
                foreach(var obj in objects) {
                    if(obj != player) {
                        obj.FireEvent(new EActivate() { instigator = player });
                    }
                }
            }

            // check for number keys, which are reserved for activating skills
            int numberKeyPressed;
            if(int.TryParse("" + engine.lastKey.Character, out numberKeyPressed)) {
                player.FireEvent(new ERequestUseSkill() { slot = (numberKeyPressed - 1) });
            }
            
            // if any action was performed, do a cycle of turns for all other turn takers
            if(didAction) {
                PerformPlayerTurn();
            }
        }


        /// <summary>
        /// Update for the tile selection state
        /// </summary>
        private void TileSelectUpdate() {
            if(doMouseLook) {
                cursor.position.x = engine.mouseData.CellX;
                cursor.position.y = engine.mouseData.CellY;
            }
            else {
                Vector2 deltaCursor = InputVector();
                cursor.position += deltaCursor;
            }

            // update the los state of the pointed to location
            var map = engine.world.currentMap;
            Vector2 worldPos = Renderer.ScreenToWorldCoord(camera, cursor.position);
            if(map.InBounds(worldPos.X, worldPos.Y)) {                
                cursor.hasLOS = map.GetTile(worldPos.X, worldPos.Y).cachedLOS;
            }
            else {
                cursor.hasLOS = false;
            }
            
            // out of inspect mode
            if(engine.lastKey.KeyCode == KEY_INSPECT) {
                GotoInputState(EPlayerInputState.Normal);
            }
            else if(engine.mouseData.LeftButtonPressed && cursor.hasLOS) { // attack the tile, then go to normal inputs
                Entity[] hits = map.GetAllObjectsAt(worldPos.X, worldPos.Y);
                foreach(Entity e in hits) {                   
                    player.FireEvent(new EDoAttack() { target = e });                    
                }

                PerformPlayerTurn();

                GotoInputState(EPlayerInputState.Normal);
            }
        }


        private void UseHUDUpdate() {
            // delegate updating to the huds currently open window
            hud.UpdateActiveDynamicWindow();
        }

        /// <summary>
        /// Sends the player controller into the new input state, changing the behaviour of the controller
        /// </summary>
        public void GotoInputState(EPlayerInputState newState) {
            if(newState == inputState) {
                return;
            }

            inputState = newState;

            // Update the cursor location to be the players location
            if(newState == EPlayerInputState.TileSelect) {
                cursor.position = player.position;
            }
        }

        /// <summary>
        /// Gets the input direction of the last key
        /// </summary>
        private Vector2 InputVector() {
            Vector2 inputVector = new Vector2();

            if(engine.lastKey.KeyCode == KEY_RIGHT || engine.lastKey.Character == Char_RIGHT) {
                inputVector.x = 1;
            }

            if(engine.lastKey.KeyCode == KEY_LEFT || engine.lastKey.Character == Char_LEFT) {
                inputVector.x = -1;
            }

            if(engine.lastKey.KeyCode == KEY_DOWN || engine.lastKey.Character == Char_DOWN) {
                inputVector.y = 1;
            }

            if(engine.lastKey.KeyCode == KEY_UP || engine.lastKey.Character == Char_UP) {
                inputVector.y = -1;
            }

            return inputVector;
        }

        /// <summary>
        /// Initiates the engine to cycle entity turns, starting with the player
        /// </summary>
        private void PerformPlayerTurn() {
            engine.world.ProcessTurns();
            UpdatePlayerLOS();
        }

        private void UpdatePlayerLOS() {
            // request the sight range dynamically, so that components can determine it
            var sight = (EGetStat_SightRange)player.FireEvent(new EGetStat_SightRange());

            //TODO: light level modifier from currentMap
            engine.world.currentMap.UpdateLOSFrom(player.position, sight.range);
        }

        /// <summary>
        /// Called when the grab item hud interaction is completed
        /// </summary>
        public void GrabItemActionCompleted(object data) {
            Entity obj = (Entity)data;
            if(obj != null && player != null && obj != player) {
                player.FireEvent(new EAquireItem() { item = obj });
            }

            GotoInputState(EPlayerInputState.Normal);
        }


    }
}
