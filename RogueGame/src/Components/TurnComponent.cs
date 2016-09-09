using System;

namespace RogueGame.Components {

    class TurnComponent : Component, IComparable {
        /// <summary>
        /// constant stat
        /// </summary>
        public int speed = 0;

        // running tally that builds up by the speed value every time this entity misses its turn
        public int initiative;

        public int CompareTo(object other) {
            TurnComponent turnTaker = (TurnComponent)other;
            if(turnTaker == null) {
                return -1;
            }

            return this.initiative.CompareTo(turnTaker.initiative);
        }

    }
}
