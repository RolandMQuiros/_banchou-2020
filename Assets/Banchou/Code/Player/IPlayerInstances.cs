using UnityEngine;

namespace Banchou.Player {
    public interface IPlayerInstances {
        GameObject Get(PlayerId playerId);
        void Set(PlayerId playerId, GameObject gameObject);
    }
}