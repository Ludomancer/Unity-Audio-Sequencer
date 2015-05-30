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
using UnityEngine;
using System.Collections.Generic;

public abstract class SequencerBase : MonoBehaviour
{

    #region Enumerations

    #endregion

    #region Events and Delegates
    /// <summary>
    /// Fired when Initialization is finished and module is ready to play.
    /// </summary>
    public Action OnReadyEvent;
    #endregion

    #region Variables
    /// <summary>
    /// Should the sequencer start playing automatically on Awake.
    /// </summary>
    public bool playWhenReady;
    /// <summary>
    /// Beats per minute. Use SetBpm to change Bpm programmatically.
    /// </summary>
    public int bpm;
    /// <summary>
    /// Print logs.
    /// </summary>
    public bool log;
    /// <summary>
    /// Is sequencer muted or not. Sequncer will continue counting steps. Use SetMuted or ToggleMute to change Bpm programmatically.
    /// </summary>
    public bool isMuted;
    /// <summary>
    /// Is playing. 
    /// </summary>
    protected bool _isPlaying;
    #endregion

    #region Properties
    /// <summary>
    /// Is playing.
    /// </summary>
    public bool IsPlaying
    {
        get { return _isPlaying; }
        protected set { _isPlaying = value; }
    }

    /// <summary>
    /// True if clip data is loaded.
    /// </summary>
    public virtual bool IsReady
    {
        get { return false; }
    }
    #endregion

    #region Methods

    private void Awake()
    {
        OnAwake();
    }

    /// <summary>
    /// Called when Initialization is finished and module is ready to play.
    /// </summary>
    protected virtual void OnReady()
    {
        if (OnReadyEvent != null) OnReadyEvent();
    }

    public abstract void OnAwake();

    public abstract void Play();

    public abstract void Play(double newPercentage);

    public abstract void Play(float fadeDuration);

    public abstract void Stop();

    public abstract void Stop(float fadeDuration);

    public abstract void SetBpm(int newBpm);

    public abstract void SetPercentage(double newPercentage);

    public abstract void Pause(bool isPaused);

    public abstract void Pause(bool isPaused, float fadeDuration);

    public abstract void Mute(bool isMuted);

    public abstract void Mute(bool isMuted, float fadeDuration);

    public abstract void SetFadeDurations(float fadeIn, float fadeOut);

    public abstract void ToggleMute();

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}