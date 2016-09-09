using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Components;

namespace RogueGame.Interface {
    public class WindowCharacterCreateMenu : WindowGameMenu {
        
        private const int NUMBER_OF_POINTS  = 250;  // max number of attribute poitns available to spend on character creation
        private const int ATTR_POINTS_DELTA = 10;   // how much does the increase / decrease buttons effect the value of an attribute by
        private const int MAX_ATTR_LEVEL    = 100;  // Max number an attribute can be increased up to
        private const int MAX_NAME_LENGTH   = 21;   // max length of character name. Important because of texts that I dont want to need to split onto two lines

        private const int MAX_EQUIPABLES    = 5;
        private const int MAX_EQUIP_SKILLS = 5;

        private class StatSelector {
            public int current;
            public string statName;
            public Button increase;
            public Button decrease;
        }
        
        private TextField characterNameField;

        // attribute component that the menu uses to bdynamically create an attribute selector for each one
        private AttributesComponent attributes;

        // attribute selector has an increase and decrease button and the current value of the stat
        private List<StatSelector> attrSelectors;        
        
        // inspector windows that show info about the items.skills available to choose from
        private WindowSkillInfo skillInspector;
        private WindowItemInfo itemInspector;

        // list of entities that can be sleected on character creation: 
        private List<Entity> defaultSkills;
        private List<Entity> defaultItems;

        private List<Entity> equippedSkills;
        private List<Entity> equippedItems;

        // the rect areas wherethe main list of skills / items that you choose from are in
        private SubSection skillSelectSection;
        private SubSection itemSelectSection;

        // the areas you drop the selected skills/items
        private SubSection skillDropSection;
        private SubSection itemDropSection;

        private bool isHoveringOnItem = false;

        /// <summary>
        /// Number of points the player has left to assign to attributes
        /// </summary>
        private int availablePoints;

        public WindowCharacterCreateMenu(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            borderStyle = EBorderStyle.NONE;
            availablePoints = NUMBER_OF_POINTS;

            dragdrop = new HUD.DragDrop();

            characterNameField = new TextField(3, 5, MAX_NAME_LENGTH, 1, elementDarker, elementDark, foregroundColor, CharacterNameCompleted);

            // sub windows for inspecitng the starrting skills and starting items
            skillInspector = new WindowSkillInfo(0, 0, 30, 12, "[skill]", EBorderStyle.ALL, null, null);
            itemInspector = new WindowItemInfo(0, 0, 30, 12, "[item]", EBorderStyle.ALL, null, null);
        }


        public override void OnDestroy() {
            base.OnDestroy();
            skillInspector.OnDestroy();
            itemInspector.OnDestroy();
        }

        public override List<Window> GetSubMenus() {
            List<Window> subWindows = base.GetSubMenus();
            subWindows.Add(itemInspector);
            subWindows.Add(skillInspector);
            return subWindows;
        }
        
        
        // permament features
        public override void InitializeConsole() {
            const int topY = 12;
            const int left = 3;
            base.InitializeConsole();
            
            // back button
            buttons.Add(new Button(console.getWidth() - 10, 1, 6, 1, "[back]",
                elementDark, Constants.COL_BLUE, foregroundColor,
                GotoMainMenu));

            // start game button
            buttons.Add(new Button(console.getWidth() - 10, console.getHeight() - 2, 6, 1, "[done]",
                elementDark, Constants.COL_BLUE, foregroundColor,
                StartGame)); // TODO: change to load level


            // init some temp components to store data in -------------------------------------------------------- Attribute buttons stuff
            attributes = new AttributesComponent();

            // init the stat adjusting buttons for each stat
            attrSelectors = new List<StatSelector>();
            int buttonLine = 0;
            foreach(string attr in attributes.Attributes()) {
                int y = buttonLine + topY;
                const int leftOffset = 14;

                StatSelector statSelector = new StatSelector();
                statSelector.statName = attr;
                statSelector.current = 0;

                // the decrease button
                statSelector.decrease = new Button(left + leftOffset, y, 1, 1, "-",
                    elementDark, Constants.COL_BLUE, foregroundColor,
                    DecreaseCurrentStat);
                statSelector.decrease.payload = statSelector;

                // the increase button
                statSelector.increase = new Button(left + leftOffset + 6, y, 1, 1, "+",
                    elementDark, Constants.COL_BLUE, foregroundColor,
                    IncreaseCurrentStat);
                statSelector.increase.payload = statSelector;

                buttons.Add(statSelector.decrease);
                buttons.Add(statSelector.increase);

                attrSelectors.Add(statSelector);
                buttonLine += 2;
            }

            const string mainTitle = "[CREATE CHARACTER]";

            // draw main title
            DrawText(console.getWidth() / 2 - mainTitle.Length / 2, 1, mainTitle, foregroundColor);
            DrawDividerLine(0, 2, console.getWidth());

            // draw out the attibute names in a list            
            DrawText(left, 4, "Character Name:", Constants.COL_ATTENTION);
            DrawText(left, topY - 4, "Character Attrbiutes: ", Constants.COL_ATTENTION);

            // initialize the default skill and item lists ----------------------------------------------------- Item and skill selection stuff
            defaultSkills = new List<Entity>();
            defaultItems = new List<Entity>();
            equippedSkills = new List<Entity>();
            equippedItems = new List<Entity>();

            string[] skillNames = engine.dataTables.GetListValue("startSkills", "CharacterCreation", "set");
            string[] itemNames = engine.dataTables.GetListValue("startItems", "CharacterCreation", "set");

            // The skill selector area ---------------------------------------------------------
            const int skillLeft = 30;
            const int tableY = 4;
            const int tableWidth = 32;

            // define a littel subsection box for the skill select area
            skillSelectSection = new SubSection();
            skillSelectSection.origin = new Vector2(skillLeft - 1, tableY + 1);
            skillSelectSection.size = new Vector2(tableWidth, 43);

            skillDropSection = new SubSection();
            skillDropSection.origin = new Vector2(skillLeft - 1, skillSelectSection.origin.Y + skillSelectSection.size.Y + 3);
            skillDropSection.size = new Vector2(tableWidth, 7);

            DrawText(skillSelectSection.origin.X, skillSelectSection.origin.Y - 1, "Starting Skills:", Constants.COL_ATTENTION);
            DrawText(skillDropSection.origin.X, skillDropSection.origin.Y - 1, "Equipped Skills:", Constants.COL_ATTENTION);

            int line = 0;
            // for each skill in the list from data, instantiate the skill antity and create a hover target for it
            foreach(string name in skillNames) {
                Entity skill = engine.data.InstantiateEntity(name);
                defaultSkills.Add(skill);

                var getName = (EGetScreenName)skill.FireEvent(new EGetScreenName());

                int y = tableY + 2 + line;
                //DrawText(skillLeft, y, "+" + getName.text, foregroundColor);
                
                // create a hover target for each skill
                Hoverable hoverTarget = new Hoverable();
                hoverTarget.entity = skill;
                hoverTarget.width = getName.text.Length + 1;
                hoverTarget.origin = new Vector2(skillSelectSection.origin.X + 1, y);
                hoverTarget.handler = OpenSkillInspector;

                hoverableItems.Add(hoverTarget);
                line += 1;
            }

            // The item selector area ---------------------------------------------------------
            const int itemLeft = 63;

            itemSelectSection = new SubSection();
            itemSelectSection.origin = new Vector2(itemLeft - 1, tableY + 1);
            itemSelectSection.size = new Vector2(tableWidth, 43);

            itemDropSection = new SubSection();
            itemDropSection.origin = new Vector2(itemLeft - 1, itemSelectSection.origin.Y + itemSelectSection.size.Y + 3);
            itemDropSection.size = new Vector2(tableWidth, 7);

            DrawText(itemSelectSection.origin.X, itemSelectSection.origin.Y - 1, "Starting Items:", Constants.COL_ATTENTION);
            DrawText(itemDropSection.origin.X, itemDropSection.origin.Y - 1, "Equipped Items:", Constants.COL_ATTENTION);

            line = 0;
            // for each item in the list from data, instantiate the item entity and create a hover target for it
            foreach(string name in itemNames) {
                Entity item = engine.data.InstantiateEntity(name);
                defaultItems.Add(item);

                var getName = (EGetScreenName)item.FireEvent(new EGetScreenName());

                int y = tableY + 2 + line;
                //DrawText(itemLeft, y, "+" + getName.text, foregroundColor);
                
                // create a hover target for each skill
                Hoverable hoverTarget = new Hoverable();
                hoverTarget.entity = item;
                hoverTarget.width = getName.text.Length + 1;
                hoverTarget.origin = new Vector2(itemSelectSection.origin.X + 1, y);
                hoverTarget.handler = OpenItemInspector;

                hoverableItems.Add(hoverTarget);
                line += 1;
            }
            
        }


        
        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            DrawBox(skillSelectSection.origin.X, skillSelectSection.origin.Y,
                skillSelectSection.size.X, skillSelectSection.size.Y, backgroundColor, true);
            
            DrawBox(skillDropSection.origin.X, skillDropSection.origin.Y,
                skillDropSection.size.X, skillDropSection.size.Y, backgroundColor, true);
            
            DrawBox(itemSelectSection.origin.X, itemSelectSection.origin.Y,
                 itemSelectSection.size.X, itemSelectSection.size.Y, backgroundColor, true);
            
            DrawBox(itemDropSection.origin.X, itemDropSection.origin.Y,
                itemDropSection.size.X, itemDropSection.size.Y, backgroundColor, true);

            base.GetUpdatedConsole();

            DrawText(3, 9, "points: " + availablePoints.ToString("D3"), Constants.COL_FRIENDLY);

            foreach(StatSelector statSelector in attrSelectors) {
                int y = statSelector.increase.origin.Y;

                // draw the stat name, and the value inbetween the inc. and dec. buttons. Center the 3 digit number
                DrawText(3, y, "- " + statSelector.statName, Constants.COL_NORMAL);
                DrawText(statSelector.decrease.origin.X + 2, y, statSelector.current.ToString("D3"), Constants.COL_NORMAL);
            }

            // character name, and label on top of it
            characterNameField.Update(engine.mouseData, engine.lastKey);
            characterNameField.Draw(this);
            
            // in case nothing was hovered on, close both windows because its impossible to hover on both an item and skill at the same time
            if(!isHoveringOnItem) {
                itemInspector.Close();
                skillInspector.Close();
            }

            isHoveringOnItem = false;

            return console;
        }

        
        /// <summary>
        /// Called when the supplied hoverable gets hovered over
        /// </summary>
        protected override void OnItemHovered(Hoverable hovered) {
            base.OnItemHovered(hovered);
            isHoveringOnItem = true;
            
            // if the item that is being hovered on was also clicked on, start dragging it
            if(engine.mouseData.LeftButtonPressed) {
                var getName = (EGetScreenName)hovered.entity.FireEvent(new EGetScreenName());
                SetDragDrop(getName.text, hovered, OnDragItemDropped);
            }
        }

        

        // Called by back button
        public void GotoMainMenu(Button btn) {
            engine.OpenMenu(new WindowMainMenu(0, 0, engine.consoleWidth, engine.consoleHeight, null, EBorderStyle.NONE, null, null));
        }


        // called whe the character name field gets accepted by user
        public void CharacterNameCompleted() {
            // Maybe check if the name is valid?
        }


        // called when the player is one creating character, and wants to start the game
        public void StartGame(Button btn) {
            // player must have entered a name
            if(characterNameField.text.Length <= 0) {
                return;
            }

            // set up the initializer with all the data collected over the creation process
            PlayerCharacterInitializer initializer = new PlayerCharacterInitializer();
            initializer.name = characterNameField.text;
            initializer.attributes = attributes;
            initializer.startingSkills = equippedSkills;
            initializer.startingItems = equippedItems;

            // assign all the values collected from the ui selectors to the initializers temporary attribute component. Which will be 
            // copied onto the player
            foreach(var attr in attrSelectors) {
                var attrField = attributes.GetType().GetField(attr.statName);
                if(attrField != null) {
                    attrField.SetValue(attributes, attr.current);
                }
            }

            // And start the game, initializing the player character with the data set in the UI
            engine.StartNewGame(initializer);
        }


        /// <summary>
        /// Increments the stat that the button targets, by the global incremental delta
        /// </summary>
        public void IncreaseCurrentStat(Button btn) {
            StatSelector targetStat = (StatSelector)btn.payload;

            if(targetStat == null) {
                return;
            }

            if(availablePoints - ATTR_POINTS_DELTA < 0 || targetStat.current + ATTR_POINTS_DELTA > MAX_ATTR_LEVEL) {
                return;
            }

            availablePoints -= ATTR_POINTS_DELTA;
            targetStat.current += ATTR_POINTS_DELTA;
        }

        /// <summary>
        /// decreases the stat that the button targets, by the global incremental delta
        /// </summary>
        public void DecreaseCurrentStat(Button btn) {
            StatSelector targetStat = (StatSelector)btn.payload;

            if(targetStat == null) {
                return;
            }

            if(targetStat.current - ATTR_POINTS_DELTA < 0) {
                return;
            }

            availablePoints += ATTR_POINTS_DELTA;
            targetStat.current -= ATTR_POINTS_DELTA;
        }


        // Hover handlers for items and skills
        private void OpenItemInspector(Hoverable hovered, int x, int y) {
            if(hovered != null &&
                (!itemInspector.isVisible || hovered.entity != itemInspector.targetItem)) {
                // move the window to the correct position, and open it for the new item
                itemInspector.origin.y = y;
                itemInspector.origin.x = x;
                itemInspector.OpenForItem(hovered.entity);
            }
        }

        private void OpenSkillInspector(Hoverable hovered, int x, int y) {
            if(hovered != null &&
                (!skillInspector.isVisible || hovered.entity != skillInspector.targetSkill)) {
                // move the window to the correct position, and open it for the new item
                skillInspector.origin.y = y;
                skillInspector.origin.x = x;
                skillInspector.OpenForSkill(hovered.entity);
            }
        }


        // check which sub area the item was dropped in
        public void OnDragItemDropped() {
            Vector2 mLoc = ScreenToConsoleCoord(engine.mouseData.CellX, engine.mouseData.CellY);
            Entity e;
            Hoverable hoverable;
            if(dragdrop.data is Hoverable) {
                hoverable = (Hoverable)dragdrop.data;
                e = hoverable.entity;
            }
            else {
                return;
            }

            SkillComponent skillComp = e.Get<SkillComponent>();
            EquipableComponent equipComp = e.Get<EquipableComponent>();

            // entity was dropped in the skill drop section, try to equip it as a skill
            if(IsMouseInSubSection(skillDropSection, mLoc)) {                
                if(skillComp != null && !equippedSkills.Contains(e)) {
                    // check there is room to equip this skill
                    if(equippedSkills.Count + 1 <= MAX_EQUIP_SKILLS) {
                        equippedSkills.Add(e);

                        // put the hoverable in the skill subsection
                        hoverable.origin.y = skillDropSection.origin.Y + equippedSkills.Count;
                    }
                }
            }
            else if(IsMouseInSubSection(skillSelectSection, mLoc)) {
                if(skillComp != null && equippedSkills.Contains(e)) {
                    equippedSkills.Remove(e);

                    // returnt eh hoverable to its location in the dfaults section
                    int i = defaultSkills.IndexOf(e);
                    hoverable.origin.y = skillSelectSection.origin.Y + i + 1;
                }
            }

            // otherwise if it was in the item drop section
            if(IsMouseInSubSection(itemDropSection, mLoc)) {                
                if(equipComp != null && !equippedItems.Contains(e)) {
                    // check there is room to equip this item
                    if(equippedItems.Count + 1 <= MAX_EQUIPABLES) {
                        equippedItems.Add(e);

                        // put the hoverable in the skill subsection
                        hoverable.origin.y = itemDropSection.origin.Y + equippedItems.Count;
                    }
                }
            }
            else if(IsMouseInSubSection(itemSelectSection, mLoc)) {
                if(equipComp != null && equippedItems.Contains(e)) {
                    equippedItems.Remove(e);

                    // return the hoverable to its location in the dfaults section
                    int i = defaultItems.IndexOf(e);
                    hoverable.origin.y = itemSelectSection.origin.Y + i + 1;
                }
            }
        }





        

    }
}
