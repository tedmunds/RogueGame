using System;

namespace RogueGame.Components {


    public class InvulnerableComponent : Component {



        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            // intercepts damage event with very high priority, and just eats teh event
            owner.AddEventResponse(typeof(EDoDamage), Event_HandleDamage, 10000);
        }


        public bool Event_HandleDamage(ComponentEvent e) {
            ((EDoDamage)e).damage = 0;
            return false;
        }

    }
}
