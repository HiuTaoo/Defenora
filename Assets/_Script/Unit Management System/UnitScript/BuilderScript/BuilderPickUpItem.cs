using System;
using _Script.Task;
using UnityEngine;

namespace _Script.Unit_Management_System.Unit.BuilderScript
{
    public class BuilderPickUpItem : MonoBehaviour
    {
        [Header("Radar Settings")]
        public float pickupRadius = 0.5f;     
        public float scanInterval = 0.2f;     
        private LayerMask itemLayer;           

        private Builder builder;
        private UnitInventory currentInventory;
        private float scanTimer = 0f;

        private Collider2D[] results = new Collider2D[10];

        private void Awake()
        {
            builder = transform.parent.GetComponent<Builder>(); 
            itemLayer = LayerMask.GetMask("Decor");

            currentInventory = builder.currentInventory;
        }

        private void Update()
        {
            if (builder == null || currentInventory == null) return;
            if (currentInventory.IsFull) return;

            scanTimer += Time.deltaTime;
            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;
                PerformRadarScan();
            }
        }

        private void PerformRadarScan()
        {
            int hitCount = Physics2D.OverlapCircleNonAlloc(transform.position, pickupRadius, results, itemLayer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = results[i];
                if (hit == null || !hit.CompareTag("Item")) continue;

                if (hit.TryGetComponent<Item>(out Item item))
                {
                    if (item.IsAvailableFor(builder) && !item.isDropping)
                    {
                        builder.PickupItem(item);
                    
                        if (currentInventory.IsFull && builder.currentTask != null && builder.currentTask.taskType != TaskType.TransportItem)
                        {
                            TaskManager.Instance.RemoveTask(builder.currentTask);
                            builder.ResetState();
                        }

                        break; 
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}