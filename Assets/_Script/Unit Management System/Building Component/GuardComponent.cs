using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Script.Unit_Management_System.Building
{
    public class GuardComponent : MonoBehaviour
    {
        [Tooltip("Danh sách những điểm mà cung thủ có thể đứng")]
        public Transform[] positionSpots;

        [Tooltip("Lưu tên unit và vị trí đang đứng")]
        public List<SpotData> listArcherPositions = new();

        private global::Building building;
        private SpriteRenderer buildingRenderer;

        private void Awake()
        {
            building = GetComponent<global::Building>();
            buildingRenderer = building.GetSpriteRendererComponent();

            RegisterSpot();
        }

        public void RegisterSpot()
        {
            List<Transform> spots = new();

            foreach (Transform child in transform.GetComponentsInChildren<Transform>(true))
            {
                if (child.CompareTag("Spot"))
                    spots.Add(child);
            }

            positionSpots = spots.ToArray();
        }

        public void OnUnitAdded(global::Unit unit)
        {
            Vector3 spot = GetAvailableSpot();
            unit.transform.position = spot;
            
            if (unit.unitType != UnitType.Archer)
                return;
            if(positionSpots.Length > listArcherPositions.Count)
                listArcherPositions.Add(new SpotData
                {
                    position = spot,
                    unitId = unit.GetId()
                });
            AudioManager.Instance.PlaySFX(SoundNames.SfxSuccess);
        }

        public void OnUnitRemoved(global::Unit unit)
        {
            if (unit.unitType != UnitType.Archer)
                return;

            int index = listArcherPositions.FindIndex(s => s.unitId == unit.GetId());
            if (index >= 0)
                listArcherPositions.RemoveAt(index);
        }

        public Vector3 GetAvailableSpot()
        {
            foreach (var spot in positionSpots)
            {
                var spotData = listArcherPositions
                    .FirstOrDefault(s => s.position == spot.position);

                if (string.IsNullOrEmpty(spotData.unitId))
                    return spot.position;
            }

            return building.GetRandomPositionAroundBuilding();
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (positionSpots == null) return;

            Gizmos.color = Color.green;
            foreach (Transform spot in positionSpots)
            {
                if (spot != null)
                    Gizmos.DrawSphere(spot.position, 0.1f);
            }
        }
#endif
    }

    
}