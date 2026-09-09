using System;
using MicroHawk.Contracts;
using UnityEngine;

namespace MicroHawk.Simulation
{
    public sealed class TelemetryPublisher
    {
        public TelemetrySnapshot Latest { get; private set; }
        public event Action<TelemetrySnapshot> Published;
        public event Action<CommandResult> CommandChanged;
        public void Publish(TelemetrySnapshot snapshot){Latest=snapshot;Notify(Published,snapshot);}
        public void Outcome(CommandResult result)=>Notify(CommandChanged,result);
        private static void Notify<T>(Action<T> handlers,T item)
        {
            if(handlers==null)return;
            foreach(Action<T> handler in handlers.GetInvocationList())
                try{handler(item);}catch(Exception exception){Debug.LogException(exception);}
        }
    }
}
