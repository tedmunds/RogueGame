using System;
using libtcod;

namespace RogueGame.Components {
    public class OpenableComponent : Component {

        public char openedCh;

        public bool opened = false;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EOpen), Event_OnOpen, 1000);
            //owner.AddEventResponse(typeof(EGetScreenName), Event_GetScreenName, 2000);
        }



        public bool Event_OnOpen(ComponentEvent e) {
            EOpen openEvent = (EOpen)e;

            // was already opened previously
            if(opened) {
                openEvent.wasOpened = false;
                return true;
            }
            
            // and update the graphic
            owner.FireEvent(new ESetGraphic() {
                ch = openedCh
            });
               
            // most important part, set the opened flag in the event so other components can respond to this fact 
            opened = true;
            openEvent.wasOpened = true;

            var getAskerName = (EGetScreenName)openEvent.asker.FireEvent(new EGetScreenName());
            var getOwnerName = (EGetScreenName)owner.FireEvent(new EGetScreenName());
            Engine.LogMessage("%2" + getAskerName.text + "% opens %2" + getOwnerName.text + "%", openEvent.asker);
            return true;
        }


        public bool Event_GetScreenName(ComponentEvent e) {
            const string openText = "Open ";
            const string closedText = "Closed ";
            ((EGetScreenName)e).text += opened ? openText : closedText;

            return true;
        }



    }
}
