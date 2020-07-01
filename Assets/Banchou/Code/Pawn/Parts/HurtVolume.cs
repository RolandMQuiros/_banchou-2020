using UnityEngine;
using Redux;

using Banchou.Combatant;

namespace Banchou.Pawn.Part {
    public class HurtVolume : MonoBehaviour {
        [SerializeField] private int _strength = 0;
        [SerializeField] private Vector3 _push = Vector3.zero;
        [SerializeField] private HitMedium _medium = HitMedium.Melee;

        private PawnId _pawnId;
        private Dispatcher _dispatch;
        private CombatantActions _combatantActions;
        private Transform _orientation;

        public void Construct(
            PawnId pawnId,
            Dispatcher dispatch,
            CombatantActions combatantActions,
            Orientation orientation
        ) {
            _pawnId = pawnId;
            _dispatch = dispatch;
            _combatantActions = combatantActions;
            _orientation = orientation.transform;
        }

        private void OnTriggerEnter(Collider collider) {
            var hitVolume = collider.GetComponent<HitVolume>();
            if (hitVolume != null && _pawnId != hitVolume.PawnId) {
                Debug.Log(
                    "Hit detected!\n" +
                    $"\tTo: {hitVolume.PawnId}\n" +
                    $"\tBy: {_pawnId}\n" +
                    $"\tStrength: {_strength}\n" +
                    $"\tPush: {_push}\n" +
                    $"\tTransformed Push: {_orientation.TransformDirection(_push)}"
                );

                _dispatch(
                    _combatantActions.Hit(
                        from: _pawnId,
                        to: hitVolume.PawnId,
                        medium: _medium,
                        strength: _strength,
                        push: _orientation.TransformDirection(_push)
                    )
                );
            }
        }
    }
}