using System;
using System.Collections.Generic;
using RogueGame.Map;

namespace RogueGame.Components {
    public class InvComponent : Component {

        // max number of items that can be stored
        public int size;

        public List<Entity> items = new List<Entity>();
        
        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            
            owner.AddEventResponse(typeof(EAquireItem),     Event_OnAquire);
            owner.AddEventResponse(typeof(EDropItem),       Event_OnDrop);
            owner.AddEventResponse(typeof(EDropAllItems),   Event_OnDropAll);
            owner.AddEventResponse(typeof(EHasItem),        Event_CheckForItem);
            owner.AddEventResponse(typeof(EConsumeItem),    Event_ConsumeItem);            
        }


        public bool Event_OnAquire(ComponentEvent e) {
            World world = Engine.instance.world;

            // get the item payload of the event and add it to the internal list
            Entity newItem = ((EAquireItem)e).item;

            if(newItem != null && items.Count < size) {
                // check if it can be picked up
                ECanPickup carryEvent = (ECanPickup)newItem.FireEvent(new ECanPickup {
                    asker = owner
                });
                
                if(carryEvent.canPickup) {
                    world.DespawnEntity(newItem);
                    items.Add(newItem);

                    var getItemName = (EGetScreenName)newItem.FireEvent(new EGetScreenName());
                    var getOwnerName = (EGetScreenName)owner.FireEvent(new EGetScreenName());
                    Engine.LogMessage("%2" + getOwnerName.text + "% grabs %2" + getItemName.text + "%", owner);
                }
            }
            
            return true;
        }



        public bool Event_OnDrop(ComponentEvent e) {           
            Entity dropItem = ((EDropItem)e).item;

            // find the item that the event wants to drop and add it to the world and remove it from the inventory
            if(dropItem != null) {
                DropItem(dropItem);
            }

            return true;
        }



        public bool Event_OnDropAll(ComponentEvent e) {
            // just drop each item item at nearby location
            for(int i = items.Count - 1; i >= 0; i--) {
                DropItem(items[i]);
            }
            return true;
        }

        
        public bool Event_CheckForItem(ComponentEvent e) {
            var request = (EHasItem)e;

            string itemType = request.itemName;
            bool hasItem = false;

            // check if an item matches the payloads request
            foreach(Entity item in items) {
                if(item.name == itemType) {
                    hasItem = true;
                    break;
                }
            }

            request.hasItem = hasItem;
            return true;
        }

        
        public bool Event_ConsumeItem(ComponentEvent e) {
            var request = (EConsumeItem)e;

            string itemType = request.itemName;
            bool hasItem = false;

            // check if an item matches the payloads request
            foreach(Entity item in items) {
                if(item.name == itemType) {
                    hasItem = true;

                    // like check for item, except it gets rid of the item
                    items.Remove(item);
                    request.consumedItem = item;
                    break;
                }
            }

            request.hasItem = hasItem;
            return true;
        }

        

        public void DropItem(Entity drop) {
            World world = Engine.instance.world;

            if(items.Contains(drop)) {
                Vector2 spawnLoc = world.currentMap.GetNearestValidMove(owner.position);
                world.SpawnExisting(drop, spawnLoc);
                items.Remove(drop);
            }
        }

        
    }
}
