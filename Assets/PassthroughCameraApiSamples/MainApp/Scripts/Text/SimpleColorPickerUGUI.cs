// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MainApp.TextTools
{
    /// <summary>
    /// Minimal RGBA color picker built from 4 sliders (R/G/B/A, each 0-1) + a preview
    /// swatch. No wheel/gradient graphics needed, so it's cheap to build from the
    /// existing DebugSlider prefab (duplicate x4) - see Editor setup notes.
    /// Alpha = 0 gives a fully transparent color, satisfying the "transparent
    /// background" requirement without any extra UI.
    /// </summary>
    public class SimpleColorPickerUGUI : MonoBehaviour
    {
        [SerializeField] private Slider redSlider;
        [SerializeField] private Slider greenSlider;
        [SerializeField] private Slider blueSlider;
        [SerializeField] private Slider alphaSlider;
        [SerializeField] private Image previewImage;

        public event Action<Color> OnColorChanged;

        private bool m_suppressEvents;
        private bool m_initialized;

        public Color CurrentColor => new(redSlider.value, greenSlider.value, blueSlider.value, alphaSlider.value);

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void Start()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (m_initialized || !redSlider)
            {
                return;
            }

            m_initialized = true;
            redSlider.onValueChanged.AddListener(_ => HandleChanged());
            greenSlider.onValueChanged.AddListener(_ => HandleChanged());
            blueSlider.onValueChanged.AddListener(_ => HandleChanged());
            alphaSlider.onValueChanged.AddListener(_ => HandleChanged());
            UpdatePreview();
        }

        public void SetColorWithoutNotify(Color color)
        {
            m_suppressEvents = true;
            redSlider.SetValueWithoutNotify(color.r);
            greenSlider.SetValueWithoutNotify(color.g);
            blueSlider.SetValueWithoutNotify(color.b);
            alphaSlider.SetValueWithoutNotify(color.a);
            m_suppressEvents = false;
            UpdatePreview();
        }

        private void HandleChanged()
        {
            UpdatePreview();
            if (!m_suppressEvents)
            {
                OnColorChanged?.Invoke(CurrentColor);
            }
        }

        private void UpdatePreview()
        {
            if (previewImage)
            {
                previewImage.color = CurrentColor;
            }
        }
    }
}
