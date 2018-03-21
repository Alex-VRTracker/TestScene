using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using VRStandardAssets.Utils;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections.Generic;


// The intro scene takes users through the basics
// of interacting through VR in the other scenes.
// This manager controls the steps of the intro
// scene.
public class VRTrackerAssignmentManager : MonoBehaviour
{
	[SerializeField] private Reticle m_Reticle;                         // The scene only uses SelectionSliders so the reticle should be shown.
	[SerializeField] private SelectionRadial m_Radial;                  // Likewise, since only SelectionSliders are used, the radial should be hidden.
	[SerializeField] private UIFader m_HowToUseConfirmFader;            // Afterwards users are asked to confirm how to use sliders in this UI.
	[SerializeField] private SliderGroup m_SliderCroup;                 // They demonstrate this using this slider.
	[SerializeField] private UIFader m_AssociationFader;                // The final instructions are controlled using this fader.
	[SerializeField] private UIFader m_FailedCalibrationFader;
	[SerializeField] private SelectionSlider m_FailedCalibrationSlider;
	[SerializeField] private UIFader m_CalibrationCompleteFader;
	[SerializeField] private SelectionSlider m_CalibrationCompleteSlider;
	[SerializeField] private UIFader m_LoadingFader;

	private IEnumerator Start ()
	{
		m_Reticle.Show ();

		DontDestroyOnLoad (VRTracker.instance);
		bool assignationSuccess = false;

		//Disable by default the spectator mode on mobile device
		#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
		VRTracker.instance.isSpectator = false;
		#endif

		//Check if spectator mode is activated
		if (!VRTracker.instance.isSpectator)
		{
			m_SliderCroup.hideSpectatorMode();
			if (VRTracker.instance.autoAssignation)
			{
				VRTrackerTag[] tags = VRTracker.instance.gameObject.GetComponentsInChildren<VRTrackerTag> ();
				while(tags.Length != VRTracker.instance.tags.Count)
				{
					//Wait for everything to be loaded correctly
					yield return new WaitForSeconds(1);
				}
				if (VRTrackerTagAssignment.instance.LoadAssociation())
				{
					assignationSuccess = VRTrackerTagAssignment.instance.TryAutoAssignTag();
					if (!assignationSuccess)
					{
						m_SliderCroup.hideSkipAssignationSlider();
					}
				}
			}


			// Assign a Tag to each Prefab instance containing a Tag in VR Tracker
			if (!assignationSuccess)
			{
				// In order, fade in the UI on confirming the use of sliders, wait for the slider to be filled, then fade out the UI.
				yield return StartCoroutine(m_HowToUseConfirmFader.InteruptAndFadeIn());
				yield return StartCoroutine(m_SliderCroup.WaitForBarsToFill());
				yield return StartCoroutine(m_HowToUseConfirmFader.InteruptAndFadeOut());

				//Assignement step
				foreach (VRTrackerTag tag in VRTracker.instance.tags)
				{
					bool associationFailed = true;
					while (associationFailed)
					{
						// Edit shown title to the Prefab name
						m_AssociationFader.transform.Find("CalibrationInstructions/Title").GetComponentInChildren<Text>().text = "Assign " + tag.tagType.ToString();

						// Start assignation
						yield return StartCoroutine(ShowMenu(m_AssociationFader, tag));

						// Check if timed out and throw an error
						if (!tag.IDisAssigned)
						{
							associationFailed = true;
							yield return StartCoroutine(ShowMenu(m_FailedCalibrationFader, m_FailedCalibrationSlider));
						}
						else
						{
							associationFailed = false;
							tag.AssignTag(tag.UID);
						}
					}

				}
			}


			VRTracker.instance.SaveAssociationTagUser();
			enablePlayerCameraForNextLevel();

			// Load the next Level (the Game !)
			yield return StartCoroutine(m_LoadingFader.InteruptAndFadeIn());
			if (VRTracker.instance.serverIp == "" || VRTracker.instance.serverIp == Network.player.ipAddress)
			{
				VRTracker.instance.serverIp = Network.player.ipAddress;
				VRTracker.instance.SendServerIP(VRTracker.instance.serverIp);
				VRTrackerNetwork.instance.serverBindAddress = VRTracker.instance.serverIp;
				VRTrackerNetwork.instance.StartHost();

			}
			else
			{

				VRTrackerNetwork.instance.serverBindAddress = VRTracker.instance.serverIp;
				VRTrackerNetwork.instance.serverBindToIP = true;
				VRTrackerNetwork.instance.networkAddress = VRTracker.instance.serverIp;
				//after binding address, start server
				VRTrackerNetwork.instance.StartClient();

			}
		}
		else
		{
			VRTracker.instance.serverIp = Network.player.ipAddress;
			VRTracker.instance.SendServerIP(VRTracker.instance.serverIp);
			//Start in spectator mode
			VRTrackerNetwork.instance.StartServer();
		}

	}

	private IEnumerator ShowMenu(UIFader fader, SelectionSlider slider)
	{
		yield return StartCoroutine(fader.InteruptAndFadeIn());
		yield return StartCoroutine(slider.WaitForBarToFill());
		yield return StartCoroutine(fader.InteruptAndFadeOut());
	}

	private IEnumerator ShowMenu(UIFader fader, VRTrackerTag tag)
	{
		yield return StartCoroutine(fader.InteruptAndFadeIn());
		yield return StartCoroutine(tag.WaitForAssignation());
		if (tag.IDisAssigned)
			transform.GetComponent<AudioSource> ().Play();
		yield return StartCoroutine(fader.InteruptAndFadeOut());
	}

	/* In the Intro / Tag assignation scene, a camera is already there,
         * the one in our player character is currently disable but we want
         * to use it in the next scene. It's done using the "enableOnLoad" script
         */
	private void enablePlayerCameraForNextLevel(){
		// For each Tag
		foreach( VRTrackerTag tag in VRTracker.instance.tags){

			// Check the Tag has a camera in its children and an enable on load script
			if(tag.GetComponentInChildren<Camera>() && tag.GetComponent<EnableOnLoad>())
				tag.GetComponent<EnableOnLoad>().enableOnLoad = true;
		}
	}

}
