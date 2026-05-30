using System.Collections.Generic;
using System.Globalization;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.ArcherNode;
using _Script.BT.Node.ArcherNode.ArcherDetectedEnemy;
using _Script.BT.Node.ArcherNode.ArcherIdle;
using _Script.BT.Node.BuilderNode.Idle;
using _Script.BT.Node.LancerNode.LancerDetectedEnemy;
using _Script.BT.Node.Public_Node;
using _Script.BT.Node.WarriorNode.WarriorCombat.SearchLastSeenPosition;
using _Script.ItemScript;
using _Script.Object_Pooling;
using _Script.Unit_Management_System.Animation;
using UnityEditor;
using UnityEngine;

public class Archer : Unit
{
    [Header("Archer Combat Stat")] public float nextFireTime;

    [Header("Archer Specific")] public Transform firePoint;

    private readonly RaycastHit2D[] raycastResults = new RaycastHit2D[10];

    private bool _isStationed;

    public bool isStationed
    {
        get => _isStationed;
        set
        {
            if (_isStationed != value)
            {
                _isStationed = value;
                OnStationedChanged(_isStationed);
            }
        }
    }

    public ArcherBlackBoard archerBlackBoard { get; set; }

    protected override void Awake()
    {
        base.Awake();
        unitType = UnitType.Archer;
        attackRange = viewDistance;

        archerBlackBoard = new ArcherBlackBoard();
        bt = CreateBehaviourTree(this);
    }

    protected override void Update()
    {
        base.Update();
        UpdateSprite();
        UpdateSensors();
        UpdateFirePointPosition();
        CheckEnemyAggro();
        animFSM.ChangeState(currentState, animState);

        bt?.Tick();
    }

    #region BehaviourTree

    private BehaviourTree CreateBehaviourTree(Archer archer)
    {
        // =================================================================
        // KHỐI LOGIC CHUNG: PHÁT HIỆN & TẤN CÔNG ĐỊCH (Dùng cho cả 2 trạng thái)
        // =================================================================
        var attackActionSequence = new SequenceNode(
            new IsArcherCooldownReadyNode(archer),
            new ShootArrowNode(archer)
        );

        var detectedSequence = new SequenceNode(
            new HasAggroTargetNode(archer),
            new SelectTargetNode(archer),
            new AimAtTargetNode(archer),
            new IsArcherInDefendStateNode(archer),
            new SelectorNode(
                attackActionSequence,
                new ArcherAttackCooldownNode(archer)
            )
        );

        var idleSequence = new SequenceNode(
            new HasNoEnemyInRangeNode(archer),
            new RotateScanNode(archer),
            new WaitRandomTimeNode(archer)
        );


        // =================================================================
        // NHÁNH 1: TRẠNG THÁI TRÊN THÁP (STATIONED BRANCH)
        // Địch đến thì bắn, không có địch thì đứng xoay người quét/chờ
        // =================================================================
        var stationedCombatSelector = new SelectorNode(
            detectedSequence,
            idleSequence
        );

        var stationedBranchSequence = new SequenceNode(
            new IsArcherStationedNode(archer), // Bắt buộc phải là lính đứng tháp
            stationedCombatSelector
        );


        // =================================================================
        // NHÁNH 2: TRẠNG THÁI TỰ DO DI CHUYỂN (MOBILE/FREE BRANCH)
        // =================================================================

        // C-1. Ưu tiên 1 (Ban đêm): Trời tối -> Tìm đường đi về nhà gần nhất để trú ẩn/phòng thủ
        var mobileNightReturnSequence = new SequenceNode(
            new IsNightTimeConditionNode(archer),
            new MoveToNearestBuildingActionNode(archer)
        );

        // 🟢 C-2. Ưu tiên 2 (Ban ngày - Săn động vật): Nếu thấy động vật thì dừng lại nhắm bắn tại chỗ
        var huntAnimalsSequence = new SequenceNode(
            new SelectAnimalTargetNode(archer), // Quét tìm cừu/động vật
            new AimAtTargetNode(archer), // Ngắm bắn
            new SelectorNode(
                attackActionSequence, // Nếu CooldownReady -> Chạy ShootArrowNode (Trả về Running giữ anim)
                new ArcherAttackCooldownNode(archer) // Nếu đang hồi -> Giữ trạng thái Idle chờ bắn phát tiếp theo
            )
        );

        // C-3. Ưu tiên 3 (Ban ngày - Di chuyển làm việc/tuần tra nếu không có gì để săn)

        // Trường hợp A: Có assignedBuilding (nhưng ko đứng tháp) -> Tuần tra xung quanh tâm công trình đó
        var patrolAssignedBuildingSequence = new SequenceNode(
            new IsAssignedToBuildingConditionNode(archer),
            new PatrolAroundTowerActionNode(archer),
            new WaitRandomTimeNode(archer)
        );

        // Trường hợp B: Không có công trình nào -> Lấy vị trí hiện tại làm tâm và đi dạo tự do
        var wanderFreeSequence = new SequenceNode(
            new ArcherWanderActionNode(archer),
            new WaitRandomTimeNode(archer)
        );

        // Hợp nhất logic ban ngày của di chuyển: Có nhà đi tuần nhà, không nhà đi dạo tự do
        var mobileDaytimeMovementSelector = new SelectorNode(
            patrolAssignedBuildingSequence,
            wanderFreeSequence
        );

        // Hợp nhất logic HOÀ BIÊN ban ngày tổng thể: Ưu tiên có thú thì đi săn trước > Không có thú mới di chuyển dạo/tuần tra
        var mobileDaytimeSelector = new SelectorNode(
            huntAnimalsSequence, // ◄ Thú lọt vào tầm mắt là ưu tiên xả tên ngay
            mobileDaytimeMovementSelector // Không có thú thì mới đi bộ loanh quanh
        );

        // Gom toàn bộ trạng thái hòa bình của Archer tự do: Đêm về nhà phòng thủ > Ngày làm việc/Săn bắn
        var mobileIdleSelector = new SelectorNode(
            mobileNightReturnSequence,
            mobileDaytimeSelector
        );

        // Chuỗi hoàn chỉnh của Archer tự do: Ưu tiên tối cao luôn là thấy QUÁI ĐỊCH thật sự thì bắn, ko có địch mới xử lý idle/săn bắn
        var mobileBranchSequence = new SequenceNode(
            new SelectorNode(
                detectedSequence, // Địch thật (Monster/Enemy) vào tầm là ưu tiên số 1
                mobileIdleSelector // Nếu an toàn, mới xét đến Đi ngủ > Đi săn > Đi dạo tuần tra
            )
        );


        // =================================================================
        // ROOT TREE: QUYẾT ĐỊNH TRẠNG THÁI ĐỨNG IM HAY DI CHUYỂN
        // =================================================================
        var root = new SelectorNode(
            stationedBranchSequence, // Ưu tiên 1: Nếu lính đang đứng tháp -> Thực thi nhánh cố định
            mobileBranchSequence // Ưu tiên 2: Nếu không đứng tháp -> Tự động chuyển qua cơ động
        );

        return new BehaviourTree(root);
    }

    #endregion

    public override void UseSpecialAbility()
    {
        var target = archerBlackBoard.detectedEnemy;

        if (target == null)
            return;

        Vector2 start = firePoint.transform.position;
        Vector2 targetPos = target.transform.position;

        var arrow = PoolManager.Instance.Spawn(
            PrefabConfig.Instance.arrowPrefab,
            start,
            Quaternion.identity
        );

        var arrowComponent = arrow.GetComponent<ArrowProjectile>();

        var rb = target.GetComponent<Rigidbody2D>();
        var targetVelocity = rb != null ? rb.velocity : Vector2.zero;

        var predictedPos = PredictTargetPosition(
            firePoint.position,
            target.transform.position,
            targetVelocity,
            arrowComponent.speed
        );

        var dir = (predictedPos - (Vector2)firePoint.position).normalized;

        arrowComponent.Init(firePoint.position, dir, attackDamage);
    }

    #region Event

    private void OnStationedChanged(bool stationed)
    {
        UpdateSprite();

        if (bt != null) bt.ClearState();
    }

    #endregion

    #region Method

    private void UpdateSprite()
    {
        sortingYX.enabled = !isStationed;
        if (isStationed)
            UpdateSortingOrderByAssignBuilding();
    }

    private void UpdateSortingOrderByAssignBuilding()
    {
        if (!isStationed || assignedBuilding == null)
            return;
        var buildingSpriteRenderer = assignedBuilding.gameObject.GetComponent<SpriteRenderer>();
        if (buildingSpriteRenderer == null)
            return;

        spriteRenderer.sortingOrder = buildingSpriteRenderer.sortingOrder + 1;
    }

    private void UpdateIsStationed()
    {
        isStationed = assignedBuilding != null && assignedBuilding.buildingType == BuildingType.WatchTower;
    }

    public bool CheckEnemyStillInRange(GameObject target, float range)
    {
        var size = Physics2D.OverlapCircleNonAlloc(transform.position, range, results, enemyLayer);

        for (var i = 0; i < size; i++)
            if (results[i] != null &&
                results[i].gameObject == target)
                return true;

        return false;
    }

    public bool DetectEnemy(float range, Vector2 dir)
    {
        var size = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            results,
            enemyLayer);

        dir.Normalize();

        if (Camera.main == null) return false;
        Vector2 cameraPos = Camera.main.transform.position;

        for (var i = 0; i < size; i++)
        {
            var hit = results[i];
            if (hit == null || !hit.CompareTag("Enemy"))
                continue;

            Vector2 dirToEnemy = hit.transform.position - transform.position;
            dirToEnemy.Normalize();

            if (Vector2.Dot(dir, dirToEnemy) <= 0)
                continue;

            var b = hit.bounds;

            Vector2[] samplePoints =
            {
                b.center,
                new(b.center.x, b.max.y),
                new(b.center.x, b.min.y),
                new(b.min.x, b.center.y),
                new(b.max.x, b.center.y)
            };

            var visible = false;

            foreach (var point in samplePoints)
            {
                var dirRay = point - cameraPos;
                var dist = dirRay.magnitude;
                dirRay.Normalize();

                var ray = Physics2D.Raycast(
                    cameraPos,
                    dirRay,
                    dist,
                    obstacleLayer);

                if (ray.collider == null)
                {
                    visible = true;
                    break;
                }
            }

            if (!visible) continue;
            archerBlackBoard.detectedEnemy = hit.gameObject;
            return true;
        }

        return false;
    }

    public List<GameObject> DetectEnemies(float range, Vector2 dir)
    {
        var enemiesInRange = new List<GameObject>();

        var size = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            range,
            results,
            enemyLayer);

        dir.Normalize();
        Vector2 myPos = transform.position;

        for (var i = 0; i < size; i++)
        {
            var enemyHit = results[i];
            if (enemyHit == null || !enemyHit.CompareTag("Enemy"))
                continue;

            Vector2 dirToEnemy = (enemyHit.transform.position - (Vector3)myPos).normalized;
            if (Vector2.Dot(dir, dirToEnemy) <= 0)
                continue;

            var b = enemyHit.bounds;
            Vector2[] samplePoints =
            {
                b.center,
                new(b.center.x, b.max.y),
                new(b.center.x, b.min.y),
                new(b.min.x, b.center.y),
                new(b.max.x, b.center.y)
            };

            var visible = false;

            foreach (var point in samplePoints)
            {
                var dirRay = point - myPos;
                var dist = dirRay.magnitude;
                dirRay.Normalize();

                if (assignedBuilding != null)
                {
                    var hitCount = Physics2D.RaycastNonAlloc(myPos, dirRay, raycastResults, dist, obstacleLayer);
                    var isBlockedByOther = false;

                    for (var j = 0; j < hitCount; j++)
                    {
                        var hitObj = raycastResults[j].collider.gameObject;

                        var parentTransform = hitObj.transform.parent;
                        var parentObj = parentTransform != null ? parentTransform.gameObject : null;

                        if (parentObj != assignedBuilding.gameObject && hitObj != gameObject)
                        {
                            isBlockedByOther = true;
                            break;
                        }
                    }

                    if (!isBlockedByOther)
                    {
                        visible = true;
                        Debug.DrawLine(myPos, point, Color.green);
                        break;
                    }
                }
                else
                {
                    var ray = Physics2D.Raycast(myPos, dirRay, dist, obstacleLayer);

                    if (ray.collider == null)
                    {
                        visible = true;
                        break;
                    }
                }
            }

            if (visible) enemiesInRange.Add(enemyHit.gameObject);
        }

        return enemiesInRange;
    }

    public ArcherFireDirection GetFireDirection(Vector2 from, Vector2 to)
    {
        var dir = to - from;

        if (dir.sqrMagnitude < 0.0001f)
            return ArcherFireDirection.None;

        dir.Normalize();

        var angle = Vector2.Angle(Vector2.up, dir);

        return angle switch
        {
            <= 15f => ArcherFireDirection.Up,
            <= 75f => ArcherFireDirection.DiagonalUp,
            <= 105f => ArcherFireDirection.Front,
            <= 165f => ArcherFireDirection.DiagonalDown,
            _ => ArcherFireDirection.Down
        };
    }

    public static Vector2 PredictTargetPosition(
        Vector2 shooterPos,
        Vector2 targetPos,
        Vector2 targetVelocity,
        float projectileSpeed)
    {
        var toTarget = targetPos - shooterPos;

        var a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        var b = 2f * Vector2.Dot(toTarget, targetVelocity);
        var c = Vector2.Dot(toTarget, toTarget);

        var discriminant = b * b - 4f * a * c;

        if (discriminant < 0 || Mathf.Abs(a) < 0.001f)
            return targetPos;

        var sqrt = Mathf.Sqrt(discriminant);

        var t1 = (-b - sqrt) / (2f * a);
        var t2 = (-b + sqrt) / (2f * a);

        var t = Mathf.Min(t1, t2);

        if (t < 0)
            t = Mathf.Max(t1, t2);

        if (t < 0)
            return targetPos;

        return targetPos + targetVelocity * t;
    }

    private void CheckEnemyAggro()
    {
        if (archerBlackBoard.detectedEnemy != null &&
            CheckEnemyStillInRange(archerBlackBoard.detectedEnemy, viewDistance))
        {
            var distance = archerBlackBoard.detectedEnemy.transform.position - transform.position;

            archerBlackBoard.lastDirection = distance.x > 0 ? Vector2.right : Vector2.left;
            UpdateFacing(archerBlackBoard.lastDirection);

            currentTarget = archerBlackBoard.detectedEnemy.transform;
            lastSeenPosition = currentTarget.position;

            aggroTimer = archerBlackBoard.detectedEnemy.CompareTag("Enemy") ? aggroDuration : 0.5f;

            return;
        }

        if (currentTarget != null)
        {
            aggroTimer -= Time.deltaTime;

            if (aggroTimer <= 0)
            {
                ClearAggro();
                archerBlackBoard.detectedEnemy = null;
            }
        }
    }

    private void UpdateFirePointPosition()
    {
        if (firePoint == null) return;
        var target = archerBlackBoard.detectedEnemy;
        if (target == null) return;

        Vector2 archerPos = transform.position;
        Vector2 targetPos = target.transform.position;

        var dir = (targetPos - archerPos).normalized;

        var radius = 0.4f;

        var firePos = archerPos + dir * radius;

        firePoint.position = firePos;
    }

    public void ResetAnim()
    {
        archerBlackBoard.fireDirection = ArcherFireDirection.None;
        animState = AnimState.Idle;
    }

    private void UpdateSensors()
    {
        detectTimer += Time.deltaTime;
        if (detectTimer >= detectInterval)
        {
            detectTimer = 0f;

            if (archerBlackBoard.detectedEnemy != null)
                if (CheckEnemyStillInRange(archerBlackBoard.detectedEnemy, viewDistance))
                {
                    if (archerBlackBoard.detectedEnemy.CompareTag("Enemy"))
                        GlobalAlarmSystem.TriggerAlarm(archerBlackBoard.detectedEnemy,
                            archerBlackBoard.detectedEnemy.transform.position);
                    return;
                }

            var dir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            var enemies = DetectEnemies(viewDistance, dir);

            var newTarget = SelectClosestTarget(enemies);

            if (newTarget != null)
            {
                if (newTarget.CompareTag("Enemy"))
                    GlobalAlarmSystem.TriggerAlarm(newTarget, newTarget.transform.position);

                archerBlackBoard.detectedEnemy = newTarget;
            }
        }
    }

    public override List<(string name, string value)> GetSpecialStats()
    {
        var extraStats = new List<(string name, string value)>();

        extraStats.Add(("Attack Damage", attackDamage.ToString(CultureInfo.InvariantCulture)));
        extraStats.Add(("Attack Range", attackRange.ToString(CultureInfo.InvariantCulture)));
        extraStats.Add(("Fire Rate", attackCooldown.ToString(CultureInfo.InvariantCulture)));

        return extraStats;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (archerBlackBoard == null)
            return;

        if (archerBlackBoard.detectedEnemy != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, archerBlackBoard.detectedEnemy.transform.position);

            Gizmos.DrawSphere(archerBlackBoard.detectedEnemy.transform.position, 0.1f);
        }

        DrawVisionCone();
    }

    private void DrawVisionCone()
    {
        var origin = transform.position;
        var direction = transform.localScale.x > 0 ? Vector3.right : Vector3.left;

        Handles.color = new Color(1, 1, 0, 0.2f);
        Handles.DrawSolidArc(
            origin,
            Vector3.forward,
            Quaternion.Euler(0, 0, -viewAngle / 2) * direction,
            viewAngle,
            viewDistance
        );

        Gizmos.color = Color.yellow;

        var leftBoundary = Quaternion.Euler(0, 0, -viewAngle / 2) * direction;
        var rightBoundary = Quaternion.Euler(0, 0, viewAngle / 2) * direction;

        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);
    }
#endif
}