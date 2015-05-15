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
    /// High signautre
    /// </summary>
    public int signatureHi = 4;
    /// <summary>
    /// Sequence of steps.
    /// True = Play
    /// False = Silent
    /// </summary>
    public bool[] sequence;
    /// <summary>
    /// Maximum back buffer allowed.
    /// </summary>
    public int maxBackBufferSize = 0;
    /// <summary>
    /// Enlarge backbuffer count by number specified.
    /// </summary>
    public int increaseBackBufferBy = 0;
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

    private void Awake()
    {
        StartCoroutine(Init());
    }

    /// <summary>
    /// Wait until sequencer is ready.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Init()
    {
        while (_clipData == null)
        {
            if (clip.loadState == AudioDataLoadState.Loaded)
            {
                _sampleRate = AudioSettings.outputSampleRate;
                _clipData = new float[clip.samples * clip.channels];
                clip.GetData(_clipData, 0);
            }
            yield return null;
        }
        if (playWhenReady)
        {
            Play();
        }
    }

    /// <summary>
    /// Start playing.
    /// </summary>
    public override void Play()
    {
        _nextTick = AudioSettings.dspTime * _sampleRate;
        GetComponent<AudioSource>().Play();
        _isPlaying = true;
    }

    /// <summary>
    /// Stop playing.
    /// </summary>
    public override void Stop()
    {
        GetComponent<AudioSource>().Stop();
        _isPlaying = false;
        _clipData = null;
        _index = 0;
        if (_backBuffer != null) _backBuffer.Clear();
    }

    /// <summary>
    /// Pause/Unpause palying. Not tested.
    /// </summary>
    /// <param name="pause"></param>
    public override void Pause(bool pause)
    {
        if (pause)
        {
            GetComponent<AudioSource>().Pause();
            _isPlaying = false;
            _index = 0;
            if (_backBuffer != null) _backBuffer.Clear();
        }
        else
        {
            GetComponent<AudioSource>().UnPause();
            _isPlaying = true;
        }
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
                    if (log) Debug.Log("Tick: " + _currentStep + "/" + signatureHi + " (%" + GetPercentage() + ")");
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