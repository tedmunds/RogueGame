using System;


namespace RogueGame.Components {

    /// <summary>
    /// Team indicates who can attack who, and which enemies will coordinate with eachother
    /// </summary>
    public class TeamComponent : Component {

        public int teamNum;
        
        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(ECanAttack), Event_CanAttack);
            owner.AddEventResponse(typeof(EGetTeamNumber), Event_GetNumber);
        }

        public bool Event_CanAttack(ComponentEvent e) {
            var canAttackEvent = (ECanAttack)e;
            Entity asker = canAttackEvent.asker;
            var getTeam = (EGetTeamNumber)asker.FireEvent(new EGetTeamNumber());

            // if they are on the same team, then the target is invalid
            int otherNum = getTeam.team;
            if(otherNum == teamNum) {
                canAttackEvent.validTarget = false;
            }

            return true;
        }


        public bool Event_GetNumber(ComponentEvent e) {
            ((EGetTeamNumber)e).team = teamNum;
            return true;
        }

    }
}
