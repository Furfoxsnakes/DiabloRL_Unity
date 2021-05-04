using Actors.Behaviours;
using Cartography;
using Common.Components;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using UnityEngine;

namespace Actors
{
    public abstract class Enemy : Actor
    {
        public MoveAction MoveAction => GetComponent<MoveAction>();
        public EnemyBehaviour Behaviour;
        public FOV FOV;
        public Enemy(Coord pos, string name, PackedSpriteID spriteID) : base(pos, name, (int)MapLayers.ENEMIES, spriteID)
        {
            Stats[StatTypes.AWARENESS] = 3;
        }

        public override void OnMapChanged(Map newMap)
        {
            base.OnMapChanged(newMap);
            if (newMap == null) return; // was removed from map
            Behaviour = new MoveAndAttackBehaviour(this, newMap as GameMap);
            FOV = new FOV(newMap.WalkabilityView);
        }

        public override void Die()
        {
            base.Die();
            (CurrentMap as GameMap)?.RemoveActor(this);
        }
    }
}