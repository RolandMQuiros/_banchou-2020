using Redux;
using UnityEngine;

using Banchou.Board;

namespace Banchou.Pawn {
    public class PawnSpawner : MonoBehaviour {
        [SerializeField] private string _prefabKey;
        private Dispatcher _dispatch;
        private BoardActions _boardActions;

        public void Construct(Dispatcher dispatch, BoardActions boardActions) {
            _dispatch = dispatch;
            _boardActions = boardActions;
        }

        private void Start() {
            _dispatch(_boardActions.AddPawn(_prefabKey));
        }
    }
}