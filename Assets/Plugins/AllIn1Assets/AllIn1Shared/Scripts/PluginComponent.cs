using UnityEngine;

namespace AllIn1Assets
{
	public abstract class PluginComponent : MonoBehaviour
	{
		public abstract Material GetCurrentMaterial();

		public abstract void SetCurrentMaterial(Material mat);

		public abstract void AwakeImpl();

		public void OnEnableImpl()
		{
#if UNITY_EDITOR
			OnEnableEditor();
#else
			OnEnableRuntime();
#endif
		}

		public void OnDisableImpl()
		{
#if UNITY_EDITOR
			OnDisableEditor();
#else
			OnDisableRuntime();
#endif
		}

		private void Awake()
		{
			AwakeImpl();
		}

		private void OnEnable()
		{
			OnEnableImpl();
		}

		private void OnDisable()
		{
			OnDisableImpl();
		}


		protected abstract void OnEnableRuntime();
		protected abstract void OnDisableRuntime();




#if UNITY_EDITOR
		protected abstract void OnEnableEditor();
		protected abstract void OnDisableEditor();

		public abstract void ResetImpl();

		public virtual void NewCleanMaterial(Material defaultMaterial)
		{
			Material previousMat = GetCurrentMaterial();
			Material newMaterial = new Material(defaultMaterial);

			newMaterial.CopyMatchingPropertiesFromMaterial(previousMat);

			string materialName = newMaterial.name;
			int nameStartIndex = materialName.LastIndexOf("/");
			if (nameStartIndex >= 0)
			{
				materialName = materialName.Substring(nameStartIndex + 1);
			}

			newMaterial.name = $"MAT_{materialName}";

			SetCurrentMaterial(newMaterial);
			InitMaterial(newMaterial);
		}

		public virtual void CleanMaterial(Shader newShader)
		{
			Material currMaterial = GetCurrentMaterial();
			Material newMaterial = new Material(newShader);

			newMaterial.CopyMatchingPropertiesFromMaterial(currMaterial);

			SetCurrentMaterial(newMaterial);
			InitMaterial(newMaterial);
		}

		public abstract void InitMaterial(Material mat);

		public abstract bool CheckValidComponent();

		public abstract void RemoveChildComponents();

		public Material DuplicateCurrentMaterial()
		{
			Material currMaterial = GetCurrentMaterial();
			currMaterial = new Material(currMaterial);
			return currMaterial;
		}

		private void Reset()
		{
			ResetImpl();
		}
#endif

	}
}