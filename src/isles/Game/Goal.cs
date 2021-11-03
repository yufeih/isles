// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Isles.Engine;
using Microsoft.Xna.Framework;

namespace Isles
{
    public abstract class Goal : BaseState
    {
        public float ArbitrateInterval = 2;
        private double arbitrateTimer;

        public abstract void Arbitrate();

        public override void Activate() { }

        public override void Terminate() { }

        public override StateResult Update(GameTime gameTime)
        {
            arbitrateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (arbitrateTimer <= 0)
            {
                Arbitrate();
                arbitrateTimer = ArbitrateInterval;
            }

            return StateResult.Active;
        }
    }

    public class GoalDevelop : Goal
    {
        private readonly GameWorld world;
        private readonly ComputerPlayer owner;
        private float FarmFactor = 1.0f;
        private float HeroFactor = 1.0f;
        private float TownhallFactor = 1.0f;
        private float AltarFactor = 1.0f;
        private float BarracksFactor = 1.0f;
        private float MilitiaFactor = 1.0f;
        private float HunterFactor = 1.0f;
        private float AttackUpgradeFactor = 1.0f;
        private float DefenseUpgradeFactor = 1.0f;
        private float LumbermillFactor = 1.0f;

        public GoalDevelop(GameWorld world, ComputerPlayer player)
        {
            if (player == null || world == null)
            {
                throw new ArgumentNullException();
            }

            owner = player;
            this.world = world;

            RandomizeFactors();
        }

        private void RandomizeFactors()
        {
            HeroFactor = Helper.RandomInRange(0.9f, 1.1f);
            FarmFactor = Helper.RandomInRange(0.9f, 1.1f);
            TownhallFactor = Helper.RandomInRange(0.9f, 1.1f);
            AltarFactor = Helper.RandomInRange(0.9f, 1.1f);
            BarracksFactor = Helper.RandomInRange(0.9f, 1.1f);
            MilitiaFactor = Helper.RandomInRange(0.9f, 1.1f);
            HunterFactor = Helper.RandomInRange(0.9f, 1.1f);
            AttackUpgradeFactor = Helper.RandomInRange(0.9f, 1.1f);
            DefenseUpgradeFactor = Helper.RandomInRange(0.9f, 1.1f);
            LumbermillFactor = Helper.RandomInRange(0.9f, 1.1f);
        }

        private int arbitrateCounter;

        public override void Arbitrate()
        {
            if (++arbitrateCounter > 10)
            {
                RandomizeFactors();
            }

            GatherResource();

            FeedRequests();

            SpendMoney();

            ArbitrateInterval = Helper.RandomInRange(1, 2);
        }

        private void GatherResource()
        {
            var index = 0;
            var count = 0;
            var goldmineDiggerCount = 0;
            var lumberHarvesterCount = 0;
            var builderCount = 0;

            const int MineDiggerCount = 5;
            const int HarvesterCount = 7;

            var idles = new List<Worker>();

            // Check peon states
            foreach (Entity e in owner.EnumerateObjects(owner.WorkerName))
            {
                if (e is not Worker o || o.Owner != owner)
                {
                    continue;
                }

                if (o.State is StateHarvestGold)
                {
                    goldmineDiggerCount++;
                }
                else if (o.State is StateHarvestLumber)
                {
                    lumberHarvesterCount++;
                }
                else if (o.State is StateConstruct)
                {
                    builderCount++;
                }
                else
                {
                    idles.Add(o);
                }

                count++;
            }

            // Harvest gold
            if (goldmineDiggerCount < MineDiggerCount)
            {
                var number = MineDiggerCount - goldmineDiggerCount;
                for (index = 0; index < number; index++)
                {
                    if (index < idles.Count)
                    {
                        goldmineDiggerCount++;
                        idles[index].State = new StateHarvestGold(world, idles[index], (Goldmine)null);
                        idles[index] = null;
                    }
                }
            }

            // Harvest lumber
            if (lumberHarvesterCount < HarvesterCount)
            {
                var number = MineDiggerCount - goldmineDiggerCount;
                for (; index < number; index++)
                {
                    if (index < idles.Count && idles[index] != null)
                    {
                        lumberHarvesterCount++;
                        idles[index].State = new StateHarvestLumber(world, idles[index], (Tree)null);
                    }
                }
            }

            // Change townhall rallypoint
            if (owner.Townhall.RallyPoints.Count <= 0)
            {
                owner.Townhall.PerformAction(
                    StateHarvestLumber.FindAnotherTree(null, owner.Townhall.Position, world), false);
            }

            // Train peons if we're still short of workers
            owner.Request(owner.WorkerName, HarvesterCount + MineDiggerCount,
                2.0f + 1.0f * (HarvesterCount + MineDiggerCount - count) / 5);
        }

        private void FeedRequests()
        {
            // Check for food capacity
            var lack = owner.Food + 5;
            if (lack > Player.MaxFoodCapacity)
            {
                lack = Player.MaxFoodCapacity;
            }

            lack /= GameDefault.Singleton.GetFood(owner.HouseName);

            if (lack < 0)
            {
                lack = 0;
            }

            owner.Request(owner.HouseName, (int)lack,
                (1.2f + 2.3f * (owner.Food - owner.FoodCapacity + 5) / 5) * FarmFactor);

            // Currently requests are fixed :(
            owner.FutureObjects.TryGetValue("Militia", out var militiaCount);
            owner.FutureObjects.TryGetValue("Hunter", out var hunterCount);
            owner.FutureObjects.TryGetValue("Barracks", out var barracksCount);

            owner.Request(owner.HeroName, 1, 2.5f * HeroFactor);
            owner.Request(owner.TownhallName, 1, 3.5f * TownhallFactor);
            owner.Request("Altar", 1, 2 * AltarFactor);

            owner.Request("Militia", 20,
                (1.5f + 1.0f * (4 - militiaCount) / 4) * MilitiaFactor);

            owner.Request("Hunter", 20,
                (1.5f + 1.0f * (4 - hunterCount) / 4) * HunterFactor);

            owner.Request("Barracks", 2,
                ((barracksCount == 0) ? 1.5f : (militiaCount + hunterCount > 6 ? 2.0f : 1.2f)) * BarracksFactor);

            owner.Request("Lumbermill", 1, 2 * LumbermillFactor);

            owner.Request("AttackUpgrade", 1,
                (1.0f + 1.5f * (militiaCount + hunterCount) / 4) * AttackUpgradeFactor);

            owner.Request("DefenseUpgrade", 1,
                (1.0f + 1.5f * (militiaCount + hunterCount) / 4) * DefenseUpgradeFactor);

            owner.Request("PunishOfNatureUpgrade", 1, militiaCount + hunterCount >= 8 ? 2.0f : 0.0f);

            owner.Request(owner.TowerName, 1, Helper.RandomInRange(1.0f, 2.0f));

            if (owner.Attack.MilitaryAdvantage < 0)
            {
                owner.Request(owner.TowerName, (int)(owner.Attack.MilitaryAdvantage * 0.3f),
                              Helper.RandomInRange(2.0f, 2.5f));
            }
        }

        private void SpendMoney()
        {
            if (owner.Requests.Count > 0)
            {
                string type = null;
                var max = float.MinValue;

                foreach (KeyValuePair<string, float> pair in owner.Requests)
                {
                    if (owner.CheckDependency(pair.Key) && pair.Value > max && pair.Value >= 1)
                    {
                        max = pair.Value;
                        type = pair.Key;
                    }
                }

                if (type == null || !owner.HasEnoughMoney(type))
                {
                    return;
                }

                if (Player.IsBuilding(type))
                {
                    owner.Construct(type);
                }
                else // FIXME
                {
                    owner.Train(type);
                }
            }
        }
    }

    public class GoalAttack : Goal
    {
        private readonly ComputerPlayer owner;
        public float MilitaryAdvantage;
        private int advantageCounter;

        public GoalAttack(GameWorld world, ComputerPlayer player)
        {
            if (player == null || world == null)
            {
                throw new ArgumentNullException();
            }

            owner = player;
        }

        public override void Arbitrate()
        {
            MilitaryAdvantage = MathHelper.Lerp(
                MilitaryAdvantage, ComputeMilitaryAdvantage(), 0.5f);

            if (MilitaryAdvantage > 0)
            {
                if (++advantageCounter > 5)
                {
                    advantageCounter = 0;

                    // Press the attack
                    Attack();
                }
            }
            else
            {
                advantageCounter = 0;
            }

            ArbitrateInterval = Helper.RandomInRange(5, 10);
        }

        public float ComputeMilitaryAdvantage()
        {
            float ourForce = 0;
            float enermyForce = 0;
            float distanceFactor = 0;

            foreach (GameObject o in owner.Enermy.EnumerateObjects())
            {
                var threat = ComputeThreat(o);
                enermyForce += o is Worker ? threat / 5 : threat;
            }

            foreach (GameObject o in owner.EnumerateObjects())
            {
                var threat = ComputeThreat(o);
                ourForce += o is Worker ? threat / 5 : threat;
            }

            // Add a distance factor
            LinkedList<GameObject> towns = owner.Enermy.GetObjects(
                                           owner.Enermy.TownhallName);

            if (towns != null && towns.Count > 0)
            {
                Vector2 v;

                v.X = towns.First.Value.Position.X - owner.SpawnPoint.X;
                v.Y = towns.First.Value.Position.Y - owner.SpawnPoint.Y;

                distanceFactor = v.Length() * 0.003f;
            }

            return ourForce - enermyForce + distanceFactor;
        }

        public static float ComputeThreat(GameObject o)
        {
            var threat = (o.AttackPoint.X + o.AttackPoint.Y) / 2 +
                           (o.DefensePoint.X + o.DefensePoint.Y) * 1.0f / 300.0f;

            return threat > 1 ? 1 : 0;
        }

        private int squadCount = 5;

        public void Attack()
        {
            var charactors = new List<Charactor>();

            // Find all the soldiers we've got
            foreach (GameObject o in owner.EnumerateObjects())
            {
                if (o is Charactor && !(o is Worker))
                {
                    charactors.Add(o as Charactor);
                }
            }

            if (charactors.Count >= squadCount)
            {
                Vector3 target = GetAttackTarget(owner.Enermy);

                foreach (Charactor c in charactors)
                {
                    c.AttackTo(target, false);
                }

                squadCount += 5;
                if (squadCount > 20)
                {
                    squadCount = 20;
                }
            }
        }

        private Vector3 GetAttackTarget(Player enermy)
        {
            GameObject target = null;
            var min = float.MaxValue;

            foreach (GameObject o in enermy.EnumerateObjects())
            {
                var priority = o.Priority;
                if (o is Building)
                {
                    priority -= 200;
                }

                if (priority < min)
                {
                    min = priority;
                    target = o;
                }
            }

            return target != null ?
                   target.Position : new Vector3(enermy.SpawnPoint, 0);
        }
    }

    public class GoalDefend : Goal
    {
        private readonly GameWorld world;
        private readonly ComputerPlayer owner;

        public GoalDefend(GameWorld world, ComputerPlayer player)
        {
            if (player == null || world == null)
            {
                throw new ArgumentNullException();
            }

            owner = player;
            this.world = world;
        }

        public override void Arbitrate()
        {
            Vector3? attacker = UnderAttack(out var attackerCount);

            if (attacker.HasValue)
            {
                // Defense
                var defenderCounter = 0;

                foreach (IWorldObject o in
                    world.GetNearbyObjects(owner.Townhall.Position, 200))
                {
                    if (o is Charactor c && c.Owner == owner && !(c is Worker))
                    {
                        defenderCounter++;
                        c.AttackTo(attacker.Value, false);
                    }
                }

                if (defenderCounter < attackerCount)
                {
                    // Peons, attack!
                    var number = (attackerCount - defenderCounter) * 2;

                    foreach (Worker peon in owner.EnumerateObjects(owner.WorkerName))
                    {
                        if (--number > 0)
                        {
                            IState state = peon.State;
                            peon.AttackTo(attacker.Value, false);
                            peon.QueuedStates.Enqueue(state);
                        }
                    }
                }

                ArbitrateInterval = Helper.RandomInRange(6, 10);
            }
            else
            {
                ArbitrateInterval = Helper.RandomInRange(2, 3);
            }
        }

        private Vector3? UnderAttack(out int count)
        {
            count = 0;
            GameObject attacker = null;

            foreach (IWorldObject o in
                world.GetNearbyObjects(owner.Townhall.Position, 200))
            {
                if (o is GameObject && (o as GameObject).Owner != null &&
                    owner.GetRelation((o as GameObject).Owner) == PlayerRelation.Opponent)
                {
                    count++;
                    if (attacker == null)
                    {
                        attacker = o as GameObject;
                    }
                }
            }

            return attacker != null ? attacker.Position : null;
        }
    }
}