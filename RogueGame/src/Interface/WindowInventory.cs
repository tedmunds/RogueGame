using System;
using libtcod;
using RogueGame.Components;
using RogueGame.Gameplay;

namespace RogueGame.Interface {
    public class WindowInventory : Window {

        private InvComponent inventory;
        private EquipmentComponent equipment;

        // the window that will be ipened on mouse hover of an item
        private WindowItemInfo itemInfoWindow;

        private struct SubSection {
            public int starty;
            public int endy;
        }
        private SubSection equipSection;
        private SubSection invSection;

        // flag set when the user is dragging an item from or to their inventory. Check parent.dragdrop for details
        private bool draggingItem = false;
        
        public WindowInventory(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {

            itemInfoWindow = new WindowItemInfo(0, 0, 30, 12, "[details]", EBorderStyle.ALL, player, parent);
            parent.windows.Add(itemInfoWindow);
            
            // cache the players inventory: NOTE: this needs to be updated if teh player is destroyed so that the component can be GC'd
            inventory = player.Get<InvComponent>();
            equipment = player.Get<EquipmentComponent>();

            equipSection.starty = 4;
            equipSection.endy = 24;
            invSection.starty = 25;
            invSection.endy = h;
        }


        public override void OnDestroy() {
            base.OnDestroy();
            itemInfoWindow.OnDestroy();
        }


        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();
            
        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();
            const int left = 2;
            int hoveredItemY = -1;
            Entity hoveredItem = null;
            int width = console.getWidth() - (left + 3);
            Vector2 mLoc = ScreenToConsoleCoord(engine.mouseData.CellX, engine.mouseData.CellY);
            
            InitializeConsole();

            // devider line is a horizontal line that can be drawn across the console
            string deviderLine = CharConstants.E_TJOINT + "";
            for(int i = 1; i < console.getWidth() - 1; i++) {
                deviderLine += CharConstants.HORZ_LINE;
            }
            deviderLine += CharConstants.W_TJOINT;

            // inventory weight: TODO
            DrawText(left, 2, " weight: X/X", Constants.COL_FRIENDLY);

            // equipped area -------------------------------------------------------------
            int equiptopY = equipSection.starty;
            DrawText(0, equiptopY, deviderLine, TCODColor.white);
            DrawText(left, equiptopY, " equipped ", Constants.COL_NORMAL);

            if(equipment.weapon != null) {
                EGetScreenName getName = (EGetScreenName)equipment.weapon.FireEvent(new EGetScreenName());
                string weaponName = getName.text;

                // the weapon is being hovered over
                if(mLoc.y == equiptopY + 3 &&
                   mLoc.x >= left && mLoc.x < weaponName.Length + 1 + left) {
                    DrawRect(left, equiptopY + 3, width, Constants.COL_FRIENDLY);
                    hoveredItem = equipment.weapon;
                    hoveredItemY = equiptopY + 3;

                    // was clicked, start dragging it
                    if(engine.mouseData.LeftButtonPressed) {
                        parent.SetDragDrop(weaponName, equipment.weapon, OnDragDroppedEquippedItem);

                        parent.rangeIndicator.range = CombatEngine.GetThrowRange(player, equipment.weapon);
                        draggingItem = true;
                    }
                }

                DrawText(left, equiptopY + 2, "weapon: ", Constants.COL_NORMAL);
                DrawText(left, equiptopY + 3, " " + weaponName, TCODColor.white);
            }
            
            // pack adrea (general unequipped items) -------------------------------------
            int packtopY = invSection.starty;
            
            DrawText(0, packtopY, deviderLine, TCODColor.white);
            DrawText(left, packtopY,  " pack ", Constants.COL_NORMAL);
            
            int line = 1;
            foreach(Entity item in inventory.items) {
                // get the screen name dynamically: TODO: maybe cache this?
                EGetScreenName getName = (EGetScreenName)item.FireEvent(new EGetScreenName());
                string screenName = getName.text;
                
                // check if the mouse is hovering on this item row
                if(mLoc.y == packtopY + line && 
                    mLoc.x >= left && mLoc.x < screenName.Length + 1 + left) {
                    hoveredItem = item;
                    hoveredItemY = packtopY + line;

                    // highlight the item that is hovered on
                    DrawRect(left, packtopY + line, width, Constants.COL_FRIENDLY);
                    
                    // check for clicks on this item, and start dragging it
                    if(engine.mouseData.LeftButtonPressed) {
                        parent.SetDragDrop(screenName, item, OnDragDroppedInvItem);

                        // set the range indicator based on how far the dragged item can be thrown, in case the player dicides to throw its                        
                        parent.rangeIndicator.range = CombatEngine.GetThrowRange(player, item);
                        draggingItem = true;
                    }
                }

                DrawText(left, packtopY + line, screenName, TCODColor.white);
                DrawText(left - 1, packtopY + line, ">", Constants.COL_ATTENTION);

                line += 1;
            }

            // if there is a hovered item, open the item details window
            if(hoveredItem != null && 
                (!itemInfoWindow.isVisible || hoveredItem != itemInfoWindow.targetItem)) {
                // move the window to the correct position, and open it for the new item
                itemInfoWindow.origin.y = hoveredItemY;
                itemInfoWindow.origin.x = origin.x - itemInfoWindow.console.getWidth();
                itemInfoWindow.OpenForItem(hoveredItem);
            }
            else if(hoveredItem == null && itemInfoWindow.isVisible) {
                itemInfoWindow.Close();
            }

            // tell the parent to draw the range indicator if the user is looking like they will drop the dragged item
            if(draggingItem) {
                if(!ScreenCoordInConsole(new Vector2(engine.mouseData.CellX, engine.mouseData.CellY))) {
                    parent.rangeIndicator.active = true;
                }
                else {
                    parent.rangeIndicator.active = false;
                }
            }

            return console;
        }
        

        // handle release of dragged item
        public void OnDragDroppedInvItem() {
            Entity dragged = (Entity)parent.dragdrop.data;
            Vector2 dropPos = ScreenToConsoleCoord(parent.dragdrop.x, parent.dragdrop.y);

            parent.rangeIndicator.active = false;
            draggingItem = false;

            if(dragged == null) {
                return;
            }

            // drop it if it is not dropped on the the console anywhere
            if(dropPos.x < 0 || dropPos.x > console.getWidth()) {
                Vector2 worldSpace = Renderer.ScreenToWorldCoord(engine.mainCamera, new Vector2(parent.dragdrop.x, parent.dragdrop.y + 1));
                player.FireEvent(new EThrowItem() {
                    targetLocation = worldSpace,
                    itemName = dragged.name
                });
                return;
            }

            // if its in the equip sub area, attempt to equip the item
            if(dropPos.y >= equipSection.starty && dropPos.y < equipSection.endy) {
                var equipItem = (EEquip)player.FireEvent(new EEquip() {
                    item = dragged
                });
                
                if(equipItem.wasEquipped) {
                    inventory.items.Remove(dragged);
                }

                return;
            }
        }


        // handle release of dragged item
        public void OnDragDroppedEquippedItem() {
            Entity dragged = (Entity)parent.dragdrop.data;
            World world = engine.world;
            Vector2 dropPos = ScreenToConsoleCoord(parent.dragdrop.x, parent.dragdrop.y);

            parent.rangeIndicator.active = false;
            draggingItem = false;

            if(dragged == null) {
                return;
            }

            // drop it if it is not dropped on the the console anywhere
            if(dropPos.x < 0 || dropPos.x > console.getWidth()) {
                equipment.weapon = null;
                Vector2 spawnLoc = world.currentMap.GetNearestValidMove(player.position);
                world.SpawnExisting(dragged, spawnLoc);
                return;
            }

            // if its in the equip sub area, attempt to equip the item
            if(dropPos.y >= invSection.starty && dropPos.y < invSection.endy) {
                if(inventory.items.Count < inventory.size) {
                    inventory.items.Add(dragged);
                    equipment.weapon = null;
                }

                return;
            }
        }


    }
}
