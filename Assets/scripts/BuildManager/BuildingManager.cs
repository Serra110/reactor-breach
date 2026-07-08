using System.Collections.Generic;
using UnityEngine;

namespace SimpleBuildingSystem
{
    
    public class BuildingManager : MonoBehaviour
    {
        public static BuildingManager Instance { get; private set; }

        private readonly List<BuildingPart> _placedParts = new List<BuildingPart>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterPart(BuildingPart part)
        {
            if (!_placedParts.Contains(part))
                _placedParts.Add(part);
        }

        public void UnregisterPart(BuildingPart part)
        {
            _placedParts.Remove(part);
        }

        public IReadOnlyList<BuildingPart> GetAllParts() => _placedParts;

        public BuildingPart FindNearest(Vector3 position, float maxDistance = float.MaxValue)
        {
            BuildingPart best = null;
            float bestDist = maxDistance;

            foreach (var part in _placedParts)
            {
                float dist = Vector3.Distance(position, part.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = part;
                }
            }
            return best;
        }
    }
}
