using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MicroHawk.World
{
    /// <summary>Authored simulator ground truth. Never a perception observation.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldEntity : MonoBehaviour
    {
        [SerializeField] private string stableId;
        public string StableId => stableId;
        public const string Provenance = "simulator_ground_truth";

        public static void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, "^[a-z][a-z0-9_]{0,63}$"))
                throw new ArgumentException("World ID must be lowercase snake_case, at most 64 characters.", nameof(id));
        }

#if UNITY_EDITOR
        public void ConfigureForAuthoring(string id)
        {
            ValidateId(id);
            stableId = id;
        }
#endif
    }
}
