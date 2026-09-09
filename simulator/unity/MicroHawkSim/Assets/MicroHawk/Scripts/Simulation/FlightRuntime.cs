using System;
using System.Collections.Generic;
using System.Linq;
using MicroHawk.Contracts;
using MicroHawk.Flight;
using MicroHawk.Safety;
using MicroHawk.Presentation;
using UnityEngine;

namespace MicroHawk.Simulation
{
    [DisallowMultipleComponent]
    public sealed class FlightRuntime : MonoBehaviour, IFlightOperations, ISimulationReset
    {
        public const double StepSeconds=.02;
        private readonly Queue<(long id,FlightCommand command)> queue=new();
        private readonly TelemetryPublisher publisher=new();
        private SceneConfiguration config;
        private UnitySimulatorAdapter adapter;
        private SafetyEngine safety;
        private FlightExecution execution;
        private BatteryModel battery;
        private SafetySettings limits;
        private FlightVector home;
        private SafetyDecision safetyState;
        private long tick,session,nextId;
        private bool initialized,timeout;
        private SimulationMode previousMode;
        public bool AutomaticStepping { get; set; }=true;
        public TelemetrySnapshot Telemetry=>publisher.Latest;
        public FlightState State=>execution.State;
        public event Action<CommandResult> CommandChanged { add=>publisher.CommandChanged+=value; remove=>publisher.CommandChanged-=value; }
        public event Action<TelemetrySnapshot> TelemetryPublished { add=>publisher.Published+=value; remove=>publisher.Published-=value; }

        public void Initialize(SceneConfiguration configuration)
        {
            if(initialized)throw new InvalidOperationException("Flight runtime already initialized.");
            config=configuration;config.ValidateConfiguration();
            var pads=config.World.Ids.Where(id=>id=="home_pad"||id.StartsWith("inspection_pad_",StringComparison.Ordinal)).Select(id=>{config.World.TryGet(id,out var entity);return entity.transform.position;}).ToArray();
            adapter=new UnitySimulatorAdapter(config.Drone,config.HomeSpawn,pads);
            home=UnitySimulatorAdapter.ToDomain(config.HomeSpawn.position);
            previousMode=Physics.simulationMode;Physics.simulationMode=SimulationMode.Script;
            initialized=true;ResetSimulation();
            var panel=gameObject.AddComponent<OperationsPanel>();panel.Bind(this,this);
            var rotors=gameObject.AddComponent<RotorFeedback>();rotors.Bind(this,config.Drone.transform);
        }
        private void FixedUpdate(){if(initialized&&AutomaticStepping)AdvanceOneTick();}
        private void OnDestroy(){if(initialized)Physics.simulationMode=previousMode;}
        public long Submit(FlightCommand command,long expectedSession)
        {
            long id=++nextId;
            if(expectedSession!=session){Outcome(id,command,ExecutionStatus.Rejected,ReasonCode.StaleSession);return id;}
            if(queue.Count>=16){Outcome(id,command,ExecutionStatus.Rejected,ReasonCode.QueueFull);return id;}
            queue.Enqueue((id,command));return id;
        }
        private void Outcome(long id,FlightCommand command,ExecutionStatus status,ReasonCode reason=ReasonCode.None)
            =>publisher.Outcome(new CommandResult(session,id,tick,command?.Kind,status,reason));
        public void ResetSimulation()
        {
            if(!initialized)return;
            execution?.Interrupt(ReasonCode.Reset);
            foreach(var pending in queue)Outcome(pending.id,pending.command,ExecutionStatus.Interrupted,ReasonCode.Reset);
            queue.Clear();session++;tick=nextId=0;timeout=false;
            limits=config.SafetyTuning.ValidatedCopy();
            safety=new SafetyEngine(limits,adapter);battery=new BatteryModel(config.BatteryTuning);
            execution=new FlightExecution(config.ControllerTuning);
            execution.Result+=(id,kind,status,reason)=>
            {
                if(reason==ReasonCode.Timeout)timeout=true;
                publisher.Outcome(new CommandResult(session,id,tick,kind,status,reason));
            };
            adapter.RestoreHome();safetyState=new SafetyDecision(SafetyStatus.Approved);
            Publish();
        }
        /// <summary>Local fixed-step simulation lifecycle hook, also used by physics tests; not an AI command.</summary>
        public void AdvanceOneTick()
        {
            if(!initialized)throw new InvalidOperationException("Runtime not initialized.");
            tick++;
            var body=adapter.Read();
            var batteryDecision=safety.BatteryDecision(battery.Percent,execution.State);
            EnforceBattery(batteryDecision,body);
            while(queue.Count>0)
            {
                var pending=queue.Dequeue();
                if(execution.Permission?.Cause is ReasonCode.LowBattery or ReasonCode.CriticalBattery)
                {Outcome(pending.id,pending.command,ExecutionStatus.Rejected,ReasonCode.FailsafeActive);continue;}
                var decision=safety.Authorize(pending.command,execution.State,body,battery.Percent,home,out var permission);
                safetyState=decision;
                if(!decision.Approved){Outcome(pending.id,pending.command,ExecutionStatus.Rejected,decision.Reason);continue;}
                Outcome(pending.id,pending.command,ExecutionStatus.Accepted);
                execution.Accept(permission,pending.id,body);
            }
            if(FlightStateMachine.Airborne(execution.State)&&execution.Target.HasValue)
            {
                bool support=execution.State==FlightState.TakingOff||execution.IsLanding;
                bool braking=execution.Permission.Cause is ReasonCode.Obstacle or ReasonCode.Timeout;
                if(!braking&&!safety.RuntimeRouteClear(body,execution.Target.Value,support))
                    Brake(body,ReasonCode.Obstacle);
            }
            var output=execution.Tick(body,StepSeconds);
            if(timeout){timeout=false;Brake(body,ReasonCode.Timeout);output=execution.Tick(body,StepSeconds);}
            adapter.Apply(output);adapter.Step(StepSeconds);
            battery.Tick(execution.State,adapter.Read().Velocity.Length,StepSeconds);
            if(tick%5==0)Publish();
        }
        private void EnforceBattery(SafetyDecision decision,BodySnapshot body)
        {
            if(decision.Status==SafetyStatus.Approved)return;
            if(execution.State==FlightState.Emergency&&execution.Permission?.Command is HoldCommand)return;
            if(decision.Status==SafetyStatus.ReturnHomeRequired&&execution.Permission?.Cause==ReasonCode.LowBattery&&execution.Permission.Command is HoldCommand)return;
            safetyState=decision;
            if(decision.Status==SafetyStatus.EmergencyLandRequired)
            {
                if(execution.State==FlightState.Emergency)return;
                var result=safety.Authorize(new LandCommand(),execution.State,body,battery.Percent,home,out var action,ReasonCode.CriticalBattery);
                if(!result.Approved)
                {
                    action=safety.AuthorizeBrake(body,ReasonCode.CriticalBattery);
                    safetyState=new SafetyDecision(SafetyStatus.EmergencyLandRequired,result.Reason);
                }
                ExecuteFailsafe(action,body);return;
            }
            if(execution.State is FlightState.ReturningHome or FlightState.Landing or FlightState.Emergency)return;
            if(execution.Permission?.Cause==ReasonCode.LowBattery)return;
            var rth=safety.Authorize(new ReturnHomeCommand(),execution.State,body,battery.Percent,home,out var returning,ReasonCode.LowBattery);
            if(rth.Approved)ExecuteFailsafe(returning,body);
            else
            {
                var hold=safety.AuthorizeBrake(body,ReasonCode.LowBattery);
                ExecuteFailsafe(hold,body);
                safetyState=new SafetyDecision(SafetyStatus.ReturnHomeRequired,rth.Reason);
            }
        }
        private void ExecuteFailsafe(AuthorizedFlightAction action,BodySnapshot body)
        {
            long id=++nextId;Outcome(id,action.Command,ExecutionStatus.Accepted,action.Cause);execution.Accept(action,id,body);
        }
        private void Brake(BodySnapshot body,ReasonCode reason)
        {
            execution.Interrupt(reason,ExecutionStatus.Failed);
            var cause=execution.State==FlightState.Emergency?ReasonCode.CriticalBattery:reason;
            var action=safety.AuthorizeBrake(body,cause);
            ExecuteFailsafe(action,body);
            safetyState=new SafetyDecision(cause==ReasonCode.CriticalBattery?SafetyStatus.EmergencyLandRequired:SafetyStatus.Rejected,reason);
        }
        private void Publish()=>publisher.Publish(new TelemetrySnapshot(session,tick,tick*StepSeconds,adapter.Read(),battery.Percent,execution.State,execution.ActiveCommand,execution.ActiveId,execution.Target,home,safetyState));
    }
}

