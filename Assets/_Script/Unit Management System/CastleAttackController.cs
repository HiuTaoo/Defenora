using UnityEngine;

public class CastleAttackController : MonoBehaviour
{
    [Header("Vị trí đặt cung thủ")]
    public Transform[] archerSpots;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (archerSpots == null) return;

        Gizmos.color = Color.green;
        foreach (Transform spot in archerSpots)
        {
            if (spot != null)
                Gizmos.DrawSphere(spot.position, 0.1f);
        }
    }
#endif
}
