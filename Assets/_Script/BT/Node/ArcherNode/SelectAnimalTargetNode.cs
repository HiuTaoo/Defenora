using _Script.BT.Node;
using UnityEngine;

public class SelectAnimalTargetNode : BTActionNode
{
    private const string ANIMAL_TAG = "Animal";
    private readonly Archer archer;
    private readonly int obstacleLayerMask;

    public SelectAnimalTargetNode(Unit unit) : base(unit)
    {
        archer = (Archer)unit;

        obstacleLayerMask = LayerMask.GetMask("Building", "VisionBlocker");
    }

    public override BTStatus Tick()
    {
        if (archer.archerBlackBoard.detectedEnemy != null)
            return BTStatus.Success;

        if (archer.animalResult == null || archer.animalResult.Length == 0)
            archer.animalResult = new Collider2D[10];

        var size = Physics2D.OverlapCircleNonAlloc(
            archer.transform.position,
            archer.viewDistance,
            archer.animalResult,
            archer.enemyLayer
        );

        GameObject closestAnimal = null;
        var minDistance = Mathf.Infinity;

        for (var i = 0; i < size; i++)
        {
            var hit = archer.animalResult[i];

            if (hit != null && hit.CompareTag(ANIMAL_TAG))
            {
                var floorAgent = hit.GetComponentInChildren<FloorAgent>();
                if (floorAgent != null && floorAgent._currentFloorIndex == archer.layerIndex)
                {
                    if (IsTargetObstructed(hit.transform.position)) continue;

                    var distance = Vector2.Distance(archer.transform.position, hit.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestAnimal = hit.gameObject;
                    }
                }
            }
        }

        if (closestAnimal != null)
        {
            archer.archerBlackBoard.detectedEnemy = closestAnimal;

            if (archer.currentState != UnitState.Attack)
                archer.currentState = UnitState.Attack;

            return BTStatus.Success;
        }

        return BTStatus.Failure;
    }

    /// <summary>
    ///     🌟 Hàm bổ trợ bắn tia Raycast kiểm tra vật cản giữa Cung thủ và Mục tiêu
    /// </summary>
    /// <param name="targetPosition">Vị trí của con thú</param>
    /// <returns>True nếu bị chặn bởi Building/Decor, False nếu tầm nhìn trống trải</returns>
    private bool IsTargetObstructed(Vector3 targetPosition)
    {
        Vector2 startPos = archer.transform.position;
        var direction = (Vector2)targetPosition - startPos;
        var distance = direction.magnitude;

        var hitObstacle = Physics2D.Raycast(startPos, direction.normalized, distance, obstacleLayerMask);

        if (hitObstacle.collider != null) return true;
        return false;
    }

    public override void ClearState()
    {
        base.ClearState();
        if (archer != null && archer.animalResult != null)
            for (var i = 0; i < archer.animalResult.Length; i++)
                archer.animalResult[i] = null;
    }
}