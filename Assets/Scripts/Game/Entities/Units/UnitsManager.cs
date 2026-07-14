using System;
using System.Collections.Generic;
using Game.Entities.Spawners;

namespace Game.Entities.Units
{
    public class UnitsManager : IDisposable
    {
        private readonly SpawnersManager _spawnersManager;
        private readonly Dictionary<TeammateControl, EnemyControl> _targetAssignments = new();
        private readonly Dictionary<EnemyControl, TeammateControl> _enemyTargetAssignments = new();
        private readonly HashSet<TeammateControl> _chargingTeammates = new();
        private readonly List<EnemyControl> _subscribedEnemies = new();
        private readonly List<TeammateControl> _subscribedTeammates = new();
        private readonly List<TeammateControl> _teammatesToResume = new();
        private readonly List<TeammateControl> _reassignmentBuffer = new();
        private readonly List<EnemyControl> _enemyReassignmentBuffer = new();

        public event Action OnEnemiesDefeated;

        public UnitsManager(SpawnersManager spawnersManager)
        {
            _spawnersManager = spawnersManager;
        }

        public void Init()
        {
            foreach (EnemyControl enemy in _spawnersManager.EnemyControls)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                enemy.OnDied += HandleEnemyDied;
                _subscribedEnemies.Add(enemy);
            }
        }

        public void ChargeUnits()
        {
            if (_chargingTeammates.Count > 0)
            {
                return;
            }

            ClearTargetAssignments();

            foreach (TeammateControl teammate in _spawnersManager.TeammateControls)
            {
                if (!teammate.IsAlive || !teammate.HasReachedSlot)
                {
                    continue;
                }

                _chargingTeammates.Add(teammate);
                teammate.OnDied += HandleTeammateDied;
                _subscribedTeammates.Add(teammate);
                AssignTarget(teammate);
            }

            foreach (EnemyControl enemy in _spawnersManager.EnemyControls)
            {
                if (enemy.IsAlive)
                {
                    AssignTarget(enemy);
                }
            }
        }

        public void Dispose()
        {
            foreach (EnemyControl enemy in _subscribedEnemies)
            {
                enemy.OnDied -= HandleEnemyDied;
            }

            _subscribedEnemies.Clear();
            ClearTargetAssignments();
        }

        public void PlayEnemiesVictory()
        {
            _teammatesToResume.Clear();
            foreach (TeammateControl teammate in _chargingTeammates)
            {
                if (teammate.IsAlive)
                {
                    _teammatesToResume.Add(teammate);
                }
            }

            ClearTargetAssignments();

            foreach (EnemyControl enemy in _spawnersManager.EnemyControls)
            {
                if (enemy.IsAlive)
                {
                    enemy.PlayVictory();
                }
            }

            foreach (TeammateControl teammate in _spawnersManager.TeammateControls)
            {
                if (teammate.IsAlive)
                {
                    teammate.StopCombat();
                }
            }
        }

        public void PlayTeammatesVictory()
        {
            foreach (TeammateControl teammate in _spawnersManager.TeammateControls)
            {
                if (teammate.IsAlive)
                {
                    teammate.PlayVictory();
                }
            }
        }

        public void ResumeAfterCharacterRespawn()
        {
            foreach (EnemyControl enemy in _spawnersManager.EnemyControls)
            {
                if (enemy.IsAlive)
                {
                    enemy.ResumeCombat();
                }
            }

            foreach (TeammateControl teammate in _spawnersManager.TeammateControls)
            {
                if (!teammate.IsAlive)
                {
                    continue;
                }

                if (!_teammatesToResume.Contains(teammate))
                {
                    teammate.StopCharge();
                    continue;
                }

                _chargingTeammates.Add(teammate);
                teammate.OnDied += HandleTeammateDied;
                _subscribedTeammates.Add(teammate);
                AssignTarget(teammate);
            }

            foreach (EnemyControl enemy in _spawnersManager.EnemyControls)
            {
                if (enemy.IsAlive)
                {
                    AssignTarget(enemy);
                }
            }

            _teammatesToResume.Clear();
        }

        private void AssignTarget(TeammateControl teammate)
        {
            EnemyControl target = FindBestTarget(teammate);
            if (target == null)
            {
                teammate.StopCharge();
                return;
            }

            _targetAssignments[teammate] = target;
            teammate.Charge(target);
        }

        private void AssignTarget(EnemyControl enemy)
        {
            TeammateControl target = FindBestTarget(enemy);
            if (target == null)
            {
                enemy.ResetTarget();
                return;
            }

            _enemyTargetAssignments[enemy] = target;
            enemy.SetTarget(target);
        }

        private EnemyControl FindBestTarget(TeammateControl teammate)
        {
            EnemyControl bestTarget = null;
            int lowestAttackersCount = int.MaxValue;
            float closestDistance = float.PositiveInfinity;

            foreach (EnemyControl enemy in _spawnersManager.EnemyControls)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                int attackersCount = GetAttackersCount(enemy);
                float distance = (enemy.transform.position - teammate.transform.position).sqrMagnitude;

                if (attackersCount > lowestAttackersCount ||
                    attackersCount == lowestAttackersCount && distance >= closestDistance)
                {
                    continue;
                }

                bestTarget = enemy;
                lowestAttackersCount = attackersCount;
                closestDistance = distance;
            }

            return bestTarget;
        }

        private TeammateControl FindBestTarget(EnemyControl enemy)
        {
            TeammateControl bestTarget = null;
            int lowestAttackersCount = int.MaxValue;
            float closestDistance = float.PositiveInfinity;

            foreach (TeammateControl teammate in _chargingTeammates)
            {
                if (!teammate.IsAlive)
                {
                    continue;
                }

                int attackersCount = GetAttackersCount(teammate);
                float distance = (teammate.transform.position - enemy.transform.position).sqrMagnitude;

                if (attackersCount > lowestAttackersCount ||
                    attackersCount == lowestAttackersCount && distance >= closestDistance)
                {
                    continue;
                }

                bestTarget = teammate;
                lowestAttackersCount = attackersCount;
                closestDistance = distance;
            }

            return bestTarget;
        }

        private int GetAttackersCount(EnemyControl enemy)
        {
            int attackersCount = 0;
            foreach (EnemyControl assignedEnemy in _targetAssignments.Values)
            {
                if (assignedEnemy == enemy)
                {
                    attackersCount++;
                }
            }

            return attackersCount;
        }

        private int GetAttackersCount(TeammateControl teammate)
        {
            int attackersCount = 0;
            foreach (TeammateControl assignedTeammate in _enemyTargetAssignments.Values)
            {
                if (assignedTeammate == teammate)
                {
                    attackersCount++;
                }
            }

            return attackersCount;
        }

        private void HandleEnemyDied(EnemyControl enemy)
        {
            enemy.OnDied -= HandleEnemyDied;
            _subscribedEnemies.Remove(enemy);
            _enemyTargetAssignments.Remove(enemy);

            if (_spawnersManager.EnemyControls.Count == 0)
            {
                foreach (TeammateControl teammate in _spawnersManager.TeammateControls)
                {
                    if (teammate.IsAlive)
                    {
                        teammate.StopCombat();
                    }
                }

                ClearTargetAssignments();
                OnEnemiesDefeated?.Invoke();
                return;
            }

            ReassignChargingTeammates();
        }

        private void ReassignChargingTeammates()
        {
            _targetAssignments.Clear();
            _reassignmentBuffer.Clear();

            foreach (TeammateControl teammate in _chargingTeammates)
            {
                if (teammate.IsAlive)
                {
                    _reassignmentBuffer.Add(teammate);
                }
            }

            foreach (TeammateControl teammate in _reassignmentBuffer)
            {
                AssignTarget(teammate);
            }
        }

        private void HandleTeammateDied(TeammateControl teammate)
        {
            teammate.OnDied -= HandleTeammateDied;
            _subscribedTeammates.Remove(teammate);
            _chargingTeammates.Remove(teammate);
            _targetAssignments.Remove(teammate);
            _enemyReassignmentBuffer.Clear();

            foreach (KeyValuePair<EnemyControl, TeammateControl> assignment in _enemyTargetAssignments)
            {
                if (assignment.Value == teammate)
                {
                    _enemyReassignmentBuffer.Add(assignment.Key);
                }
            }

            foreach (EnemyControl enemy in _enemyReassignmentBuffer)
            {
                _enemyTargetAssignments.Remove(enemy);
                AssignTarget(enemy);
            }
        }

        private void ClearTargetAssignments()
        {
            foreach (EnemyControl enemy in _subscribedEnemies)
            {
                if (enemy.IsAlive)
                {
                    enemy.ResetTarget();
                }
            }

            foreach (TeammateControl teammate in _subscribedTeammates)
            {
                teammate.OnDied -= HandleTeammateDied;
            }

            _subscribedTeammates.Clear();
            _chargingTeammates.Clear();
            _targetAssignments.Clear();
            _enemyTargetAssignments.Clear();
        }
    }
}
