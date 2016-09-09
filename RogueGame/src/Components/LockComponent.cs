using System;


namespace RogueGame.Components {
    public class LockComponent : Component {

        public bool isLocked = true;

        public string keyType = "";

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            
            owner.AddEventResponse(typeof(EOpen), Event_OnOpen, 2000);
            owner.AddEventResponse(typeof(EGetScreenName), Event_GetScreenName, 2000);
        }

        

        public bool Event_OnOpen(ComponentEvent e) {
            // only worry about looking for valid key if its locked
            if(!isLocked) {
                return true;
            }

            EOpen openEvent = (EOpen)e;

            // Check if the asker has a valid key
            Entity opener = openEvent.asker;

            // check if the opener has enything that can unlock this entity, if they do, it is used up
            var requestKey = (EConsumeItem)opener.FireEvent(new EConsumeItem() {
                itemName = keyType
            });
            
            // there is a valid key
            if(requestKey.hasItem) {
                isLocked = false;

                var getAskerName = (EGetScreenName)openEvent.asker.FireEvent(new EGetScreenName());
                var getOwnerName = (EGetScreenName)owner.FireEvent(new EGetScreenName());
                var getItemName = (EGetScreenName)requestKey.consumedItem.FireEvent(new EGetScreenName());
                Engine.LogMessage("%2" + getAskerName.text + "% unlocks %2" + getOwnerName.text + "% with %2" + getItemName.text + "%", openEvent.asker);
            }

            // if its still locked, stop the openable event. Lock should be ranked above any component it is supposed to block
            return !isLocked;
        }



        public bool Event_GetScreenName(ComponentEvent e) {
            const string lockedText = "Locked ";
            if(isLocked) {
                ((EGetScreenName)e).text += lockedText;
            }

            return true;
        }
    }
}
