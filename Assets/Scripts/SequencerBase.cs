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

using UnityEngine;
using System.Collections.Generic;
 
public class SequencerBase : MonoBehaviour {
   
    #region Enumerations
 
    #endregion
 
    #region Events and Delegates
 
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

    public virtual void Play()
    {
    }

    public virtual void Play(double newPercentage)
    {
    }

    public virtual void Stop()
    {
    }

    public virtual void SetBpm(int newBpm)
    {
    }

    public virtual void SetPercentage(double newPercentage)
    {
    }

    public virtual void Pause(bool isPaused)
    {
    }

    /// <summary>
    /// Mute/Unmute sequencer.
    /// </summary>
    /// <param name="isMuted"></param>
    public virtual void Mute(bool isMuted)
    {
        this.isMuted = isMuted;
    }

    /// <summary>
    /// Toggle mute status.
    /// </summary>
    public virtual void ToggleMute()
    {
        Mute(!isMuted);
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}