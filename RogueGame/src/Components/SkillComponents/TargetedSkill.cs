using System.Collections.Generic;
using RogueGame.Map;

namespace RogueGame.Components {
    public class TargetedSkill : Component {

        public float maxRange;
        public float radius;
        public int maxTargets;
    
        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(ETargetSkill), Event_GetTargets, 1000);
        }

        public bool Event_GetTargets(ComponentEvent e) {
            AreaMap map = Engine.instance.world.currentMap;
            var targetEvent = (ETargetSkill)e;

            Entity[] targets = map.GetAllObjectsInRadius(targetEvent.location, radius);
            targetEvent.targets.AddRange(targets);

            return true;
        }

    }
}
