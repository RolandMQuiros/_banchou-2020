using UnityEngine;
using UniRx;
using UniRx.Triggers;

using Banchou;
using Banchou.Network;
using Banchou.Pawn;
using Banchou.Player;

public class TestDelayCommand : MonoBehaviour
{
    private PlayerId _playerId;
    private PlayerInputStreams _playerInput;

    // Start is called before the first frame update
    public void Construct(GetState getState, PlayerInputStreams playerInput, PawnId pawnId, GetServerTime getServerTime) {
        _playerInput = playerInput;

        this.UpdateAsObservable()
            .Subscribe(_ => {
                if (Input.GetKeyDown(KeyCode.Backslash)) {
                    _playerId = getState().GetPawnPlayerId(pawnId);
                    _playerInput.PushCommand(_playerId, InputCommand.LightAttack, getServerTime() - 0.2f);
                }

                Vector3 move = new Vector3(
                    (Input.GetKeyDown(KeyCode.Keypad6) ? 1f : 0f) - (Input.GetKeyDown(KeyCode.Keypad4) ? 1f : 0f),
                    0f,
                    (Input.GetKeyDown(KeyCode.Keypad8) ? 1f : 0f) - (Input.GetKeyDown(KeyCode.Keypad2) ? 1f : 0f)
                );

                if (move != Vector3.zero) {
                    _playerInput.PushMove(getState().GetPawnPlayerId(pawnId), move, getServerTime() - 0.2f);
                }
            })
            .AddTo(this);
    }
}
