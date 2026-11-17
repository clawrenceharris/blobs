using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AllIn1Assets
{
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject objectToSpawn;
        [SerializeField] private Transform parentTransform;
        [SerializeField] private bool spawnObject;
        [SerializeField] private bool destroyAllChildren;
        [SerializeField] private float outerRadius = 5f;
        [SerializeField] private float innerRadius = 1f;
        [SerializeField] private int spawnCount = 1;
        [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

        private bool previousSpawnState;
        private bool previousDestroyState;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(!parentTransform) return;

            // Ensure inner radius is always smaller than outer radius
            innerRadius = Mathf.Min(innerRadius, outerRadius - 0.1f);

            if(spawnObject && !previousSpawnState && objectToSpawn)
                SpawnObjects();

            if(destroyAllChildren && !previousDestroyState)
                DestroyAllChildren();

            previousSpawnState = spawnObject;
            previousDestroyState = destroyAllChildren;
        }

        private void SpawnObjects()
        {
            // Use List with capacity to avoid resizing
            List<GameObject> spawnedObjects = new List<GameObject>(spawnCount);

            for(int i = 0; i < spawnCount; i++)
            {
                // Generate random position within the ring (between inner and outer radius)
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float randomDistance = Mathf.Sqrt(Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
                Vector2 randomPoint = randomDirection * randomDistance;
                Vector3 spawnPosition = parentTransform.position + new Vector3(randomPoint.x, 0, randomPoint.y);

                // Generate random rotation around Y axis
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // Generate random scale
                float randomScale = Random.Range(scaleRange.x, scaleRange.y);

                // Try to spawn as prefab first
                GameObject spawnedObject = PrefabUtility.InstantiatePrefab(objectToSpawn, parentTransform) as GameObject;
                if(spawnedObject == null)
                {
                    spawnedObject = Instantiate(objectToSpawn, parentTransform);
                    spawnedObject.name = objectToSpawn.name;
                }

                // Apply position, rotation, and scale
                spawnedObject.transform.position = spawnPosition;
                spawnedObject.transform.rotation = randomRotation;
                spawnedObject.transform.localScale = Vector3.one * randomScale;

                // Register for undo
                Undo.RegisterCreatedObjectUndo(spawnedObject, "Spawn Object");
                spawnedObjects.Add(spawnedObject);
            }

            Debug.Log($"Spawned {spawnCount} instances of {objectToSpawn.name}");
            spawnObject = false;
        }

        private void DestroyAllChildren()
        {
            int childCount = parentTransform.childCount;

            // Destroy in reverse order to avoid index shifting issues
            for(int i = childCount - 1; i >= 0; i--)
            {
                Transform child = parentTransform.GetChild(i);
                DestroyImmediate(child.gameObject);
            }

            Debug.Log($"Destroyed {childCount} children under {parentTransform.name}");
            destroyAllChildren = false;
        }

        private void OnDrawGizmosSelected()
        {
            if(!parentTransform) return;

            // Draw outer radius
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(parentTransform.position, outerRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(parentTransform.position, outerRadius);

            // Draw inner radius (exclusion zone)
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawSphere(parentTransform.position, innerRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(parentTransform.position, innerRadius);
        }
#endif
    }
}
