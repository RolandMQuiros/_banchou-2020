using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Banchou.Network {
    public enum InstanceType {
        Local,
        Server,
        Client
    }

    public class NetworkSettingsState {
        public InstanceType InstanceType = InstanceType.Server;

        public NetworkSettingsState() { }
        public NetworkSettingsState(in NetworkSettingsState prev) {
            InstanceType = prev.InstanceType;
        }
    }
}