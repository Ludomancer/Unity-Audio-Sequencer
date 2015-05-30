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
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

internal class SequencerDriver : SequencerBase
{
    #region Enumerations

    #endregion

    #region Events and Delegates
    #endregion

    #region Variables

    /// <summary>
    /// Array of sequencers to be managed.
    /// </summary>
    public SequencerBase[] sequencers;
    #endregion

    #region Properties
    /// <summary>
    /// True if all connected sequencers has loaded their clips.
    /// </summary>
    public override bool IsReady
    {
        get
        {
            if (sequencers == null) return false;
            for (int i = 0; i < sequencers.Length; i++)
            {
                if (!sequencers[i].IsReady) return false;
            }
            return true;
        }
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
    /// Wait until all connected sequencers are ready.
    /// </summary>
    /// <returns></returns>
    private IEnumerator Init()
    {
        while (!IsReady) yield return null;
        if (playWhenReady)
        {
            Play();
        }
        OnReady();
    }

    /// <summary>
    /// Play all connected sequencers.
    /// </summary>
    public override void Play()
    {
        if (!IsPlaying)
        {
            for (int i = 0; i < sequencers.Length; i++)
            {
                sequencers[i].bpm = bpm;
                sequencers[i].Play();
            }
            IsPlaying = true;
        }
    }

    /// <summary>
    /// Play all connected sequencers from specified percentage.
    /// </summary>
    /// <param name="perc">Approximate percentage.</param>
    public override void Play(double perc)
    {
        SetPercentage(perc);
        Play();
    }

    /// <summary>
    /// Play all connected sequencers. 
    /// </summary>
    /// <param name="fadeDuration"></param>
    public override void Play(float fadeDuration)
    {
        if (!IsPlaying)
        {
            for (int i = 0; i < sequencers.Length; i++)
            {
                sequencers[i].bpm = bpm;
                sequencers[i].Play(fadeDuration);
            }
            IsPlaying = true;
        }
    }

    /// <summary>
    /// Stop all connected sequencers.
    /// </summary>
    public override void Stop()
    {
        if (IsPlaying)
        {
            for (int i = 0; i < sequencers.Length; i++)
            {
                sequencers[i].Stop();
            }
            IsPlaying = false;
        }
    }

    /// <summary>
    /// Stop all connected sequencers.
    /// </summary>
    /// <param name="fadeDuration"></param>
    public override void Stop(float fadeDuration)
    {
        if (IsPlaying)
        {
            for (int i = 0; i < sequencers.Length; i++)
            {
                sequencers[i].Stop(fadeDuration);
            }
            IsPlaying = false;
        }
    }

    /// <summary>
    /// Pause/Unpause all connected sequencers.
    /// </summary>
    /// <param name="isPaused"></param>
    public override void Pause(bool isPaused)
    {
        if ((IsPlaying && isPaused) || (!IsPlaying && !isPaused))
        {
            for (int i = 0; i < sequencers.Length; i++)
            {
                sequencers[i].Pause(isPaused);
            }
            IsPlaying = !IsPlaying;
        }
    }

    /// <summary>
    /// Pause/Unpause all connected sequencers.
    /// </summary>
    /// <param name="isPaused"></param>
    /// <param name="fadeDuration"></param>
    public override void Pause(bool isPaused, float fadeDuration)
    {
        if ((IsPlaying && isPaused) || (!IsPlaying && !isPaused))
        {
            for (int i = 0; i < sequencers.Length; i++)
            {
                sequencers[i].Pause(isPaused, fadeDuration);
            }
            IsPlaying = !IsPlaying;
        }
    }

    /// <summary>
    /// Mute/Unmute all connected sequencers.
    /// </summary>
    /// <param name="isMuted"></param>
    public override void Mute(bool isMuted)
    {
        for (int i = 0; i < sequencers.Length; i++)
        {
            sequencers[i].Mute(isMuted);
        }
        this.isMuted = isMuted;
#if UNITY_EDITOR
        _isMutedOld = this.isMuted;
#endif
    }

    /// <summary>
    /// Mute/Unmute all connected sequencers.
    /// </summary>
    /// <param name="isMuted"></param>
    /// <param name="fadeDuration"></param>
    public override void Mute(bool isMuted, float fadeDuration)
    {
        for (int i = 0; i < sequencers.Length; i++)
        {
            sequencers[i].Mute(isMuted, fadeDuration);
        }
        this.isMuted = isMuted;
#if UNITY_EDITOR
        _isMutedOld = this.isMuted;
#endif
    }

    /// <summary>
    /// Changes default fade in and fade out durations of all connected sequencers.
    /// </summary>
    /// <param name="fadeIn"></param>
    /// <param name="fadeOut"></param>
    public override void SetFadeDurations(float fadeIn, float fadeOut)
    {
        for (int i = 0; i < sequencers.Length; i++)
        {
            sequencers[i].SetFadeDurations(fadeIn,fadeOut);
        }
    }

    /// <summary>
    /// Toggle mute state.
    /// </summary>
    public override void ToggleMute()
    {
        isMuted = !isMuted;
        Mute(isMuted);
    }

    /// <summary>
    /// Set approximate percentage of all connected sequencers.
    /// Ignores leftover percentage from rounding. Not precise.
    /// </summary>
    /// <param name="percentage">Approximate percentage.</param>
    public override void SetPercentage(double percentage)
    {
        for (int i = 0; i < sequencers.Length; i++)
        {
            sequencers[i].SetPercentage(percentage);
        }
    }

    /// <summary>
    /// Set Bpm of all connected sequencers.
    /// </summary>
    /// <param name="newBpm">Beats per minute.</param>
    public override void SetBpm(int newBpm)
    {
        if (newBpm < 10) newBpm = 10;
        bpm = newBpm;
        for (int i = 0; i < sequencers.Length; i++)
        {
            sequencers[i].bpm = newBpm;
        }

#if UNITY_EDITOR
        _oldBpm = this.bpm;
#endif
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
                print("Driver changed " + _isMutedOld + ":" + isMuted);
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

    [MenuItem("GameObject/Sequencer/Sequencer Driver", false, 10)]
    static void CreateSequencerController(MenuCommand menuCommand)
    {
        // Create a custom game object
        GameObject go = new GameObject("Sequencer Driver");
        go.AddComponent<SequencerDriver>();
        GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
    }
#endif
    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}