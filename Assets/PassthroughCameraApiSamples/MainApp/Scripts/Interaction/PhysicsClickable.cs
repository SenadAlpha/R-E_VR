// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MainApp.Interaction
{
    /// <summary>
    /// World-space click target for the controller laser. Independent of UGUI.
    /// </summary>
    public class PhysicsClickable : MonoBehaviour
    {
        public UnityEvent OnClick = new();

        public void Click()
        {
            OnClick?.Invoke();
        }
    }
}
