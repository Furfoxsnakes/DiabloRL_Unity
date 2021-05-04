using System;
using System.Collections.Generic;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using UnityEngine;
using GameObject = GoRogue.GameFramework.GameObject;

namespace Cartography
{
    public class MapTerrain : GoRogue.GameFramework.IGameObject
    {
        private IGameObject _gameObjectImplementation;

        public PackedSpriteID SpriteID;

        public MapTerrain(Coord pos, bool isStatic, bool isWalkable, bool isTransparent, PackedSpriteID spriteID)
        {
            _gameObjectImplementation = new GameObject(pos, (int)MapLayers.TERRAIN, this, isStatic, isWalkable, isTransparent);
            SpriteID = spriteID;
        }

        #region IGameObject

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

        public void OnMapChanged(Map newMap)
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
        
    }
}