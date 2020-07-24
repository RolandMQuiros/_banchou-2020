using System;
using UnityEngine;
using UniRx;
using UniRx.Diagnostics;
using UniRx.Triggers;

using Banchou;
using Banchou.Pawn;
using Banchou.Player;

public class TestDelayCommand : MonoBehaviour
{
    private PlayerId _playerId;
    private PlayerInputStreams _playerInput;

    // Start is called before the first frame update
    public void Construct(GetState getState, PlayerInputStreams playerInput, PawnId pawnId) {
        _playerInput = playerInput;

        this.UpdateAsObservable()
            .Subscribe(_ => {
                if (Input.GetKeyDown(KeyCode.Backslash)) {
                    _playerId = getState().GetPawnPlayerId(pawnId);
                    Fire();
                }
            })
            .AddTo(this);
    }

    public void Fire() {
        _playerInput.PushCommand(_playerId, InputCommand.LightAttack, Time.fixedUnscaledTime - 0.2f);
    }
}
