using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;

using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

namespace Banchou.Test {
    public class TimeTests {
        [UnityTest]
        public IEnumerator IsTimeConstantWithinFrame() {
            var finished = false;

            var subscription = Observable.EveryFixedUpdate()
                .Subscribe(_ => {
                    var startTime = Time.fixedTime;
                    Thread.Sleep(1000);
                    var endTime = Time.fixedTime;
                    Assert.AreEqual(startTime, endTime);
                    finished = true;
                });

            yield return new WaitUntil(() => finished);

            subscription.Dispose();
        }

        [UnityTest]
        public IEnumerator IsFixedTimeDivisibleByDelta() {
            var finished = false;

            var subscriptions = new CompositeDisposable(
                Observable.EveryFixedUpdate()
                    .Subscribe(_ => {
                        Assert.AreEqual(Time.fixedTime % Time.fixedDeltaTime, 0f);
                    }),
                Observable.Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => { finished = true; })
            );

            yield return new WaitUntil(() => finished);

            subscriptions.Dispose();
        }

        [UnityTest]
        public IEnumerator DoesSnappingTimeCauseRepeats() {
            var finished = false;

            var subscriptions = new CompositeDisposable(
                Observable.EveryFixedUpdate()
                    .Select(_ => Snapping.Snap(Time.fixedTime, Time.fixedDeltaTime))
                    .Pairwise()
                    .Subscribe(times => {
                        Assert.AreNotEqual(times.Current, times.Previous);
                    }),
                Observable.Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => { finished = true; })
            );

            yield return new WaitUntil(() => finished);
            subscriptions.Dispose();
        }

        [UnityTest]
        public IEnumerator DoesFixedDeltaTimeChangeWithFatFrames() {
            var finished = false;

            var targetDelta = Time.fixedDeltaTime;

            var subscriptions = new CompositeDisposable(
                Observable.EveryFixedUpdate()
                    .Subscribe(frameCount => {
                        Thread.Sleep((int)(10 * frameCount));
                    }),
                Observable.EveryFixedUpdate()
                    .Select(_ => Time.fixedDeltaTime)
                    .Subscribe(delta => {
                        Assert.AreEqual(targetDelta, delta);
                    }),
                Observable.Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => { finished = true; })
            );

            yield return new WaitUntil(() => finished);
            subscriptions.Dispose();
        }

        [UnityTest]
        public IEnumerator IsFixedUpdateCalledWithPhysicsAutoSimDisabled() {
            Physics.autoSimulation = false;
            var finished = false;
            var targetDelta = Time.fixedDeltaTime;
            var subscriptions = new CompositeDisposable (
                Observable.EveryFixedUpdate()
                    .Subscribe(_ => {
                        Assert.AreEqual(0f, Time.fixedDeltaTime);
                    }),
                Observable.Timer(TimeSpan.FromSeconds(1))
                    .Subscribe(_ => { finished = true; })
            );
            yield return new WaitUntil(() => finished);
            Physics.autoSimulation = true;
            subscriptions.Dispose();
        }

        [UnityTest]
        public IEnumerator DoesPhysicsResimulateCallFixedUpdate() {
            Physics.autoSimulation = false;
            var finished = false;
            var subscriptions = new CompositeDisposable (
                Observable.Interval(TimeSpan.FromSeconds(0.03f))
                    .Subscribe(_ => {
                        Physics.Simulate(0.03f);
                    }),
                Observable.EveryFixedUpdate()
                    .Subscribe(_ => {
                        Assert.AreEqual(0.03f, Time.fixedDeltaTime);
                    })
            );
            yield return new WaitUntil(() => finished);
            Physics.autoSimulation = true;
            subscriptions.Dispose();
        }
    }
}
