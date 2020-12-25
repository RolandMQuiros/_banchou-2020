using UnityEngine;

using Banchou.Network;
using Banchou.Player;

namespace Banchou.Test {
    public class BoardInjections : MonoBehaviour {
        public GetServerTime GetTime { get; private set; }
        public PlayerInputStreams PlayerInput { get; private set; }

        public void Construct(
            GetServerTime getServerTime,
            PlayerInputStreams playerInputStreams
        ) {
            GetTime = getServerTime;
            PlayerInput = playerInputStreams;
        }
    }
}