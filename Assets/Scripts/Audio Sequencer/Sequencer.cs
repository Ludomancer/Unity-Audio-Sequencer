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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

[RequireComponent(typeof (AudioSource))]
internal class Sequencer : SequencerBase
{
    #region Delegates

    public delegate void OnStep(int currentBeat, int numberOfBeats);

    #endregion

    #region FadeTarget enum

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

    #endregion

    #region Fields

    /// <summary>
    /// Event to be fired on every step.
    /// </summary>
    public OnStep onAnyStep;

    /// <summary>
    /// Event to be fired on non-empty steps.
    /// </summary>
    public OnStep onBeat;

    #endregion

    #region Properties

    /// <summary>
    /// True if clip data is loaded.
    /// </summary>
    public override bool IsReady
    {
        get { return _clipData != null; }
    }

    /// <summary>
    /// Current step.
    /// </summary>
    public int CurrentStep
    {
        get { return _currentStep; }
    }

    /// <summary>
    /// Signature Lenght
    /// </summary>
    public int NumberOfSteps
    {
        get { return sequence.Length; }
    }

    #endregion

    #region Nested type: BackBuffer

    #region Structs

    public class BackBuffer
    {
        #region Fields

        /// <summary>
        /// Data to be backbuffered
        /// </summary>
        public float[] data;

        /// <summary>
        /// Current index of data.
        /// </summary>
        public int index;

        #endregion

        #region Other Members

        public BackBuffer()
        {
        }

        public BackBuffer(float[] data)
        {
            this.data = data;
            index = 0;
        }

        public void SetData(float[] data)
        {
            this.data = data;
            index = 0;
        }

        public void ClearData()
        {
            data = null;
            index = 0;
        }

        public static implicit operator bool(BackBuffer bb)
        {
            return !ReferenceEquals(bb, null);
        }

        #endregion
    }

    #endregion

    #endregion

    #region Variables

    /// <summary>
    /// Queues events to be fired make sure we are not missing any of them. Only created if the event is used.
    /// </summary>
    private Queue<Action> _onBeatEventQueue;

    /// <summary>
    /// Queues events to be fired make sure we are not missing any of them. Only created if the event is used.
    /// </summary>
    private Queue<Action> _onAnyStepEventQueue;

    /// <summary>
    /// Audio clip to be played by this sequencer.
    /// </summary>
    public AudioClip clip;

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
    private List<BackBuffer> _activeBackBuffers;

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

    /// <summary>
    /// Number of channels the audio clip has.
    /// </summary>
    private int _clipChannels;

    /// <summary>
    /// Re-use BackBuffer's instead of creating a new one everytime we need it.
    /// </summary>
    private List<BackBuffer> _backBufferPool;

    #endregion

    #region Methods

    public override void OnAwake()
    {
#if UNITY_EDITOR
        _isMutedOld = isMuted;
        _oldBpm = bpm;
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
                    _clipChannels = clip.channels;
                    _clipData = new float[clip.samples * _clipChannels];
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
            _clipChannels = clip.channels;
            _clipData = new float[clip.samples * _clipChannels];
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
            _clipChannels = clip.channels;
            _clipData = new float[clip.samples * _clipChannels];
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
        if (_activeBackBuffers != null)
        {
            _activeBackBuffers.Clear();
            _activeBackBuffers = null;
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
        if (_activeBackBuffers != null) _activeBackBuffers.Clear();

        double samplesTotal = _sampleRate * 60.0F / bpm * 4.0F;
        double samplesPerTick = samplesTotal / NumberOfSteps;
        double newSamplePos = samplesTotal * _newPercentage;
        double currentTickDouble = newSamplePos / samplesPerTick;
        _currentStep = (int)Math.Round(currentTickDouble, MidpointRounding.ToEven);
        if (log) print("Set Percentage: " + _currentStep + " (%" + _newPercentage + ")");
        _newPercentage = -1;
    }

    private void Update()
    {
        if (_onAnyStepEventQueue != null)
        {
            while (_onAnyStepEventQueue.Count > 0)
            {
                _onAnyStepEventQueue.Dequeue().Invoke();
            }
        }
        if (_onBeatEventQueue != null)
        {
            while (_onBeatEventQueue.Count > 0)
            {
                _onBeatEventQueue.Dequeue().Invoke();
            }
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

    void OnAudioFilterRead(float[] bufferData, int bufferChannels)
    {
        if (!IsReady || !_isPlaying) return;
        double samplesPerTick = _sampleRate * 60.0F / bpm * 4.0F / NumberOfSteps;
        double sample = AudioSettings.dspTime * _sampleRate;
        if (_newPercentage > -1)
        {
            UpdatePercentage();
            return;
        }

        if (isMuted)
        {
            int dataLeft = bufferData.Length;
            while (dataLeft > 0)
            {
                double newSample = sample + dataLeft;
                if (_nextTick < newSample)
                {
                    dataLeft = (int)(newSample - _nextTick);
                    _nextTick += samplesPerTick;
                    if (++_currentStep > NumberOfSteps) _currentStep = 1;
                    _progress = _currentStep * samplesPerTick;
                    if (sequence[_currentStep - 1])
                    {
                        _index = 0;
                        if (onBeat != null) _fireBeatEvent++;
                    }
                    else
                    {
                        _index = -1;
                    }
                    if (onAnyStep != null) _fireAnyStepEvent++;
                }
                else break;
            }
        }
        else
        {
            for (int dataIndex = 0; dataIndex < bufferData.Length / bufferChannels; dataIndex++)
            {
                if (_activeBackBuffers != null)
                {
                    for (int backBufferIndex = 0; backBufferIndex < _activeBackBuffers.Count; backBufferIndex++)
                    {
                        BackBuffer bb = _activeBackBuffers[backBufferIndex];

                        int clipChannel = 0;
                        int sourceChannel = 0;
                        while (sourceChannel < bufferChannels)
                        {
                            bufferData[dataIndex * bufferChannels + sourceChannel] +=
                                bb.data[bb.index * _clipChannels + clipChannel];

                            sourceChannel++;
                            clipChannel++;
                            if (clipChannel == _clipChannels - 1) clipChannel = 0;
                        }

                        bb.index++;
                        if (bb.index >= bb.data.Length / bufferChannels)
                        {
                            ReleaseBackBuffer(backBufferIndex);
                            backBufferIndex--;
                            if (log) print("BackBuffer recycled. Total: " + _activeBackBuffers.Count);
                        }
                    }
                }

                if (_index != -1)
                {
                    int clipChannel = 0;
                    int sourceChannel = 0;
                    while (sourceChannel < bufferChannels)
                    {
                        bufferData[dataIndex * bufferChannels + sourceChannel] +=
                            _clipData[_index * _clipChannels + clipChannel];

                        sourceChannel++;
                        clipChannel++;
                        if (clipChannel == _clipChannels - 1) clipChannel = 0;
                    }

                    _index++;
                    if (_index >= _clipData.Length / _clipChannels)
                    {
                        _index = -1;
                    }
                }

                _progress = _currentStep * samplesPerTick + dataIndex;

                if (sample + dataIndex >= _nextTick)
                {
                    //Refactored to increase readability.
                    AddToBackBuffer(bufferChannels);

                    _nextTick += samplesPerTick;
                    if (++_currentStep > NumberOfSteps)
                    {
                        _currentStep = 1;
                    }
                    _progress = _currentStep * samplesPerTick;
                    if (sequence[_currentStep - 1])
                    {
                        _index = 0;
                        if (onBeat != null)
                        {
                            if (_onBeatEventQueue == null) _onBeatEventQueue = new Queue<Action>();
                            _onBeatEventQueue.Enqueue(() => onBeat(_currentStep, NumberOfSteps));
                        }
                    }
                    else
                    {
                        _index = -1;
                    }
                    if (onAnyStep != null)
                    {
                        if (_onAnyStepEventQueue == null) _onAnyStepEventQueue = new Queue<Action>();
                        _onAnyStepEventQueue.Enqueue(() => onAnyStep(_currentStep, NumberOfSteps));
                    }
                    if (log) Debug.Log("Tick: " + _currentStep + " (%" + GetPercentage() + ")");
                }
            }
        }
    }

    /// <summary>
    /// Add remaining audio data to back buffer to be played in next audio thread cycles.
    /// </summary>
    private void AddToBackBuffer(int channels)
    {
        if (maxBackBufferSize > 0)
        {
            if (_index != -1 && _index < _clipData.Length)
            {
                float[] newBackBuffer = new float[_clipData.Length - _index];
                for (int i = _index; i < _clipData.Length / _clipChannels; i++)
                {
                    int clipChannel = 0;
                    while (clipChannel < _clipChannels)
                    {
                        newBackBuffer[(i - _index) * _clipChannels + clipChannel] =
                            _clipData[i * _clipChannels + clipChannel];
                        clipChannel++;
                    }
                }
                BackBuffer bb = BackBufferFactory();
                if (bb)
                {
                    bb.SetData(newBackBuffer);
                    if (log)
                        print("New BackBuffer[" + newBackBuffer.Length + "] added. Total: " +
                            _activeBackBuffers.Count + "/" + _activeBackBuffers.Capacity);
                }
            }
        }
    }

    BackBuffer BackBufferFactory()
    {
        BackBuffer bb = null;

        if (_activeBackBuffers == null && maxBackBufferSize > 0 && increaseBackBufferBy > 0)
            _activeBackBuffers = new List<BackBuffer>(increaseBackBufferBy);
        if (_activeBackBuffers != null && _activeBackBuffers.Count < maxBackBufferSize)
        {
            if (_activeBackBuffers.Count == _activeBackBuffers.Capacity)
            {
                int newCap = _activeBackBuffers.Capacity + increaseBackBufferBy;
                if (newCap > maxBackBufferSize) newCap = maxBackBufferSize;
                _activeBackBuffers.Capacity = newCap;
            }

            if (_backBufferPool != null && _backBufferPool.Count > 0)
            {
                bb = _backBufferPool[0];
                _backBufferPool.RemoveAt(0);
            }
            else
            {
                bb = new BackBuffer();
            }

            _activeBackBuffers.Add(bb);
        }

        return bb;
    }

    void ReleaseBackBuffer(int bbIndex)
    {
        BackBuffer bb = _activeBackBuffers[bbIndex];
        _activeBackBuffers.RemoveAt(bbIndex);
        bb.ClearData();

        if (_backBufferPool == null) _backBufferPool = new List<BackBuffer>();
        _backBufferPool.Add(bb);
    }

    void ReleaseBackBuffer(BackBuffer bb)
    {
        _activeBackBuffers.Remove(bb);
        bb.ClearData();

        if (_backBufferPool == null) _backBufferPool = new List<BackBuffer>();
        _backBufferPool.Add(bb);
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

    #region Classes

    #endregion
}