using System;
using libtcod;
using RogueGame.Components;
using RogueGame.Map;

namespace RogueGame.Interface {


    public class WindowPickupItem : WindowInspection {

        public HUD.HUDCallback onPickup;

        public WindowPickupItem(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
        }


        public override void InitializeConsole() {
            base.InitializeConsole();

            DrawText(console.getWidth() - 13, console.getHeight() - 1, "[esc] close", Constants.COL_NORMAL);
        }

        
        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();

            // reinit b/c mouse over will constantly change the background
            InitializeConsole();

            // highlight the item the mouse is over
            Vector2 mloc = ScreenToConsoleCoord(engine.mouseData.CellX, engine.mouseData.CellY);
            
            if(mloc.X > 0 && mloc.X < console.getWidth() && 
                mloc.Y > 0 && mloc.Y < top + inspectTile.entities.Count) {
                // Get the entity that is being highlighted
                Entity entity = inspectTile.entities[mloc.Y - top];

                // check if the object can be picked up at all, and choose the color to draw the highlight
                ECanPickup carryEvent = (ECanPickup)entity.FireEvent(new ECanPickup {
                    asker = player
                });
                
                TCODColor highlightColor = carryEvent.canPickup ? Constants.COL_FRIENDLY : highlightColor = Constants.COL_ANGRY;

                DrawRect(1, mloc.Y, console.getWidth() - 3, highlightColor);

                // get the screen name to draw on top
                var getName = (EGetScreenName)entity.FireEvent(new EGetScreenName());
                string screenName = getName.text;

                DrawText(left, mloc.Y, screenName, TCODColor.white);

                // on mouse pressed
                if(engine.mouseData.LeftButtonPressed && carryEvent.canPickup) {
                    onPickup(entity);
                }
            }

            return console;
        }

    }
}
