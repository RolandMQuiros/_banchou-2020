﻿using System;
using TMPro;
using UniRx;
using UnityEngine;

using Banchou.Network;

namespace Banchou.Prototype {
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ServerTimeLabel : MonoBehaviour {
        public void Construct(
            GetServerTime getServerTime
        ) {
            var label = GetComponent<TextMeshProUGUI>();
            Observable.Interval(TimeSpan.FromSeconds(1))
                .CatchIgnoreLog()
                .Subscribe(_ => {
                    label.text = $"Server time: {getServerTime()}\nLocal time: {Time.fixedUnscaledTime}";
                })
                .AddTo(this);
        }
    }
}