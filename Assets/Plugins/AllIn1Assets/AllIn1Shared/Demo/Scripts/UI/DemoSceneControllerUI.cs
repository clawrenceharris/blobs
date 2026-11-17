using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace AllIn1Assets
{
	public class DemoSceneControllerUI : MonoBehaviour
	{
		[Header("Pre-instantiated Instances")]
		[SerializeField] private DemoExpositorUI[] demoElementInstancesUI;

		[Header("References")]
		public TMP_Text txtDemoName;
		public TMP_Text txtInformationText;
		public AllIn1DemoScaleTween informationTween;

		[Header("Lerp Speed")] 
		public float lerpSpeed;

		private int currentDemoIndex;
		private DemoExpositorUI currentDemoElement;
		private DemoExpositorUI lastDemoElement;
		private bool isTransitioning;

		private EventSystem eventSystem;

		private void Start()
		{
			Init();
		}

		public void Init()
		{
			CheckEventSystem();

			for (int i = 0; i < demoElementInstancesUI.Length; i++)
			{
				demoElementInstancesUI[i].gameObject.SetActive(true);
				demoElementInstancesUI[i].Init(lerpSpeed);

				if (i > 0)
				{
					demoElementInstancesUI[i].gameObject.SetActive(false);
				}
			}

			currentDemoElement = demoElementInstancesUI[0];
			lastDemoElement = demoElementInstancesUI[demoElementInstancesUI.Length - 1];
		}

		private void CheckEventSystem()
		{
			EventSystem eventSystemInScene = GameObject.FindAnyObjectByType<EventSystem>();
			if (eventSystemInScene != null)
			{
				GameObject.Destroy(eventSystemInScene.gameObject);
			}

			GameObject goEventSystem = new GameObject("Event System");
			eventSystem = goEventSystem.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
			goEventSystem.AddComponent<InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
			goEventSystem.AddComponent<StandaloneInputModule>();
#endif
		}

		public void NextDemo()
		{
			if (!CanNavigate()) return;

			currentDemoIndex++;
			ClampDemoIndex();

			ShowCurrentDemo(DemoMovementDir.RIGHT);
		}

		public void PreviousDemo()
		{
			if (!CanNavigate()) return;

			currentDemoIndex--;
			ClampDemoIndex();

			ShowCurrentDemo(DemoMovementDir.LEFT);
		}

		private bool CanNavigate()
		{
			return !isTransitioning && demoElementInstancesUI != null && demoElementInstancesUI.Length > 1;
		}

		private void ShowCurrentDemo(DemoMovementDir dir)
		{
			if (demoElementInstancesUI == null || currentDemoIndex >= demoElementInstancesUI.Length)
			{
				Debug.LogError("Invalid demo instance index or instances not created!");
				return;
			}

			if (isTransitioning) return; // Extra safety check

			StartCoroutine(HandleDemoTransition(dir));
		}

		private void ClampDemoIndex()
		{
			if (demoElementInstancesUI == null) return;

			if (currentDemoIndex >= demoElementInstancesUI.Length)
			{
				currentDemoIndex = 0;
			}
			else if (currentDemoIndex < 0)
			{
				currentDemoIndex = demoElementInstancesUI.Length - 1;
			}
		}

		private System.Collections.IEnumerator HandleDemoTransition(DemoMovementDir dir)
		{
			isTransitioning = true;

			lastDemoElement = currentDemoElement;

			// Get the pre-instantiated element
			currentDemoElement = demoElementInstancesUI[currentDemoIndex];

			if (currentDemoElement == null)
			{
				Debug.LogError($"Demo instance at index {currentDemoIndex} is null!");
				isTransitioning = false;
				yield break;
			}

			// Update the demo name text
			UpdateDemoNameText();

			// Activate and initialize the current demo element
			currentDemoElement.gameObject.SetActive(true);
			//currentDemoElement.Init(lerpSpeed);

			if (lastDemoElement != null)
			{
				// Setup positions and start animations
				currentDemoElement.PlaceNextTo(lastDemoElement, dir);
				currentDemoElement.MoveToCenter();

				// Start exit animation for previous element
				if (dir == DemoMovementDir.LEFT)
				{
					lastDemoElement.MoveToLeft();
				}
				else
				{
					lastDemoElement.MoveToRight();
				}

				lastDemoElement.Hide();
			}

			currentDemoElement.Show();

			isTransitioning = false;
		}

		private void UpdateDemoNameText()
		{
			if (txtDemoName == null) return;

			if (currentDemoElement != null)
			{
				txtDemoName.text = demoElementInstancesUI[currentDemoIndex].expositorName;
				txtInformationText.text = demoElementInstancesUI[currentDemoIndex].expositorDescription;
				informationTween.ScaleUpTween();
			}
			else
			{
				txtDemoName.text = "Unknown Demo";
			}
		}
	}
}