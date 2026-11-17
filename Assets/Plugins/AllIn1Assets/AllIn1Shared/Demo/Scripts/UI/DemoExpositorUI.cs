using UnityEngine;

namespace AllIn1Assets
{
    public class DemoExpositorUI : MonoBehaviour
    {
		private const string TAG = "DemoExpositorUI";

		private DemoElementMovementState movementState;

        private DemoExpositorMovementUI demoExpositorMovement;
        private float lerpSpeed;
		 
        public PropertyTweenCollection tweenCollection;

        public DemoElement[] demoElements;
		public RectTransform rectTransform;

		public bool moveAwayOnHide = true;

#if UNITY_EDITOR
        public bool drawGizmos;
#endif

        private bool initialized;

        [Header("Config")]
        public string expositorName;
        [TextArea] public string expositorDescription;

        public virtual void Init(float lerpSpeed)
        {
            this.lerpSpeed = lerpSpeed;

            this.demoExpositorMovement = new DemoExpositorMovementUI(rectTransform, lerpSpeed);

            tweenCollection.Init();

            if(demoElements != null)
            {
                for(int i = 0; i < demoElements.Length; i++)
                {
                    demoElements[i].Init();
                }
            }

            initialized = true;
        }

        public virtual void Show()
        {
			
		}

        public virtual void Hide()
        {

		}

        protected virtual void Update()
        {
            if(!initialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;

            demoExpositorMovement.Update(deltaTime);

            //tweenCollection.Update(deltaTime);
        }

        public void MoveToLeft()
        {
			if (moveAwayOnHide)
			{
				this.demoExpositorMovement.StartMoving(rectTransform.anchoredPosition3D, GetLeftOutPosition(), lerpSpeed, true);
			}
		}


		public void MoveToRight()
        {
			if (moveAwayOnHide)
			{
				this.demoExpositorMovement.StartMoving(rectTransform.anchoredPosition3D, GetRightOutPosition(), lerpSpeed, true);
			}
        }

        public void MoveToCenter()
        {
            this.demoExpositorMovement.StartMoving(rectTransform.anchoredPosition3D, Vector3.zero, lerpSpeed, false);
        }

		public Vector3 GetRightBoundPosition()
		{
			Vector3 res = rectTransform.anchoredPosition3D + Vector3.right * rectTransform.sizeDelta.x * 0.5f;
			return res;
		}

		public Vector3 GetLeftBoundPosition()
		{
			Vector3 res = rectTransform.anchoredPosition3D - Vector3.right * rectTransform.sizeDelta.x * 0.5f;
			
			return res;
		}

		public Vector3 GetRightOutPosition()
        {
            Vector3 res = new Vector3(900f, 0f, 0f);
			return res;
        }

        public Vector3 GetLeftOutPosition()
        {
            Vector3 res = new Vector3(-900f, 0f, 0f);
            return res;
        }

        public void PlaceNextTo(DemoExpositorUI previousDemoElement, DemoMovementDir dir)
        {
			if(dir == DemoMovementDir.RIGHT)
            {
                Vector3 previousLeftPos = previousDemoElement.GetLeftOutPosition();
				rectTransform.anchoredPosition3D = previousLeftPos;
            }
            else if(dir == DemoMovementDir.LEFT)
            {
                Vector3 previousRightPos = previousDemoElement.GetRightOutPosition();
				rectTransform.anchoredPosition3D = previousRightPos;
            }
        }
        
        [ContextMenu("Set Expositor Name to GameObject Name")]
        private void SetExpositorNameToGameObjectName()
        {
            expositorName = gameObject.name;
        }
    }
}