﻿using System;
using System.Linq;
using Redux;

using UnityEngine;
using UnityEngine.AI;
using UniRx;
using UniRx.Diagnostics;

using Banchou.DependencyInjection;
using Banchou.Network;
using Banchou.Network.Message;
using Banchou.Pawn.Part;
using Banchou.Player;

namespace Banchou.Pawn {
    public delegate float GetDeltaTime();

    public class PawnContext : MonoBehaviour, IContext, IPawnInstance {
        [SerializeField] private string _pawnId = string.Empty;

        [Header("Binding overrides")]
        [SerializeField] private Animator _animator = null;
        [SerializeField] private Rigidbody _rigidbody = null;
        [SerializeField] private Part.Orientation _orientation = null;
        [SerializeField] private NavMeshAgent _agent = null;
        private IMotor _motor = null;

        public PawnId PawnId { get; private set; }
        public Rigidbody Body { get => _rigidbody; }
        public Transform Orientation { get => _orientation.transform; }
        public NavMeshAgent Agent { get => _agent; }
        public IMotor Motor { get => _motor; }

        public Vector3 Position { get => _rigidbody?.position ?? transform.position; }
        public Vector3 Forward { get => _orientation?.transform.forward ?? transform.forward; }

        private Dispatcher _dispatch;
        private GetState _getState;
        private PawnActions _pawnActions;
        private IObservable<GameState> _observeState;
        private PlayerInputStreams _playerInput;

        private Subject<InputCommand> _commandSubject = new Subject<InputCommand>();
        private float _deltaTime = 0f;

        public void Construct(
            PawnId pawnId,
            Dispatcher dispatch,
            GetState getState,
            IObservable<GameState> onStateUpdate,
            PlayerInputStreams playerInput,
            IObservable<SyncPawn> onPawnSync = null
        ) {
            PawnId = pawnId;
            _dispatch = dispatch;
            _getState = getState;
            _observeState = onStateUpdate;
            _pawnActions = new PawnActions(PawnId);
            _playerInput = playerInput;

            _animator = _animator == null ? GetComponentInChildren<Animator>(true) : _animator;
            _rigidbody =  _rigidbody == null ? GetComponentInChildren<Rigidbody>(true) : _rigidbody;
            _orientation = _orientation == null ? GetComponentInChildren<Part.Orientation>(true) : _orientation;
            _agent = _agent == null ? GetComponentInChildren<NavMeshAgent>(true) : _agent;
            _motor = _motor == null ? GetComponentInChildren<Part.IMotor>(true) : _motor;

            if (_agent != null) {
                _agent.updatePosition = false;
                _agent.updateRotation = false;
            }

            if (onPawnSync != null) {
                onPawnSync
                    .Where(syncPawn => syncPawn.PawnId == PawnId)
                    .CatchIgnoreLog()
                    .Subscribe(syncPawn => {
                        transform.position = syncPawn.Position;
                        if (_orientation != null) {
                            _orientation.transform.rotation = Quaternion.LookRotation(syncPawn.Forward);
                        } else {
                            transform.rotation = Quaternion.LookRotation(syncPawn.Forward);
                        }
                    })
                    .AddTo(this);
            }

            _pawnId = PawnId.ToString();
        }

        public void InstallBindings(DiContainer container) {
            container.Bind<PawnId>(PawnId);
            container.Bind<IPawnInstance>(this);
            container.Bind<Animator>(_animator);
            container.Bind<Rigidbody>(_rigidbody);
            container.Bind<Part.Orientation>(_orientation);
            container.Bind<NavMeshAgent>(_agent);
            container.Bind<Part.IMotor>(_motor);
            container.Bind<PawnActions>(_pawnActions);
            container.Bind<GetDeltaTime>(() => _deltaTime);

            // Short-circuit dispatcher for client facade pawns
            container.Bind<Redux.Dispatcher>(action => {
                if (_getState().IsServer()) {
                    return _dispatch(action);
                }
                return action;
            });

            container.Bind<ObservePlayerLook>(
                () => _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(playerId => _playerInput.ObserveLook(playerId).Select(unit => unit.Look))
            );

            container.Bind<ObservePlayerMove>(
                () => _observeState
                    .Select(state => state.GetPawnPlayerId(PawnId))
                    .DistinctUntilChanged()
                    .SelectMany(playerId => _playerInput
                        .ObserveMove(playerId)
                        // .Do(move => Debug.Log($"{move.Move} -- {move.When}"))
                        // Make sure we only handle latest, in case they come in out-of-order over the network
                        // .Scan((prev, move) => move.When > prev.When ? move : prev)
                        .Select(unit => unit.Move)
                    )
            );

            container.Bind<ObservePlayerCommand>(() => _commandSubject);
            container.Bind<Subject<InputCommand>>(_commandSubject, t => t == typeof(Part.Rollback));
        }

        private void OnDrawGizmos() {
            if (_agent != null) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_agent.nextPosition, 0.25f);

                if (_agent.path?.corners != null) {
                    Gizmos.color = Color.blue;
                    for (int i = 0; i < _agent.path.corners.Length; i++) {
                        Gizmos.DrawWireSphere(_agent.path.corners[i], 0.25f);
                    }
                }
            }
        }
    }
}