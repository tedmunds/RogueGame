using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Components;

namespace RogueGame.Interface {
    /// <summary>
    /// Game menu is a specialization of Window that assumes it does not belong to a HUD object, and is full screen
    /// </summary>
    public class WindowGameMenu : Window{

        protected delegate void HoverHandler(Hoverable hovered, int y, int x);

        protected class Hoverable {
            public Entity entity;
            public Vector2 origin;
            public int width;
            public HoverHandler handler;
        }

        protected struct SubSection {
            public Vector2 origin;
            public Vector2 size;
        }


        /// <summary>
        /// Internal drag drop 
        /// </summary>
        public HUD.DragDrop dragdrop;

        // A list of targets that the mouse should hover over and open a thing
        protected List<Hoverable> hoverableItems;
        protected List<Button> buttons;

        public WindowGameMenu(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            borderStyle = EBorderStyle.NONE;
        }



        public override void OnDestroy() {
            base.OnDestroy();
        }


        public virtual List<Window> GetSubMenus() {
            List<Window> subWindows = new List<Window>();
            return subWindows;
        }



        public override void InitializeConsole() {
            base.InitializeConsole();
            buttons = new List<Button>();
            hoverableItems = new List<Hoverable>();
        }


        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();

            // update and handle inpupts on buttons
            foreach(Button b in buttons) {
                b.Update(engine.mouseData);
                b.Draw(this);
            }
            
            // handle drag drops
            if(dragdrop.active) {
                dragdrop.x = engine.mouseData.CellX;
                dragdrop.y = engine.mouseData.CellY - 1;

                if(engine.mouseData.LeftButtonPressed) {
                    DropDragObject();
                }
            }

            // Handle all hoverable items
            Vector2 mLoc = ScreenToConsoleCoord(engine.mouseData.CellX, engine.mouseData.CellY);
            foreach(Hoverable hoverable in hoverableItems) {
                int hoverX = hoverable.origin.X;
                int hoverY = hoverable.origin.Y;

                var getName = (EGetScreenName)hoverable.entity.FireEvent(new EGetScreenName());

                DrawText(hoverable.origin.X, hoverable.origin.Y, "+" + getName.text, foregroundColor);

                // the mouse is hovering over this hover area, so highlight the background and open inspector window. Also handles clicks
                if(mLoc.X >= hoverX && mLoc.X < hoverX + hoverable.width &&
                    mLoc.Y == hoverY) {
                    DrawRect(hoverX, hoverY, hoverable.width, Constants.COL_FRIENDLY, false);

                    // always call the hover handler, which opens the appropriate inspector window
                    hoverable.handler(hoverable, hoverX + hoverable.width, mLoc.Y);
                    OnItemHovered(hoverable);
                }
                else {
                    // reset it to rect the background color
                    DrawRect(hoverX, hoverY, hoverable.width, backgroundColor, false);
                }
            }
            
            return console;
        }


        /// <summary>
        /// Called when the supplied hoverable gets hovered over
        /// </summary>
        protected virtual void OnItemHovered(Hoverable hovered) {

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
            
            dragdrop.dropHandler?.Invoke();
            
            dragdrop.data = null;
            dragdrop.text = "";
            dragdrop.active = false;
        }

        /// <summary>
        /// Returns true if the input mouse location is inside the input subsection
        /// </summary>
        protected bool IsMouseInSubSection(SubSection subSection, Vector2 mLoc) {
            if(mLoc.X >= subSection.origin.X && mLoc.X < subSection.origin.X + subSection.size.X &&
                mLoc.Y >= subSection.origin.Y && mLoc.Y < subSection.origin.Y + subSection.size.Y) {
                return true;
            }

            return false;
        }



    }
}
