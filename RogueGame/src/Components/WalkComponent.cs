using System;
using libtcod;
using RogueGame.Map;

namespace RogueGame.Components {

    public class WalkComponent : Component {


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            
            owner.AddEventResponse(typeof(EMove), Event_OnMove);
        }


        public bool Event_OnMove(ComponentEvent e) {
            EMove moveEvent = (EMove)e;
            Vector2 moveDir = moveEvent.direction;
            
            Vector2 currentPos = owner.position;
            Vector2 targetPos = currentPos + moveDir;

            // first check if the tile is passable
            AreaMap map = Engine.instance.world.currentMap;
            if(map.CanMoveTo(targetPos.X, targetPos.Y)) {
                // next check if there is an entity there that can be attacked
                Entity[] targets = map.GetAllObjectsAt(targetPos.X, targetPos.Y);
                bool canPass = true;

                // check if the target blocks passage, and if so try to attack it
                if(targets.Length > 0) {
                    foreach(Entity target in targets) {
                        EGetBlockState blocks = (EGetBlockState)target.FireEvent(new EGetBlockState() {
                            asker = owner,
                            blocking = false
                        });
                        
                        if(blocks.blocking) {
                            // first see if it can be opened
                            var openEvent = (EOpen)target.FireEvent(new EOpen {
                                asker = owner
                            });

                            // if it was not able to be opened, try attacking it
                            if(!openEvent.wasOpened) {
                                owner.FireEvent(new EDoAttack() { target = target });
                            }

                            canPass = false;
                        }
                    }
                }
                
                if(canPass) {
                    map.MoveEntity(owner, targetPos);
                }
            }
            
            return true;
        }
        

    }
}
