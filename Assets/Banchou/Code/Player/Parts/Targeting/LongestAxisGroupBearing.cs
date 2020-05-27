using System.Linq;
using UnityEngine;

using Cinemachine;
using UniRx;
using UniRx.Triggers;

namespace Banchou {
    [RequireComponent(typeof(CinemachineTargetGroup))]
    public class LongestAxisBearing : MonoBehaviour {
        [SerializeField] private float _rotationSpeed = 100f;

        public void Construct() {
            var targetGroup = GetComponent<CinemachineTargetGroup>();

            // Calculate forward vector from longest separation axis
            var calculateForward = this.FixedUpdateAsObservable()
                // This involves a cross-join of all targets, so let's avoid calculating it too often
                .Select(_ => targetGroup.m_Targets.Select(t => t.target))
                .Select(targets => targets
                    .SelectMany(current => targets.Select(target => target.position - current.position))
                    .OrderByDescending(axis => axis.sqrMagnitude)
                    .FirstOrDefault()
                )
                .Where(longestAxis => longestAxis != Vector3.one)
                .Select(longestAxis => Vector3.Cross(longestAxis.normalized, Vector3.up));

            this.FixedUpdateAsObservable()
                .WithLatestFrom(calculateForward, (_, forward) => forward)
                .Subscribe(forward => {
                    if (Vector3.Dot(forward, Camera.main.transform.forward) < 0f) {
                        forward *= -1;
                    }

                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(forward, Vector3.up),
                        _rotationSpeed * Time.deltaTime
                    );
                })
                .AddTo(this);
        }
    }
}