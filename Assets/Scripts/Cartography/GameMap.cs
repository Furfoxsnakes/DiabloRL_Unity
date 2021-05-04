using System.Collections.Generic;
using System.Collections.ObjectModel;
using Actors;
using Common;
using Enums;
using GoRogue;
using GoRogue.GameFramework;
using GoRogue.MapGeneration;
using GoRogue.MapViews;
using UnityEngine;

namespace Cartography
{
    public class GameMap : Map
    {
        public ReadOnlyCollection<Actor> Actors => _actors.AsReadOnly();
        private List<Actor> _actors;

        public GameMap(int width, int height, uint layersBlockingWalkability = 4294967295, uint layersBlockingTransparency = 4294967295, uint entityLayersSupportingMultipleItems = 0) : base(width, height, (int)MapLayers.Count, Distance.CHEBYSHEV, layersBlockingWalkability, layersBlockingTransparency, entityLayersSupportingMultipleItems)
        {
            _actors = new List<Actor>();
        }

        public void AddActor(Actor actor)
        {
            AddEntity(actor);
            _actors.Add(actor);
        }

        public void RemoveActor(Actor actor)
        {
            if (_actors.Contains(actor))
            {
                _actors.Remove(actor);
                RemoveEntity(actor);
            }
        }
    }
}