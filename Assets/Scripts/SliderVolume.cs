using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SliderVolume : MonoBehaviour
{
    public Slider slider;
    public AudioMixerGroup mixerGroup;
    public string floatString;

    private void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        mixerGroup.audioMixer.SetFloat(floatString, value);
    }
}
