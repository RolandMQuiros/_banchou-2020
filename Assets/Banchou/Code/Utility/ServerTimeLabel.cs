using System;
using TMPro;
using UniRx;
using UnityEngine;

using Banchou.Network;

namespace Banchou.Prototype {
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ServerTimeLabel : MonoBehaviour {
        public void Construct(
            GetTime getTime
        ) {
            var label = GetComponent<TextMeshProUGUI>();
            Observable.Interval(TimeSpan.FromMilliseconds(100))
                .CatchIgnoreLog()
                .Subscribe(_ => {
                    label.text = $"Server time: {getTime()}\nLocal time: {Time.fixedTime}";
                })
                .AddTo(this);
        }
    }
}