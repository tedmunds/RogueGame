//#define ENTRY_GAME

using System;
using System.Collections.Generic;
using System.Diagnostics;
using libtcod;
using RogueGame.Components;
using RogueGame.Data;
using RogueGame.Interface;

namespace RogueGame {
    public class Engine {

        private const int APPLICATION_EXIT_NORMAL = 0;
        private const int APPLICATION_EXIT_ERROR = -1;
        
        /// <summary>
        /// singleton implementation
        /// </summary>
        public static Engine instance;

        /// <summary>
        /// Defines what state the engines update / render loop are in, defines what things will get updated
        /// </summary>
        public enum EEngineState {
            Menu,
            Game,
            Pause
        }

        public EEngineState engineState;

        private bool gotoNewState = false;
        private EEngineState nextState;

        /// <summary>
        /// The base frame rate for real time simulation
        /// </summary>
        private const int FRAME_RATE = 60;
        private int MS_PER_UPDATE;
        private Stopwatch internalTimer;
        private long previousUpdateTime;
        private long frameLag;

        /// <summary>
        /// The main console the game will be drawn into
        /// </summary>
        private TCODConsole mainConsole;
        public int consoleWidth;
        public int consoleHeight;

        private bool isRunning;

        /// <summary>
        /// Cached input references, set at start of frame
        /// </summary>
        public TCODKey lastKey;
        public TCODMouseData mouseData;
        
        /// <summary>
        /// Camera is used by the renderer to draw the scene to the main console
        /// </summary>
        public Camera mainCamera;

        /// <summary>
        /// Area manager manages the world state
        /// </summary>
        public World world;

        /// <summary>
        /// The window that is open a menu. Null if t he engine state is not MENU
        /// </summary>
        public WindowGameMenu currentMenuContext;

        /// <summary>
        /// Data manger is used for instantiating new entities from prototypes
        /// </summary>
        public DataManager data;

        /// <summary>
        /// Engine renderer updates the main console based on game state before libtcod draws it
        /// </summary>
        public Renderer renderer;

        public PlayerController playerController;

        /// <summary>
        /// Data table system, contains all non entity data in a tiny database format
        /// </summary>
        public TableManager dataTables;

        // set of windows that have been destroyed and need to be cleaned up
        private List<Window> destroyedWindows = new List<Window>();


        public Engine(TCODConsole mainConsole) {
            instance = this;
            isRunning = false;

            this.mainConsole = mainConsole;
            consoleWidth = mainConsole.getWidth();
            consoleHeight = mainConsole.getHeight();

            // Frame rate intialization
            TCODSystem.setFps(FRAME_RATE);
            internalTimer = new Stopwatch();
            MS_PER_UPDATE = 1000 / FRAME_RATE;

            renderer = new Renderer();
            mainCamera = new Camera(20, 2, consoleWidth - 40, consoleHeight - 12);

            // Load blueprint data files
            data = new DataManager();
            data.LoadContent();

            // load data tables
            dataTables = new TableManager();
            dataTables.LoadData(TableManager.LoadDataFileList("DataTables.dir"));
            
#if ENTRY_GAME
            StartNewGame(null);
#else
            OpenMenu(new WindowMainMenu(0, 0, consoleWidth, consoleHeight, null, Window.EBorderStyle.NONE, null, null));
#endif
        }

        
        /// <summary>
        /// Initializes everything needed for new game and goes into the game state
        /// </summary>
        public void StartNewGame(PlayerCharacterInitializer characterInitializer) {
            Console.WriteLine("Engine::StartNewGame() @ " + internalTimer.Elapsed);

            world = new World();
            world.InitializeNewLevel();
            if(characterInitializer != null) {
                characterInitializer.ApplyToPlayer(world.player);
            }
            
            playerController = new PlayerController(world.player);
            
            GotoState(EEngineState.Game);
        }


        /// <summary>
        /// Puts the enigine into the new state causing it to change upadate / render sate and mode
        /// Does not actually transition until this update cycle is over
        /// </summary>
        public void GotoState(EEngineState newState) {
            gotoNewState = true;
            nextState = newState;
        }

        
        // Called internally when the new transition actually happens, at the start of the frame AFTER the Goto state was called
        private void EnteredNewState(EEngineState newState) {
            engineState = newState;
        }



        /// <summary>
        /// Forces the game to exit safeley
        /// </summary>
        public void Exit() {
            isRunning = false;
            //TODO: saving gamestate and stuff
        }

        
        /// <summary>
        /// Starts the game simulation and set off the normal update life cycle
        /// </summary>
        /// <returns> A code corresponding to how the game exited: 0 = normal quit </returns>
        public int Run() {
            if(mainConsole == null || isRunning) {
                return APPLICATION_EXIT_ERROR;
            }

            isRunning = true;
            internalTimer.Start();

            // Main update loop
            while(!TCODConsole.isWindowClosed() && isRunning) {
                long current = internalTimer.ElapsedMilliseconds;
                long frameTime = current - previousUpdateTime;
                previousUpdateTime = current;

                frameLag += frameTime;

                // Actual update / console drawing runs on an inner catchup loop that is also interuptable
                // will run catchup frames or delay so that it closely matches desired FRAME_RATE, that 
                // is also synced with the TCODConsoles own render frame rate
                while(frameLag >= MS_PER_UPDATE
                      && !TCODConsole.isWindowClosed() && isRunning) {

                    // TCOD must be flushed immedietly before polling the input channels
                    TCODConsole.flush();
                    lastKey = TCODConsole.checkForKeypress((int)TCODKeyStatus.KeyPressed);
                    mouseData = TCODMouse.getStatus();

                    // Handle transitions between states
                    if(gotoNewState) {
                        EnteredNewState(nextState);
                        gotoNewState = false;
                    }

                    Update();
                    Render();

                    // always allow for core update interrupt
                    if(lastKey.KeyCode == TCODKeyCode.F4) {
                        isRunning = false;
                    }
                }
            }

            internalTimer.Stop();
            return APPLICATION_EXIT_NORMAL;
        }


        /// <summary>
        /// Real time update loop:
        /// Updates game at a fixed frame rate set by FRAME_RATE
        /// </summary>
        private void Update() {
            // Perform an update cycle
            switch(engineState) {
                case EEngineState.Menu:
                    
                    break;
                case EEngineState.Game:
                    // Updates the player controller 
                    playerController.Update();
                    
                    // TEMP: better saving menu and stuff, maybe keep quicksave though
                    if(lastKey.KeyCode == TCODKeyCode.F5) {
                        GameSave.SaveGamestate(world, "TestSave.rsv");
                    }
                    if(lastKey.KeyCode == TCODKeyCode.F9) {
                       GameSave.LoadGameSave(this, ref world, "TestSave.rsv");
                    }

                    break;
                case EEngineState.Pause:

                    break;
                default:
                    break;
            }

            // clean up trailing windows
            for(int i = destroyedWindows.Count - 1; i >= 0; i--) {
                destroyedWindows[i].OnDestroy();
                destroyedWindows.RemoveAt(i);
            }
        }



        /// <summary>
        /// Runs at arbitrary rate: renders everything with a render component.
        /// NOTE: render does not actually draw anything to the window, it only fills out console,
        /// The TCODConsole does the actual window drawing using the data set in Render()
        /// </summary>
        private void Render() {
            mainConsole.clear();
            switch(engineState) {
                case EEngineState.Menu:
                    if(currentMenuContext != null) {
                        renderer.DrawMenu(mainConsole, currentMenuContext);
                    }
                    break;
                case EEngineState.Game:
                    renderer.ReDraw(mainConsole, world.currentMap);
                    break;
                case EEngineState.Pause:

                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Puts the input entity into the correct system lists
        /// </summary>
        public void RegisterEntity(Entity e) {
            // sort the render components by their layer each time a new one is added, should be mostly just O(N)
            RenderComponent renderable = e.Get<RenderComponent>();
            if(renderable != null) {
                renderer.drawList.Add(renderable);
                renderer.drawList.Sort(delegate(RenderComponent a, RenderComponent b) {
                    return b.layer.CompareTo(a.layer);
                });
            }
        }

        /// <summary>
        /// Removes the input entity from any system lists
        /// </summary>
        public void UnregisterEntity(Entity e) {
            RenderComponent renderable = e.Get<RenderComponent>();
            if(renderable != null) {
                renderer.drawList.Remove(renderable);
            }
        }
        

        /// <summary>
        /// Opens the window as a full screen window that enters the menu gamestate
        /// </summary>
        public void OpenMenu(WindowGameMenu newWindow) {
            // Get rid of old menu context if there is one
            if(currentMenuContext != null && currentMenuContext != newWindow) {
                currentMenuContext.SetVisible(false);
                currentMenuContext.CleanupWindow();
                currentMenuContext = null;
            }

            currentMenuContext = newWindow;
            GotoState(EEngineState.Menu);
        }

        /// <summary>
        /// Marks teh input window (and libtcod console) to be cleaned up at the end of the next frame
        /// </summary>
        public void DestoyWindow(Window menu) {
            destroyedWindows.Add(menu);
        }


        /// <summary>
        /// Prints the message to the log console area, and saves it in the log
        /// </summary>
        public static void LogMessage(string message, Entity source = null) {
            if(instance != null && instance.playerController != null) {
                Vector2 location = new Vector2(-1, -1);
                if(source != null) {
                    location = source.position;
                }

                instance.playerController.hud.logWindow.LogMessage(message, location);
            }
        }



    }
}
