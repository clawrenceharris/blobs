using UnityEngine;

namespace AllIn1Assets
{
	public class DemoExpositorMovementUI
	{
		private RectTransform rectTransform;
		private bool isMoving;
		private bool disableOnFinish;

		private Vector3 srcPosition;
		private Vector3 dstPosition;


		private float timer;
		private float lerpSpeed;

		public DemoExpositorMovementUI(RectTransform rectTransform, float lerpSpeed)
		{
			this.rectTransform = rectTransform;
			this.lerpSpeed = lerpSpeed;
		}

		public void StartMoving(Vector3 srcPosition, Vector3 dstPosition, float lerpSpeed, bool disableOnFinish)
		{
			this.srcPosition = srcPosition;
			this.dstPosition = dstPosition;
			this.lerpSpeed = lerpSpeed;

			this.isMoving = true;
			this.disableOnFinish = disableOnFinish;

			timer = 0f;
		}

		public void Update(float deltaTime)
		{
			if (isMoving)
			{
				Move(deltaTime);
			}
		}

		private void Move(float deltaTime)
		{
			float t = timer / 0.25f;
			timer += Time.deltaTime;

			Vector3 newAnchoredPos3D = Vector3.Lerp(srcPosition, dstPosition, t);
			rectTransform.anchoredPosition3D = newAnchoredPos3D;

			if(t >= 1f)
			{
				MovementFinished();
			}
		}

		private void MovementFinished()
		{
			if (disableOnFinish)
			{
				rectTransform.gameObject.SetActive(false); 
			}
		}
	}
}