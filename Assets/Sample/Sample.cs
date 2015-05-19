using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

internal class Sample : MonoBehaviour
{
    #region Enumerations

    #endregion

    #region Events and Delegates

    #endregion

    #region Variables

    public SequencerBase sequnncerBase;
    public Text playingText;
    public Text bpmText;
    public Text mutedText;
    #endregion

    #region Properties

    #endregion

    #region Methods

    private void Awake()
    {
        playingText.text = "Playing: " + sequnncerBase.IsPlaying;
        mutedText.text = "Muted: " + sequnncerBase.isMuted;
        bpmText.text = "Bpm: " + sequnncerBase.bpm;
    }

    public void OnPlay()
    {
        sequnncerBase.Play();
        playingText.text = "Playing: " + sequnncerBase.IsPlaying;
    }

    public void OnStop()
    {
        sequnncerBase.Stop();
        playingText.text = "Playing: " + sequnncerBase.IsPlaying;
    }

    public void OnPause()
    {
        sequnncerBase.Pause(true);
        playingText.text = "Playing: " + sequnncerBase.IsPlaying;
    }

    public void OnUnPause()
    {
        sequnncerBase.Pause(false);
        playingText.text = "Playing: " + sequnncerBase.IsPlaying;
    }

    public void OnMute()
    {
        sequnncerBase.Mute(true);
        mutedText.text = "Muted: " + sequnncerBase.isMuted;
    }

    public void OnUnMute()
    {
        sequnncerBase.Mute(false);
        mutedText.text = "Muted: " + sequnncerBase.isMuted;
    }

    public void ChangeBpm(int bpmDelta)
    {
        sequnncerBase.SetBpm(sequnncerBase.bpm + bpmDelta);
        bpmText.text = "Bpm: " + sequnncerBase.bpm;
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}