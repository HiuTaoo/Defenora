using System;
using System.Collections.Generic;
using _Script.BT;
using _Script.BT.BlackBoard;
using _Script.BT.GlobalAlarm;
using _Script.BT.Node.BuilderNode.Idle;
using _Script.BT.Node.CivilianNode;
using UnityEngine;

// Chứa PanicFleeActionNode nếu bạn dùng chung
// Chứa các node tự do lang thang

namespace _Script.Unit_Management_System.UnitScript
{
    public class Civilian : global::Unit
    {
        public bool isPanicking { get; set; }

        public CivilianBlackBoard civilianBlackBoard { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            bt = CreateCivilianBT(this);
            civilianBlackBoard = new CivilianBlackBoard();
        }

        protected override void Update()
        {
            if (Mathf.Approximately(Time.timeScale, 0f))
                return;

            base.Update();

            if (!isPanicking)
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, viewDistance);
                foreach (var hit in hits)
                    if (hit != null && hit.CompareTag("Enemy"))
                    {
                        Debug.LogWarning(
                            $"[Civilian Sensor] 🚨 {gameObject.name} phát hiện quái vật {hit.name}! Kích hoạt báo động toàn cục!");

                        isPanicking = true;
                        characterMovement.RequestStopMoving();
                        bt?.ClearState();

                        var enemyLayer = hit.GetComponentInChildren<FloorAgent>()._currentFloorIndex;

                        GlobalAlarmSystem.TriggerAlarm(hit.gameObject, hit.transform.position, enemyLayer);
                        break;
                    }
            }

            animFSM.ChangeState(currentState, animState);
            bt?.Tick();
        }

        #region Behavior Tree

        public BehaviourTree CreateCivilianBT(Civilian civilian)
        {
            var wanderFreeSequence = new SequenceNode(
                new HasIdleTimeNode(civilian),
                new CivilianMoveAroundNode(civilian),
                new WaitRandomTimeNode(civilian)
            );

            var root = new SelectorNode(
                new CivilianPanicNode(civilian),
                wanderFreeSequence
            );

            return new BehaviourTree(root);
        }

        #endregion

        #region Unity Lifecycle Events (Nhận báo động ngược từ đồng đội)

        protected override void HandleGlobalAlarm(GameObject enemy, Vector3 spottedPosition, int layerIndex)
        {
            if (isPanicking) return;

            if (Vector2.Distance(transform.position, spottedPosition) > hearRange) return;

            Debug.LogWarning(
                $"[Civilian Global Alarm] 🚨 {gameObject.name} nghe thấy tiếng la hét báo động từ xa! Hoảng loạn!");

            isPanicking = true;

            if (characterMovement != null)
                characterMovement.RequestStopMoving();

            bt?.ClearState();
        }

        #endregion

        #region Methods

        public void ResetState()
        {
            currentState = UnitState.Idle;
            animState = AnimState.Idle;
            isPanicking = false;

            animFSM.ChangeState(UnitState.Idle, AnimState.Idle);
            bt?.ClearState();
        }

        public override void UseSpecialAbility()
        {
            throw new NotImplementedException();
        }

        public override List<(string name, string value)> GetSpecialStats()
        {
            return new List<(string name, string value)>();
        }

        #endregion
    }
    
}