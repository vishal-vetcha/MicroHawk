using System;
using MicroHawk.World;
using UnityEngine;

namespace MicroHawk.Simulation
{
    /// <summary>Scene composition and tuning. Runtime is assembled on Play without rewriting the authored scene.</summary>
    public sealed class SceneConfiguration : MonoBehaviour
    {
        [SerializeField] private WorldRegistry world;
        [SerializeField] private WorldEntity homePad;
        [SerializeField] private Transform homeSpawn;
        [SerializeField] private Rigidbody drone;
        public WorldRegistry World => world;
        public WorldEntity HomePad => homePad;
        public Transform HomeSpawn => homeSpawn;
        public Rigidbody Drone => drone;

        [SerializeField] private MicroHawk.Safety.SafetySettings safetyTuning=new();
        [SerializeField] private MicroHawk.Flight.FlightTuning controllerTuning=new();
        [SerializeField] private MicroHawk.Flight.BatterySettings batteryTuning=new();
        public MicroHawk.Safety.SafetySettings SafetyTuning=>safetyTuning;
        public MicroHawk.Flight.FlightTuning ControllerTuning=>controllerTuning;
        public MicroHawk.Flight.BatterySettings BatteryTuning=>batteryTuning;
        public FlightRuntime Runtime { get; private set; }
        private void Start()
        {
            Runtime=gameObject.AddComponent<FlightRuntime>();
            Runtime.Initialize(this);
        }
        private void Awake() => ValidateConfiguration();

        public void ValidateConfiguration()
        {
            if (!world || !homePad || !homeSpawn || !drone)
                throw new InvalidOperationException("Missing MicroHawk bootstrap reference.");
            world.Rebuild();
            if (homePad.StableId != "home_pad" || !world.TryGet("home_pad", out var registered) || registered != homePad)
                throw new InvalidOperationException("Home pad is not the registered home_pad.");
            if (drone.isKinematic || !drone.useGravity || drone.mass <= 0f)
                throw new InvalidOperationException("Drone requires a dynamic body with positive mass and gravity.");
        }

#if UNITY_EDITOR
        public void ConfigureForAuthoring(WorldRegistry registry, WorldEntity home, Transform spawn, Rigidbody body)
        {
            world = registry;
            homePad = home;
            homeSpawn = spawn;
            drone = body;
            ValidateConfiguration();
        }
#endif
    }
}

