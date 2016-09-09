using System;
using libtcod;

namespace RogueGame.Components {
    public class RenderComponent : Component {

        public int layer;

        public char ch;
        public char goreCh;
        public TCODColor color;
        public TCODColor goreColor;
        public string displayName;

        private char normalChar;
        private TCODColor normalColor;


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            
            owner.AddEventResponse(typeof(ESetGraphic), Event_SetGraphic);
            owner.AddEventResponse(typeof(EGetScreenName), Event_GetScreenName, 1000);
            owner.AddEventResponse(typeof(EDied), Event_OnDeath);
        }


        public bool Event_OnDeath(ComponentEvent e) {
            // swap the char and color to the dead version, and cache the normal ones so they can be re-used later
            normalChar = ch;
            normalColor = color;

            ch = goreCh;
            color = goreColor;

            return true;
        }


        public bool Event_GetScreenName(ComponentEvent e) {
            ((EGetScreenName)e).text += displayName;
            return true;
        }



        public bool Event_SetGraphic(ComponentEvent e) {
            var newGraphic = (ESetGraphic)e;
            char newCh = newGraphic.ch;
            TCODColor newCol = newGraphic.color;

            // store the olf graphic before assigning the new ones
            normalChar = ch;
            normalColor = color;

            ch = newCh;
            if(newCol != null) {
                color = newCol;
            }

            return true;
        }



    }
}
