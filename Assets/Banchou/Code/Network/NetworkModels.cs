using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Banchou.Network {
    public enum Mode : byte {
        Local,
        Server,
        Client
    }

    public enum RollbackPhase : byte {
        Complete,
        Rewind,
        Resimulate
    }

    public class NetworkState {
        public Guid Id = Guid.Empty;
        public Mode Mode = Mode.Local;
        public int PeerId = 0;
        public bool IsConnecting = false;
        public IPEndPoint IP = new IPEndPoint(127, 9050);
        public IEnumerable<Guid> Clients = Enumerable.Empty<Guid>();
        public int SimulateMinLatency = 0;
        public int SimulateMaxLatency = 0;

        public bool IsRollbackEnabled = true;
        public bool IsPhysicsRollbackEnabled = true;
        public float RollbackHistoryDuration = 1f;
        public float RollbackDetectionMinThreshold = 0.017f;
        public float RollbackDetectionMaxThreshold = 0.17f;

        public float LastUpdated = 0f;

        public NetworkState() { }
        public NetworkState(in NetworkState prev) {
            Id = prev.Id;
            Mode = prev.Mode;
            PeerId = prev.PeerId;
            IsConnecting = prev.IsConnecting;
            IP = prev.IP;
            Clients = prev.Clients;

            SimulateMinLatency = prev.SimulateMinLatency;
            SimulateMaxLatency = prev.SimulateMaxLatency;

            IsRollbackEnabled = prev.IsRollbackEnabled;
            IsPhysicsRollbackEnabled = prev.IsPhysicsRollbackEnabled;
            RollbackHistoryDuration = prev.RollbackHistoryDuration;
            RollbackDetectionMinThreshold = prev.RollbackDetectionMinThreshold;
            RollbackDetectionMaxThreshold = prev.RollbackDetectionMaxThreshold;

            LastUpdated = prev.LastUpdated;
        }
    }

    public struct RemoteAction {
        public object Action;
        public float When;
    }

    public struct ClientConnection {
        public Guid ClientNetworkId;
        public GameState State;
        public float When;
    }

    public delegate IObservable<float> ObserveBeforeResimulation();
    public delegate IObservable<float> ObserveAfterResimulation();
}