using System;
using System.Collections.Generic;
using UnityEngine;

namespace MicroHawk.World
{
    /// <summary>Scene-scoped lookup for deterministic references, not detected objects.</summary>
    public sealed class WorldRegistry : MonoBehaviour
    {
        [SerializeField] private WorldEntity[] entities = Array.Empty<WorldEntity>();
        private readonly Dictionary<string, WorldEntity> index = new(StringComparer.Ordinal);
        public int Count => index.Count;
        public IReadOnlyCollection<string> Ids => index.Keys;

        private void Awake() => Rebuild();

        public void Rebuild()
        {
            var validated = new Dictionary<string, WorldEntity>(StringComparer.Ordinal);
            foreach (var entity in entities)
            {
                if (entity == null) throw new InvalidOperationException("World registry contains a missing reference.");
                WorldEntity.ValidateId(entity.StableId);
                if (!validated.TryAdd(entity.StableId, entity))
                    throw new InvalidOperationException($"Duplicate world ID: {entity.StableId}");
            }
            index.Clear();
            foreach (var pair in validated) index.Add(pair.Key, pair.Value);
        }

        public bool TryGet(string id, out WorldEntity entity) => index.TryGetValue(id, out entity);

#if UNITY_EDITOR
        public void ConfigureForAuthoring(WorldEntity[] authoredEntities)
        {
            entities = authoredEntities;
            Rebuild();
        }
#endif
    }
}
