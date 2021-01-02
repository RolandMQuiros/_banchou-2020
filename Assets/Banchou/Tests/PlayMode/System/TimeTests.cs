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
                    var startTime = Time.fixedUnscaledTime;
                    Thread.Sleep(1000);
                    var endTime = Time.fixedUnscaledTime;
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
                        Assert.AreEqual(Time.fixedUnscaledTime % Time.fixedUnscaledDeltaTime, 0f);
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
                    .Select(_ => Snapping.Snap(Time.fixedUnscaledTime, Time.fixedUnscaledDeltaTime))
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

            var subscriptions = new CompositeDisposable(
                Observable.EveryFixedUpdate()
                    .Subscribe(frameCount => {
                        Thread.Sleep((int)(10 * frameCount));
                    }),
                Observable.EveryFixedUpdate()
                    .Select(_ => Time.fixedUnscaledDeltaTime)
                    .Pairwise()
                    .Subscribe(times => {
                        Assert.Greater(times.Current, times.Previous);
                    }),
                Observable.Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => { finished = true; })
            );

            yield return new WaitUntil(() => finished);
            subscriptions.Dispose();
        }
    }
}
