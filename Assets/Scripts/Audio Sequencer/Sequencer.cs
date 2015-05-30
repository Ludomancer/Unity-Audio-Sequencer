#region Author
/************************************************************************************************************
Author: Nidre (Erdin Kacan)
Website: http://erdinkacan.tumblr.com/
GitHub: https://github.com/Nidre
Behance : https://www.behance.net/erdinkacan
************************************************************************************************************/
#endregion
#region Copyright
/************************************************************************************************************
The MIT License (MIT)
Copyright (c) 2015 Erdin
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
************************************************************************************************************/
#endregion

using UnityEngine.UI;
using System.Collections;
using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
internal class Sequencer : SequencerBase
{

    #region Enumerations

    [Flags]
    public enum FadeTarget
    {
        Play = (1 << 0),
        Stop = (1 << 1),
        Mute = (1 << 2),
        UnMute = (1 << 3),
        Pause = (1 << 4),
        UnPause = (1 << 5)
    }

    #endregion

    #region Events and Delegates
    /// <summary>
    /// Event to be fired on non-empty steps.
    /// </summary>
    public Action OnBeat;
    /// <summary>
    /// Event to be fired on every step.
    /// </summary>
    public Action OnAnyStep;
    #endregion

    #region Variables
    /// <summary>
    /// Audio clip to be played by this sequencer.
    /// </summary>
    public AudioClip clip;
    /// <summary>
    /// Low signature.
    /// </summary>
    public int signatureLo = 4;
    /// <summary>
    /// Sequence of steps.
    /// True = Play
    /// False = Silent
    /// </summary>
    public bool[] sequence;
    /// <summary>
    /// Maximum back buffer allowed.
    /// </summary>
    [Range(0, 100)]
    public int maxBackBufferSize = 0;
    /// <summary>
    /// Enlarge backbuffer count by number specified.
    /// </summary>
    [Range(0, 100)]
    public int increaseBackBufferBy = 0;
    /// <summary>
    /// Fade in duration from muted to unmuted.
    /// </summary>
    [Range(0, 60)]
    public float fadeInDuration;
    /// <summary>
    /// Fade in duration from unmuted to muted.
    /// </summary>
    [Range(0, 60)]
    public float fadeOutDuration;
    /// <summary>
    /// When to trigger fade.
    /// </summary>
    [BitMask]
    public FadeTarget fadeWhen;
    /// <summary>
    /// Current step.
    /// </summary>
    private int _currentStep;
    /// <summary>
    /// Time of next tick.
    /// </summary>
    private double _nextTick;
    /// <summary>
    /// Sample rate.
    /// </summary>
    private double _sampleRate;
    /// <summary>
    /// Current index of clip data.
    /// </summary>
    private int _index;
    /// <summary>
    /// Clip data.
    /// </summary>
    private float[] _clipData;
    /// <summary>
    /// List of backbuffers.
    /// </summary>
    private List<BackBuffer> _backBuffer;
    /// <summary>
    /// Remaining beat events to be fired.
    /// </summary>
    private int _fireBeatEvent;
    /// <summary>
    /// Remaining any step events to be fired.
    /// </summary>
    private int _fireAnyStepEvent;
    /// <summary>
    /// Progress used to calculate approximate percentage.
    /// </summary>
    private double _progress;
    /// <summary>
    /// Temporary variable to set percentage on Audio Thread.
    /// </summary>
    private double _newPercentage = -1;
    /// <summary>
    /// Initial volume value to fade in.
    /// </summary>
    private float _initialVolumeValue;
    /// <summary>
    /// Volume of audio source just before fading in or out
    /// </summary>
    private float _volumeBeforeFade;
    /// <summary>
    /// Target volume when fade in/or finishes.
    /// </summary>
    private float _volumeAfterFade;
    /// <summary>
    /// Curernt percentage of fade progress.
    /// </summary>
    private float _fadeProgress = 1;
    /// <summary>
    /// Current fade speed;
    /// </summary>
    private float _fadeSpeed;
    /// <summary>
    /// What are we fading into.
    /// </summary>
    private FadeTarget _fadeTarget;
    /// <summary>
    /// Attached audio source.
    /// </summary>
    private AudioSource _audioSource;
    #endregion

    #region Properties
    /// <summary>
    /// True if clip data is loaded.
    /// </summary>
    public override bool IsReady
    {
        get { return _clipData != null; }
    }
    #endregion

    #region Methods

    public override void OnAwake()
    {
#if UNITY_EDITOR
        _isMutedOld = this.isMuted;
        _oldBpm = this.bpm;
#endif
        StartCoroutine(Init());
    }

    /// <summary>
    /// Wait until sequencer is ready.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Init()
    {
        _audioSource = GetComponent<AudioSource>();
        _initialVolumeValue = _audioSource.volume;
        _volumeAfterFade = _initialVolumeValue;
        _sampleRate = AudioSettings.outputSampleRate;
        _audioSource.volume = 0;
        if (clip == null)
        {
            clip = _audioSource.clip;
        }
        if (clip != null)
        {
            while (_clipData == null)
            {
                if (clip.loadState == AudioDataLoadState.Loaded)
                {
                    _clipData = new float[clip.samples * clip.channels];
                    clip.GetData(_clipData, 0);
                }
                yield return null;
            }
            if (playWhenReady)
            {
                Play();
            }
            OnReady();
        }
        else Debug.LogWarning("Audio Clip can not be null.");
    }

    public void SetAudioClip(AudioClip newClip)
    {
        clip = newClip;
        if (clip != null)
        {
            _clipData = new float[clip.samples * clip.channels];
            clip.GetData(_clipData, 0);
        }
        else _clipData = null;
    }

    /// <summary>
    /// Set mute state.
    /// </summary>
    /// <param name="isMuted"></param>
    public override void Mute(bool isMuted)
    {
        Mute(isMuted, isMuted ? fadeOutDuration : fadeInDuration);
    }

    /// <summary>
    ///  Toggle mute state.
    /// </summary>
    /// <param name="isMuted"></param>
    /// <param name="fadeDuration"></param>
    public override void Mute(bool isMuted, float fadeDuration)
    {
        if (isMuted && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Mute))
        {
            _fadeTarget = FadeTarget.Mute;
            FadeOut(fadeDuration);
        }
        else if (!isMuted && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.UnMute))
        {
            _fadeTarget = FadeTarget.UnMute;
            FadeIn(fadeDuration);
        }
        else
        {
            _audioSource.volume = isMuted ? 0 : _initialVolumeValue;
            _fadeProgress = 1;
            MuteInternal(isMuted);
        }
    }

    /// <summary>
    /// Changes default fade in and fade out durations.
    /// </summary>
    /// <param name="fadeIn"></param>
    /// <param name="fadeOut"></param>
    public override void SetFadeDurations(float fadeIn, float fadeOut)
    {
        fadeInDuration = fadeIn;
        fadeOutDuration = fadeOut;
    }

    private void MuteInternal(bool isMuted)
    {
        this.isMuted = isMuted;
#if UNITY_EDITOR
        _isMutedOld = this.isMuted;
#endif
    }

    /// <summary>
    /// Start playing.
    /// </summary>
    public override void Play()
    {
        Play(fadeInDuration);
    }

    /// <summary>
    /// Start playing from specified percentage.
    /// </summary>
    /// <param name="newPercentage"></param>
    public override void Play(double newPercentage)
    {
        SetPercentage(newPercentage);
        Play();
    }

    /// <summary>
    /// Start playing.
    /// </summary>
    /// <param name="fadeDuration"></param>
    public override void Play(float fadeDuration)
    {
        if (!IsPlaying && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Play))
        {
            _fadeTarget = FadeTarget.Play;
            PlayInternal();
            FadeIn(fadeDuration);
        }
        else
        {
            _audioSource.volume = isMuted ? 0 : _initialVolumeValue;
            _fadeProgress = 1;
            PlayInternal();
        }
    }

    private void PlayInternal()
    {
        _nextTick = AudioSettings.dspTime * _sampleRate;
        if (_clipData == null)
        {
            _clipData = new float[clip.samples * clip.channels];
            clip.GetData(_clipData, 0);
        }
        _audioSource.Play();
        _isPlaying = true;
    }

    /// <summary>
    /// Stop playing.
    /// </summary>
    public override void Stop()
    {
        Stop(fadeOutDuration);
    }

    /// <summary>
    /// Stop playing.
    /// </summary>
    /// <param name="fadeDuration"></param>
    public override void Stop(float fadeDuration)
    {
        if (IsPlaying && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Stop))
        {
            _fadeTarget = FadeTarget.Stop;
            FadeOut(fadeDuration);
        }
        else
        {
            _audioSource.volume = isMuted ? 0 : _initialVolumeValue;
            _fadeProgress = 1;
            StopInternal();
        }
    }

    private void StopInternal()
    {
        _isPlaying = false;
        _audioSource.Stop();
        _clipData = null;
        _index = 0;
        _currentStep = 0;
        if (_backBuffer != null)
        {
            _backBuffer.Clear();
            _backBuffer = null;
        }
    }

    /// <summary>
    /// Pause/Unpause.
    /// </summary>
    /// <param name="isPaused"></param>
    public override void Pause(bool isPaused)
    {
        Pause(isPaused, isPaused ? fadeOutDuration : fadeInDuration);
    }

    /// <summary>
    /// Pause/Unpause.
    /// </summary>
    /// <param name="isPaused"></param>
    /// <param name="fadeDuration"></param>
    public override void Pause(bool isPaused, float fadeDuration)
    {
        if (isPaused && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.Pause))
        {
            _fadeTarget = FadeTarget.Pause;
            FadeOut(fadeDuration);
        }
        else if (!isPaused && fadeDuration > 0 && fadeWhen.IsFlagSet(FadeTarget.UnPause))
        {
            _fadeTarget = FadeTarget.UnPause;
            PauseInternal(false);
            FadeIn(fadeDuration);
        }
        else
        {
            _audioSource.volume = isMuted ? 0 : _initialVolumeValue;
            _fadeProgress = 1;
            PauseInternal(isPaused);
        }
    }

    private void PauseInternal(bool isPaused)
    {
        if (isPaused)
        {
            _audioSource.Pause();
            _isPlaying = false;
        }
        else
        {
            _audioSource.UnPause();
            _isPlaying = true;
        }
    }

    /// <summary>
    /// Toggle mute state.
    /// </summary>
    public override void ToggleMute()
    {
        isMuted = !isMuted;
    }

    private void FadeIn(float duration)
    {
        _fadeSpeed = 1f / duration;
        _fadeProgress = 0;
        MuteInternal(false);
        _volumeBeforeFade = _audioSource.volume;
        _volumeAfterFade = _initialVolumeValue;
    }

    private void FadeOut(float duration)
    {
        _fadeSpeed = 1f / duration;
        _fadeProgress = 0;
        _volumeBeforeFade = _audioSource.volume;
        _volumeAfterFade = 0;
    }

    /// <summary>
    /// Get approximate percentage.
    /// </summary>
    /// <returns>Approximate percentage.</returns>
    public double GetPercentage()
    {
        double samplesTotal = _sampleRate * 60.0F / bpm * 4.0F;
        return _progress / samplesTotal;
    }

    /// <summary>
    /// Set approximate percentage.
    /// Ignores leftover percentage from rounding. Not precise.
    /// </summary>
    /// <param name="percentage">Approximate percentage.</param>
    public override void SetPercentage(double percentage)
    {
        _newPercentage = percentage;
    }

    /// <summary>
    /// Updates percentage of the sequence on Audio Thread.
    /// </summary>
    private void UpdatePercentage()
    {
        _index = 0;
        if (_backBuffer != null) _backBuffer.Clear();

        double samplesTotal = _sampleRate * 60.0F / bpm * 4.0F;
        double samplesPerTick = samplesTotal / signatureLo;
        double newSamplePos = samplesTotal * _newPercentage;
        double currentTickDouble = newSamplePos / samplesPerTick;
        _currentStep = (int)Math.Round(currentTickDouble, MidpointRounding.ToEven);
        if (log) print("Set Percentage: " + _currentStep + " (%" + _newPercentage + ")");
        _newPercentage = -1;
    }

    private void Update()
    {
        while (_fireAnyStepEvent > 0)
        {
            _fireAnyStepEvent--;
            OnAnyStep();
        }
        while (_fireBeatEvent > 0)
        {
            _fireBeatEvent--;
            OnBeat();
        }
        if (_fadeProgress < 1)
        {
            _fadeProgress += Time.deltaTime * _fadeSpeed;
            if (_fadeProgress > 1) _fadeProgress = 1;
            _audioSource.volume = Mathf.Lerp(_volumeBeforeFade, _volumeAfterFade, _fadeProgress);
            if (_fadeProgress == 1)
            {
                switch (_fadeTarget)
                {
                    case FadeTarget.Play:
                    case FadeTarget.UnPause:
                    case FadeTarget.UnMute:
                        //Done on start of Fade.
                        break;
                    case FadeTarget.Stop:
                        StopInternal();
                        break;
                    case FadeTarget.Mute:
                        MuteInternal(true);
                        break;
                    case FadeTarget.Pause:
                        PauseInternal(true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }

    /// <summary>
    /// Set Bpm.
    /// </summary>
    /// <param name="newBpm">Beats per minute.</param>
    public override void SetBpm(int newBpm)
    {
        if (newBpm < 10) newBpm = 10;
        bpm = newBpm;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!IsReady || !_isPlaying) return;
        double samplesPerTick = _sampleRate * 60.0F / bpm * 4.0F / signatureLo;
        double sample = AudioSettings.dspTime * _sampleRate;
        if (_newPercentage > -1)
        {
            UpdatePercentage();
            return;
        }

        if (isMuted)
        {
            int dataLeft = data.Length;
            while (dataLeft > 0)
            {
                double newSample = sample + dataLeft;
                if (_nextTick < newSample)
                {
                    dataLeft = (int)(newSample - _nextTick);
                    _nextTick += samplesPerTick;
                    if (++_currentStep > signatureLo) _currentStep = 1;
                    _progress = _currentStep * samplesPerTick;
                    if (sequence[_currentStep - 1])
                    {
                        _index = 0;
                        if (OnBeat != null) _fireBeatEvent++;
                    }
                    else
                    {
                        _index = -1;
                    }
                    if (OnAnyStep != null) _fireAnyStepEvent++;
                }
                else break;
            }
        }
        else
        {
            for (int dataIndex = 0; dataIndex < data.Length; dataIndex++)
            {
                if (_backBuffer != null)
                {
                    for (int backBufferIndex = 0; backBufferIndex < _backBuffer.Count; backBufferIndex++)
                    {
                        data[dataIndex] += _backBuffer[backBufferIndex].data[_backBuffer[backBufferIndex].index];

                        _backBuffer[backBufferIndex].index += 1;
                        if (_backBuffer[backBufferIndex].index >= _backBuffer[backBufferIndex].data.Length)
                        {
                            _backBuffer.RemoveAt(backBufferIndex);
                            backBufferIndex--;
                            if (log) print("BackBuffer removed. Total: " + _backBuffer.Count);
                        }
                    }
                }

                if (_index != -1)
                {
                    data[dataIndex] += _clipData[_index];

                    _index++;
                    if (_index >= _clipData.Length)
                    {
                        _index = -1;
                    }
                }

                _progress = _currentStep * samplesPerTick + dataIndex;

                if (sample + dataIndex >= _nextTick)
                {
                    //Refactored to increase readability.
                    AddToBackBuffer();
                    _nextTick += samplesPerTick;
                    if (++_currentStep > signatureLo)
                    {
                        _currentStep = 1;
                    }
                    _progress = _currentStep * samplesPerTick;
                    if (sequence[_currentStep - 1])
                    {
                        _index = 0;
                        if (OnBeat != null) _fireBeatEvent++;
                    }
                    else
                    {
                        _index = -1;
                    }
                    if (OnAnyStep != null) _fireAnyStepEvent++;
                    if (log) Debug.Log("Tick: " + _currentStep + " (%" + GetPercentage() + ")");
                }
            }
        }
    }

    /// <summary>
    /// Add remaining audio data to back buffer to be played in next audio thread cycles.
    /// </summary>
    private void AddToBackBuffer()
    {
        if (maxBackBufferSize > 0)
        {
            if (_index != -1 && _index < _clipData.Length)
            {
                if (_backBuffer == null) _backBuffer = new List<BackBuffer>(increaseBackBufferBy);
                else if (_backBuffer.Count != maxBackBufferSize)
                {
                    if (_backBuffer.Count == _backBuffer.Capacity) _backBuffer.Capacity += increaseBackBufferBy;
                    float[] newBackBuffer = new float[_clipData.Length - _index];
                    for (int i = _index; i < _clipData.Length; i++) newBackBuffer[i - _index] = _clipData[i];
                    _backBuffer.Add(new BackBuffer(newBackBuffer));
                    if (log)
                        print("New BackBuffer[" + newBackBuffer.Length + "] added. Total: " +
                              _backBuffer.Count + "/" + _backBuffer.Capacity);
                }
            }
        }
    }


#if UNITY_EDITOR

    private bool _isMutedOld;
    private int _oldBpm;

    /// <summary>
    /// Check and update when options are changed from editor.
    /// </summary>
    void LateUpdate()
    {
        if (IsReady)
        {
            if (_isMutedOld != isMuted)
            {
                _isMutedOld = isMuted;
                Mute(isMuted);
            }
            if (_oldBpm != bpm)
            {
                _oldBpm = bpm;
                SetBpm(bpm);
            }
        }
    }

    [MenuItem("GameObject/Sequencer/Sequencer", false, 10)]
    static void CreateSequencerController(MenuCommand menuCommand)
    {
        // Create a custom game object
        GameObject go = new GameObject("Sequencer");
        go.AddComponent<AudioSource>().playOnAwake = false;
        go.AddComponent<Sequencer>();
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
#endif

    #endregion

    #region Structs
    public class BackBuffer
    {
        public BackBuffer(float[] data)
        {
            this.data = data;
            index = 0;
        }

        /// <summary>
        /// Data to be backbuffered
        /// </summary>
        public float[] data;
        /// <summary>
        /// Current index of data.
        /// </summary>
        public int index;
    }

    #endregion

    #region Classes

    #endregion
}