using System;
using System.Collections.Generic;
using Common.Components;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using Interfaces;
using UnityEngine;
using GameObject = GoRogue.GameFramework.GameObject;

namespace Actors
{
    public class Actor : IGameObject, IActor, IDrawable
    {
        #region IGameObject

        private IGameObject _gameObjectImplementation;
        
        public uint ID => _gameObjectImplementation.ID;

        public int Layer => _gameObjectImplementation.Layer;

        public void AddComponent(object component)
        {
            _gameObjectImplementation.AddComponent(component);
        }

        public T GetComponent<T>()
        {
            return _gameObjectImplementation.GetComponent<T>();
        }

        public IEnumerable<T> GetComponents<T>()
        {
            return _gameObjectImplementation.GetComponents<T>();
        }

        public bool HasComponent(Type componentType)
        {
            return _gameObjectImplementation.HasComponent(componentType);
        }

        public bool HasComponent<T>()
        {
            return _gameObjectImplementation.HasComponent<T>();
        }

        public bool HasComponents(params Type[] componentTypes)
        {
            return _gameObjectImplementation.HasComponents(componentTypes);
        }

        public void RemoveComponent(object component)
        {
            _gameObjectImplementation.RemoveComponent(component);
        }

        public void RemoveComponents(params object[] components)
        {
            _gameObjectImplementation.RemoveComponents(components);
        }

        public bool MoveIn(Direction direction)
        {
            return _gameObjectImplementation.MoveIn(direction);
        }

        public virtual void OnMapChanged(Map newMap)
        {
            _gameObjectImplementation.OnMapChanged(newMap);
        }

        public Map CurrentMap => _gameObjectImplementation.CurrentMap;

        public bool IsStatic => _gameObjectImplementation.IsStatic;

        public bool IsTransparent
        {
            get => _gameObjectImplementation.IsTransparent;
            set => _gameObjectImplementation.IsTransparent = value;
        }

        public bool IsWalkable
        {
            get => _gameObjectImplementation.IsWalkable;
            set => _gameObjectImplementation.IsWalkable = value;
        }

        public Coord Position
        {
            get => _gameObjectImplementation.Position;
            set => _gameObjectImplementation.Position = value;
        }

        public event EventHandler<ItemMovedEventArgs<IGameObject>> Moved
        {
            add => _gameObjectImplementation.Moved += value;
            remove => _gameObjectImplementation.Moved -= value;
        }

        #endregion

        // IDrawable
        public PackedSpriteID SpriteID
        {
            get => _spriteID;
            set => _spriteID = value;
        }
        private PackedSpriteID _spriteID;

        // IActor
        public string Name => _name;
        private string _name;
        
        public Stats Stats => GetComponent<Stats>();

        public Actor(Coord pos, string name, int mapLayer, PackedSpriteID spriteID)
        {
            _gameObjectImplementation = new GameObject(pos, mapLayer, this, false, false, false);
            _name = name;
            _spriteID = spriteID;
            
            AddComponent(new Stats());
        }

        public virtual void TakeDamage(int amount)
        {
            Stats[StatTypes.HEALTH] -= amount;
            if (Stats[StatTypes.HEALTH] <= 0)
                Die();
        }

        public virtual void Die()
        {
            Debug.Log($"{this} has died.");
        }
    }
}