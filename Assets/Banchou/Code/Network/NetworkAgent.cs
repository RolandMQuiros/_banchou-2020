using System;

using MessagePack;
using MessagePack.Resolvers;
using LiteNetLib;
using Newtonsoft.Json;
using Redux;
using UniRx;
using UnityEngine;

using Banchou.Board;
using Banchou.Player;

namespace Banchou.Network {
    public class NetworkAgent : MonoBehaviour, INetLogger {
        public Rollback Rollback => _rollback;
        private GetState _getState;

        private IDisposable _agent;
        private NetworkClient _client;
        private NetworkServer _server;
        private Rollback _rollback;

        public void Construct(
            IObservable<GameState> observeState,
            GetState getState,
            Dispatcher dispatch,
            PlayersActions playerActions,
            NetworkActions networkActions,
            BoardActions boardActions,
            PlayerInputStreams playerInput,
            GetTime getTime
        ) {
            _getState = getState;

            NetDebug.Logger = this;
            var messagePackOptions = MessagePackSerializerOptions
                .Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(CompositeResolver.Create(
                    BanchouMessagePackResolver.Instance,
                    MessagePack.Unity.UnityResolver.Instance,
                    StandardResolver.Instance
                ));

            var settings = JsonConvert.DefaultSettings();
            settings.TypeNameHandling = TypeNameHandling.Objects;
            var jsonSerializer = JsonSerializer.Create(settings);

            observeState
                .Select(state => state.GetNetworkMode())
                .StartWith(Mode.Local)
                .DistinctUntilChanged()
                .Subscribe(mode => {
                    if (mode != Mode.Local) {
                        _agent?.Dispose();
                        _rollback?.Dispose();
                    }

                    switch (mode) {
                        case Mode.Client:
                            _client = new NetworkClient(
                                observeState,
                                playerInput,
                                dispatch,
                                networkActions,
                                jsonSerializer,
                                messagePackOptions
                            ).Start(
                                host: getState().GetIP(),
                                pollInterval: Observable.EveryFixedUpdate(),
                                timeInterval: Observable.Interval(TimeSpan.FromSeconds(5))
                            );
                            _agent = _client;
                            _rollback = new Rollback(
                                observeState,
                                _client.ObserveRemoteActions,
                                _client.ObserveRemoteInput,
                                dispatch,
                                getTime,
                                playerInput,
                                boardActions
                            );
                            break;
                        case Mode.Server:
                            _server = new NetworkServer(
                                getState().GetNetworkId(),
                                observeState,
                                getState,
                                dispatch,
                                networkActions,
                                playerInput,
                                jsonSerializer,
                                messagePackOptions
                            ).Start(Observable.EveryFixedUpdate());
                            _agent = _server;
                            _rollback = new Rollback(
                                observeState,
                                Observable.Empty<RemoteAction>(),
                                _server.ObserveRemoteInput,
                                dispatch,
                                getTime,
                                playerInput,
                                boardActions
                            );
                            break;
                    }
                }).AddTo(this);
        }

        public float GetTime() {
            if (_rollback != null && _rollback.Phase == RollbackPhase.Resimulate) {
                return _rollback.CorrectionTime;
            }
            return _client?.GetTime() ?? _server?.GetTime() ?? Time.fixedUnscaledTime;
        }

        public float GetDeltaTime() {
            if (_rollback != null && _rollback.Phase == RollbackPhase.Resimulate) {
                return _rollback.DeltaTime;
            }
            return _getState().GetBoardTimescale() * Time.fixedUnscaledDeltaTime;
        }

        public void OnDestroy() {
            _agent?.Dispose();
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args) {
            Debug.LogFormat($"{level}{str}", args);
        }
    }
}