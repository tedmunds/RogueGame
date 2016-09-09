using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;

namespace RogueGame.Interface {

    /// <summary>
    /// Manages a set of static windows that display info, and also a dynamic window that can be opened and closed
    /// </summary>
    public class HUD {
        public delegate void HUDCallback(object parameter);
        
        /// <summary>
        /// Stores the data for dragging and dropping an object
        /// </summary>
        public struct DragDrop {
            public bool active;
            public int x;
            public int y;
            public string text;
            public object data;
            public Action dropHandler;
        }

        /// <summary>
        /// Data for a circle showing how far the player can attack / detect etc. 
        /// </summary>
        public struct RangeIndicator {
            public bool active;
            public float range;
        }
        
        private static TCODColor cursorColor = new TCODColor(120, 155, 191);

        private Entity player;
        Engine engine;
        World world;
        private PlayerController controller;

        public List<Window> windows;

        public WindowInspection inspectionWindow;
        public WindowPickupItem pickupWindow;
        public WindowLog logWindow;
        public WindowEnemyHealth enemyHealthWindow;

        /// <summary>
        /// The dynamic, open window that has focus currently
        /// </summary>
        public Window openWindow;
        private HUDCallback openWindowClosed;

        /// <summary>
        /// One thing can be drag dropped on the hud at a time
        /// </summary>
        public DragDrop dragdrop;

        public RangeIndicator rangeIndicator;

        public HUD(PlayerController controller) {
            this.controller = controller;
            player = controller.player;
            engine = Engine.instance;
            world = engine.world;

            // create the base windows
            windows = new List<Window>();

            windows.Add(new WindowPlayerState(0, 0, 
                20, engine.consoleHeight, 
                "[status]", Window.EBorderStyle.ALL, player, this));     // left menu

            windows.Add(new WindowInventory(engine.consoleWidth - 20, 0, 
                20, engine.consoleHeight, 
                "[inventory]", Window.EBorderStyle.ALL, player, this));  // right menu

            logWindow = new WindowLog(20, engine.consoleHeight - 10, 
                engine.consoleWidth - 40, 10, 
                "[log]", Window.EBorderStyle.NORTH, player, this);  // bottom (console) menu
            windows.Add(logWindow);

            enemyHealthWindow = new WindowEnemyHealth(20, 0, // top menu
                engine.consoleWidth - 40, 2,
                "", Window.EBorderStyle.SOUTH, player, this);  // very small top bar for showing last enemy hit health and basic stats
            windows.Add(enemyHealthWindow);

            inspectionWindow = new WindowInspection(0, 0, 36, 12, "[inspect]", Window.EBorderStyle.ALL, player, this);            
            windows.Add(inspectionWindow);

            pickupWindow = new WindowPickupItem(engine.consoleWidth / 2 - 16, engine.consoleHeight / 2 - 6, 36, 12, 
                "[pickup]", Window.EBorderStyle.ALL, player, this);
            windows.Add(pickupWindow);

            openWindow = null;
            rangeIndicator.active = false;
            rangeIndicator.range = 5.0f;
        }


        public void Update() {
            if(dragdrop.active) {
                dragdrop.x = engine.mouseData.CellX;
                dragdrop.y = engine.mouseData.CellY - 1;

                if(engine.mouseData.LeftButtonPressed) {
                    DropDragObject();
                }
            }
        }


        /// <summary>
        /// updates and blits the window consoles
        /// </summary>
        public void Draw(TCODConsole baseConsole, Camera camera) {
            if(controller != null) {
                var tileCursor = controller.cursor;

                DrawTileCursor(baseConsole, tileCursor, camera);
            } 

            // draw the range indicatar out from the players position
            if(rangeIndicator.active) {
                Renderer.DrawCircle(baseConsole, 
                    Renderer.WorldToScreenCoord(camera, player.position), 
                    rangeIndicator.range, 
                    Constants.COL_DARK_GREEN);
            }
        }


        // Draw the tile cursor, and inspection window if there are entites under the tile
        private void DrawTileCursor(TCODConsole baseConsole, PlayerController.TileCursor tileCursor, Camera camera) {
            if(tileCursor.IsActive) {
                Vector2 start = Renderer.WorldToScreenCoord(camera, player.position);
                Vector2 end = tileCursor.position;
                Vector2 inspectPos = Renderer.ScreenToWorldCoord(camera, tileCursor.position);

                // never draw it if cursor is not on map
                if(!world.currentMap.InBounds(inspectPos)) {
                    return;
                }

                // change color depending on if the player has los to the endpoint
                TCODColor lineColor = tileCursor.hasLOS ? Constants.COL_FRIENDLY : Constants.COL_ANGRY;

                Renderer.DrawLine(baseConsole, start, end, lineColor);
                baseConsole.setCharBackground(end.X, end.Y, cursorColor);

                // decide if there is anything to inspect on this tile
                Entity[] entities = world.currentMap.GetAllObjectsAt(inspectPos.X, inspectPos.Y);

                // if there is stuff to inspect, and the inspect window is not open or is initialized for a different tile
                if(entities.Length > 0 &&
                    (!inspectionWindow.isVisible || inspectionWindow.currentLocation != inspectPos)) {
                    // open the window, but make sure its on the opposite side of the where the cursor is so it never overlaps
                    if(tileCursor.position.X < engine.consoleWidth / 2) {
                        inspectionWindow.origin.x = (camera.screenX + camera.width) - inspectionWindow.console.getWidth();
                    }
                    else {
                        inspectionWindow.origin.x = camera.screenX;
                    }

                    var tile = world.currentMap.GetTile(inspectPos.X, inspectPos.Y);

                    // only open the inspector if the target tile is in los
                    if(tile.cachedLOS) {
                        inspectionWindow.OpenForTile(tile, inspectPos);
                    }
                }
                else if(entities.Length == 0 && inspectionWindow.isVisible) {
                    inspectionWindow.Close();
                }
            }
            else {
                // always close inspect window if tile cursor gets turned off
                if(inspectionWindow.isVisible) {
                    inspectionWindow.Close();
                }
            }
        }



        /// <summary>
        /// Sets up a new drag drop data. returns true if it was set. retuns false if there was already a dragdrop set
        /// </summary>
        public bool SetDragDrop(string text, object data, Action onDropHandler) {
            if(dragdrop.data != null) {
                return false;
            }

            dragdrop.x = engine.mouseData.CellX;
            dragdrop.y = engine.mouseData.CellY;

            dragdrop.text = text;
            dragdrop.data = data;
            dragdrop.dropHandler = onDropHandler;
            dragdrop.active = true;
            return true;
        }

        /// <summary>
        /// Drops the current drag drop item
        /// </summary>
        public void DropDragObject() {
            if(dragdrop.data == null) {
                return;
            }

            if(dragdrop.dropHandler != null) {
                dragdrop.dropHandler();
            }

            dragdrop.data = null;
            dragdrop.text = "";
            dragdrop.active = false;
        }


        /// <summary>
        /// Update whatever the open window
        /// </summary>
        public void UpdateActiveDynamicWindow() {
            if(openWindow != null) {
                TCODKeyCode key = engine.lastKey.KeyCode;

                // escape always closes the window
                if(key == TCODKeyCode.Escape) {
                    openWindowClosed(null);
                    openWindow.SetVisible(false);
                }
            }
        }


        /// <summary>
        /// Opens the input window. Whent he window closes, the callback wil be called
        /// </summary>
        public void OpenPickupWindow(HUDCallback closeCallback) {
            openWindow = pickupWindow;
            pickupWindow.OpenForTile(world.currentMap.GetTile(player.position.X, player.position.Y), player.position);
            pickupWindow.SetVisible(true);

            // setup the callback chain: the window will call the huds function which handles generic window closing and callback
            openWindowClosed = closeCallback;
            pickupWindow.onPickup = CloseOpenedWindow;
        }

        /// <summary>
        /// Force closed the currently opened window
        /// </summary>
        public void CloseOpenedWindow(object data) {
            if(openWindow == null) {
                return;
            }

            openWindowClosed(data);
            openWindow.SetVisible(false);
        }

    }
}
