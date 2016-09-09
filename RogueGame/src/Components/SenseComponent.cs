using System;


namespace RogueGame.Components {
    public class SenseComponent : Component {

        public int sightRange;


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            owner.AddEventResponse(typeof(EGetStat_SightRange), Event_GetSightRange);
        }


        public bool Event_GetSightRange(ComponentEvent e) {
            ((EGetStat_SightRange)e).range = sightRange;
            return true;
        }
    }
}
