# Unity-Audio-Sequencer
A precise audio sequencer for Unity3d

## Basic feature list:

 * **Seamless** and **Stable** Audio Sequencer.
 * Stable even in high **metronomes**.
 * Ability to change **Bpm** or **Sequence** at **runtime**.
 * Works with **Unity3D 5+** since it uses [OnAudioFilterRead](http://docs.unity3d.com/ScriptReference/MonoBehaviour.OnAudioFilterRead.html) to access Audio Buffer.

## Acknowledgments

* This project uses an amazing C# [script template](http://www.sarpersoher.com/my-unity-new-c-script-template/) by [Sarper Soher](http://www.sarpersoher.com/).
* This project influenced by [Unity Metronome example](http://docs.unity3d.com/ScriptReference/AudioSettings-dspTime.html).
* Further contributions are welcome.

## Components
* **Sequencer:** Basic and main component that actually plays the audio files.
* **Sequener Group:** Manages child sequencers.
* **Sequencer Driver:** Manager any list of Sequencer Groups or Sequencers.
* **Sequencer Base:** Base class for all of the classes above. Should not be used by itself.


## Setup
### Sample
* Use **Sample Prefab** provided or open the **Sample Scene** to see a working example.
### Step by Step
* Import and set your audio files to **Decompress on Load**.
* Create a **Sequencer Group**. It will automtically create a **Sequencer** as child.
![Create Sequencer Group](http://i.imgur.com/oy6mcFn.png)
* Set audio file to **Sequencer**.
![Set Audio File](http://i.imgur.com/boegcsV.png)
* Set Bpm of **Sequencer Group**  
![Set Bpm](http://i.imgur.com/DRjels2.png)

### Alternative Usages
* **Sequencer Driver** can be used instead of **Sequencer Group** but you should manually set which **Sequencer** or **Sequencer Grops** it should manage.

  ![Sequencer Driver](http://i.imgur.com/vLhppb2.png)
* **Sequencer** can also be used without a **Sequncer Group** or **Sequencer Driver**
![Sequencer Only](http://i.imgur.com/uxSKPBf.png)
