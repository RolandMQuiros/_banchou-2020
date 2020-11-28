using System;
using System.Collections.Generic;
using System.Linq;

using UniRx;
using UnityEngine;

using Banchou.Player;
using Banchou.Network;

namespace Banchou.Pawn.Part {
    public class Rollback : MonoBehaviour {
        [SerializeField] private float _historyWindow = 3f;
        [SerializeField] private float _rollbackThreshold = 0.05f;

        private PawnId _pawnId;
        private GetServerTime _getServerTime;
        private LinkedList<PawnFSMState> _history = new LinkedList<PawnFSMState>();
        public PawnRollbackState State { get; private set; }

        public float FastForwardStartTime { get; private set; }
        public float FastForwardCurrentTime { get; private set; }

        [SerializeField] private PawnFSMState[] _debugHistory = null;

        [Serializable]
        private struct InputUnit {
            public InputCommand Command;
            public Vector3 Move;
            public float When;
            public float Diff;
        }
        [SerializeField] private InputUnit[] _debugBuffer = null;

        public Rollback PushStateChange(int stateHash, bool isLoop, float clipLength, float when) {
            while (_history.Count > 1 && _history.First.Value.FixedTimeAtChange < _getServerTime() - _historyWindow) {
                _history.RemoveFirst();
            }

            // Only distinct changes
            if (_history.Count == 0 || _history.Last.Value.StateHash != stateHash) {
                _history.AddLast(
                    new PawnFSMState {
                        PawnId = _pawnId,
                        StateHash = stateHash,
                        IsLoop = isLoop,
                        ClipLength = clipLength,
                        FixedTimeAtChange = when
                    }
                );
            }

            _debugHistory = _history.ToArray();

            return this;
        }

        public void Construct(
            PawnId pawnId,
            IObservable<GameState> observeState,
            PlayerInputStreams playerInput,
            Subject<InputCommand> commandSubject,
            Subject<Vector3> moveSubject,
            GetServerTime getServerTime,
            Animator animator = null
        ) {
            _getServerTime = getServerTime;

            if (animator != null) {
                var movesAndCommands = observeState
                    .Select(state => state.GetPawnPlayerId(pawnId))
                    .DistinctUntilChanged()
                    .SelectMany(
                        playerId => playerInput.ObserveCommand(playerId)
                            .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                            .Select(unit => new InputUnit {
                                Command = unit.Command,
                                When = unit.When,
                                Diff = getServerTime() - unit.When
                            })
                            .Merge(playerInput.ObserveMove(playerId)
                                .Scan((prev, unit) => unit.When > prev.When ? unit : prev)
                                .Select(unit => new InputUnit {
                                    Move = unit.Move,
                                    When = unit.When,
                                    Diff = getServerTime() - unit.When
                                })
                            )
                    );

                // Handle rollbacks
                movesAndCommands
                    .CatchIgnoreLog()
                    .Subscribe(unit => {
                        if (unit.Diff > _rollbackThreshold) {
                            var now = getServerTime();
                            var deltaTime = Mathf.Min(unit.Diff, Time.fixedUnscaledDeltaTime);

                            var targetState = _history.Aggregate((target, step) => {
                                if (unit.When > step.FixedTimeAtChange) {
                                    return step;
                                }
                                return target;
                            });

                            // Tell the RecordStateHistory FSMBehaviours to stop recording
                            State = PawnRollbackState.RollingBack;

                            // Revert to state when desync happened
                            animator.Play(
                                stateNameHash: targetState.StateHash,
                                layer: 0,
                                normalizedTime: (now - targetState.FixedTimeAtChange - deltaTime)
                                    / targetState.ClipLength
                            );

                            // Tells the RecordStateHistory FSMBehaviours to start recording again
                            State = PawnRollbackState.FastForward;
                            FastForwardStartTime = now - unit.Diff;

                            // Kick off the fast-forward. Need to run this before pushing the commands so the _animator.Play can take
                            animator.Update(deltaTime);

                            // Pump input into streams
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                            } else {
                                commandSubject.OnNext(unit.Command);
                            }

                            // Resimulate to present
                            var resimulationTime = 0f;
                            int frames = 0;
                            while (resimulationTime < unit.Diff) {
                                animator.Update(Mathf.Min(deltaTime, unit.Diff - resimulationTime));
                                resimulationTime = Mathf.Min(resimulationTime + deltaTime, unit.Diff);
                                FastForwardStartTime = FastForwardCurrentTime + resimulationTime;
                                frames++;
                            }

                            Debug.Log($"Resimulated {frames} frames, over {unit.Diff} seconds\nCommand: {unit.Command}\nMove: {unit.Move}\nWhen:{unit.When}\nNow:{_getServerTime()}");
                            State = PawnRollbackState.Complete;
                        } else {
                            if (unit.Command == InputCommand.None) {
                                moveSubject.OnNext(unit.Move);
                            } else {
                                commandSubject.OnNext(unit.Command);
                            }
                        }
                    })
                    .AddTo(this);
            }
        }
    }
}