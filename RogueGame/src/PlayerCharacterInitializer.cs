using System;
using System.Reflection;
using System.Collections.Generic;
using libtcod;
using RogueGame.Components;


namespace RogueGame {

    /// <summary>
    /// A container for all the data needed to create a character
    /// </summary>
    public class PlayerCharacterInitializer {

        public AttributesComponent attributes;
        public string name;
        public List<Entity> startingSkills;
        public List<Entity> startingItems;

        /// <summary>
        /// Assigns the values and settings as specified by this initializer, to the input entity
        /// </summary>
        public void ApplyToPlayer(Entity player) {
            if(player == null) {
                return;
            }

            RenderComponent renderer = player.Get<RenderComponent>();
            renderer.displayName = name;

            // for each stat, assign the value from the initializers component, to the player entities component
            AttributesComponent playerAttr = player.Get<AttributesComponent>();
            foreach(FieldInfo attrField in attributes.AttributeFields()) {
                attrField.SetValue(playerAttr, attrField.GetValue(attributes));
            }

            // give the player all of the skills and items they selected
            var playerSkills = player.Get<SkillUserComponent>();
            foreach(Entity e in startingSkills) {
                playerSkills.AddNewSkill(e);
            }

            foreach(Entity e in startingItems) {
                player.FireEvent(new EAquireItem() { item = e });
            }
        }

    }
}
