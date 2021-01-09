using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using UniRx;
using UnityEngine;
using UnityEngine.TestTools;

using Banchou.Player;

namespace Banchou.Test {
    public class DeterminismTests {
        [UnityTest]
        public IEnumerator RigidbodyMoveDeterminism() {
            var platform = new GameObject("Terrain", typeof(BoxCollider));
            platform.transform.position = new Vector3(0f, -0.5f, 0f);
            platform.transform.localScale = new Vector3(20f, 1f, 20f);

            var first = new GameObject("First", typeof(Rigidbody), typeof(CapsuleCollider)).GetComponent<Rigidbody>();
            first.useGravity = true;
            first.transform.position = new Vector3(-5f, 0f, 0f);
            var second = new GameObject("Second", typeof(Rigidbody), typeof(CapsuleCollider)).GetComponent<Rigidbody>();
            second.useGravity = true;
            second.transform.position = new Vector3(5f, 0f, 0f);

            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(first.position, first.transform.position);
            Assert.AreEqual(second.position, second.transform.position);
            Assert.AreEqual(first.position.z, second.position.z);

            yield return new WaitForSecondsRealtime(1f);
            Assert.AreEqual(first.position, first.transform.position);
            Assert.AreEqual(second.position, second.transform.position);
            Assert.AreEqual(first.position.z, second.position.z);

            var offset = Vector3.forward * Time.fixedDeltaTime;
            var firstDestination = first.position + offset;
            var secondDestination = second.position + offset;

            first.MovePosition(firstDestination);
            second.MovePosition(secondDestination);

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(first.position, firstDestination);
            Assert.AreEqual(second.position, secondDestination);

            Assert.AreEqual(first.position.z, second.position.z);

            Assert.AreEqual(16.6700000762939453125f, 16.67f);

            yield return null;
        }

        [Test]
        public void DistinctStructsUntilChanged() {
            Subject<InputUnit> subject = new Subject<InputUnit>();
            int subscribeCalls = 0;
            var sub = subject
                .DistinctUntilChanged()
                .Subscribe(unit => {
                    subscribeCalls++;
                    Assert.AreEqual(1, subscribeCalls);
                });

            subject.OnNext(new InputUnit {
                PlayerId = new PlayerId(12345),
                Type = InputUnitType.Command,
                Direction = new Vector3(123f, 321f, 456f),
                When = 16.67f
            });

            subject.OnNext(new InputUnit {
                PlayerId = new PlayerId(12345),
                Type = InputUnitType.Command,
                Direction = new Vector3(123f, 321f, 456f),
                When = 16.67f
            });
        }
    }
}
