using UnityEngine;
using UnityEngine.UI;

internal class Sample : MonoBehaviour
{
    #region Enumerations

    #endregion

    #region Variables

    public SequencerBase sequencerBase;
    public Text playingText;
    public Text bpmText;
    public Text mutedText;
    public Text seekText;
    public Slider seek;

    #endregion

    #region Methods

    private void Awake()
    {
        playingText.text = "Playing: " + sequencerBase.IsPlaying;
        mutedText.text = "Muted: " + sequencerBase.isMuted;
        bpmText.text = "Bpm: " + sequencerBase.bpm;
        seekText.text = "Seek: 0.00%";
    }

    public void OnPlay()
    {
        seek.interactable = false;
        sequencerBase.Play(seek.value);
        playingText.text = "Playing: " + sequencerBase.IsPlaying;
    }

    public void OnSeek(float perc)
    {
        sequencerBase.SetPercentage(perc);
        seekText.text = string.Format("Seek: {0:F2}%", (perc * 100));
    }

    public void OnStop()
    {
        seek.interactable = true;
        sequencerBase.Stop();
        playingText.text = "Playing: " + sequencerBase.IsPlaying;
    }

    public void OnPause()
    {
        sequencerBase.Pause(true);
        playingText.text = "Playing: " + sequencerBase.IsPlaying;
    }

    public void OnUnPause()
    {
        sequencerBase.Pause(false);
        playingText.text = "Playing: " + sequencerBase.IsPlaying;
    }

    public void OnMute()
    {
        sequencerBase.Mute(true);
        mutedText.text = "Muted: " + sequencerBase.isMuted;
    }

    public void OnUnMute()
    {
        sequencerBase.Mute(false);
        mutedText.text = "Muted: " + sequencerBase.isMuted;
    }

    public void ChangeBpm(int bpmDelta)
    {
        sequencerBase.SetBpm(sequencerBase.bpm + bpmDelta);
        bpmText.text = "Bpm: " + sequencerBase.bpm;
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}