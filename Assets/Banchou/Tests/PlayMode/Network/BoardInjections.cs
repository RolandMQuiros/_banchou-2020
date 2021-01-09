using UnityEngine;

using Banchou.Network;
using Banchou.Player;

namespace Banchou.Test {
    public class BoardInjections : MonoBehaviour {
        public GetTime GetTime { get; private set; }
        public PlayerInputStreams PlayerInput { get; private set; }

        public void Construct(
            GetTime getTime,
            PlayerInputStreams playerInputStreams
        ) {
            GetTime = getTime;
            PlayerInput = playerInputStreams;
        }
    }
}