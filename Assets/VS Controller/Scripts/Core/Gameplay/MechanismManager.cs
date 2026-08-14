/////////////////////////////////////////////////////////////////////////////////
//
//	MechanismManager.cs
//
//	Description:	set up mechanisms in ourselves and store their logic.           
//					
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using UnityEngine;

namespace VSController
{
    public class MechanismManager : MonoBehaviour
    {
        [System.Serializable]
        public class Mechanism
        {
            public string name;
            public List<FloorButton> floorButtons;
            public List<ManualButton> manualButtons;
            public List<MovableObject> doors;
            public List<string> allowedTags;
            public AudioClip buttonPressSound;
            public AudioClip doorOpenSound;

            public MechanismColor mechanismColor = MechanismColor.White;

            [HideInInspector] public bool isActive = false;
            [HideInInspector] public AudioSource buttonAudioSource;
            [HideInInspector] public AudioSource doorAudioSource;

            public enum MechanismColor
            {
                White, Red, Green, Blue, Yellow, Cyan, Magenta, Orange
            }
        }

        public List<Mechanism> mechanisms;

        private Dictionary<Mechanism, int> buttonPressCounts = new Dictionary<Mechanism, int>();
        private readonly Dictionary<MovableObject, int> doorActivationCounts = new();

        private void Start()
        {
            foreach (var mech in mechanisms)
            {

                buttonPressCounts[mech] = 0;

                mech.buttonAudioSource = CreateAudioSource($"ButtonAudio_{mech.name}");
                mech.doorAudioSource = CreateAudioSource($"MoveObjectAudio_{mech.name}");

                foreach (var btn in mech.floorButtons)
                {
                    if (btn != null)
                        btn.AddMechanism(this, mech);  // Assigns this mechanism to the floor button
                }

                foreach (var manualBtn in mech.manualButtons)
                {
                    if (manualBtn != null)
                        manualBtn.AddMechanism(this, mech);  // Assigns this mechanism to the manual button
                }
            }
        }

        // Creating an AudioSource for the needs of mechanisms
        private AudioSource CreateAudioSource(string name)
        {
            GameObject audioObject = new GameObject(name);
            audioObject.transform.parent = transform;
            return audioObject.AddComponent<AudioSource>();
        }

        // Called when pressing the button
        public void ButtonPressed(Mechanism mech)
        {
            // Increment the press count but clamp it to the total number of buttons
            buttonPressCounts[mech] = Mathf.Clamp(buttonPressCounts[mech] + 1, 0, mech.floorButtons.Count + mech.manualButtons.Count);

            // Activate the mechanism when all buttons have been pressed
            if (buttonPressCounts[mech] == mech.floorButtons.Count + mech.manualButtons.Count)
            {
                ActivateMechanism(mech);
            }

            // Play button press sound 
            if (mech.buttonPressSound != null)
            {
                PlaySound(mech.buttonAudioSource, mech.buttonPressSound);
            }
        }

        // Called when releasing the button
        public void ButtonReleased(Mechanism mech)
        {
            // Take away this button
            buttonPressCounts[mech] = Mathf.Clamp(buttonPressCounts[mech] - 1, 0, mech.floorButtons.Count + mech.manualButtons.Count);

            // Deactivate the mechanism if not all buttons are pressed anymore
            if (buttonPressCounts[mech] < mech.floorButtons.Count + mech.manualButtons.Count)
            {
                DeactivateMechanism(mech);
            }
        }

        private void ActivateMechanism(Mechanism mech)
        {
            if (!mech.isActive)
            {
                mech.isActive = true;
                Debug.Log($"Mechanism {mech.name} activated!");

                // Calling activation in MovebleObject.cd
                foreach (var door in mech.doors)
                {
                    if (!doorActivationCounts.ContainsKey(door))
                        doorActivationCounts[door] = 0;

                    doorActivationCounts[door]++;

                    if (doorActivationCounts[door] == 1)
                        door.Open();
                }

                // And play the sound 
                if (mech.doorOpenSound != null)
                {
                    PlaySound(mech.doorAudioSource, mech.doorOpenSound);
                }
            }
        }

        private void DeactivateMechanism(Mechanism mech)
        {
            if (mech.isActive)
            {
                mech.isActive = false;
                Debug.Log($"Mechanism {mech.name} deactivated!");

                // Calling deactivation in MovebleObject.cd
                foreach (var door in mech.doors)
                {
                    if (!doorActivationCounts.ContainsKey(door))
                        continue;

                    doorActivationCounts[door]--;

                    if (doorActivationCounts[door] <= 0)
                    {
                        doorActivationCounts.Remove(door);
                        door.Close();
                    }
                }
            }
        }

        // All sounds are played by this method
        private void PlaySound(AudioSource source, AudioClip clip)
        {
            if (source != null && clip != null)
            {
                source.clip = clip;
                source.Play();
            }
        }
    }
}

