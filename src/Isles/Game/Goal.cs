//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Isles.Engine;
using Isles.Graphics;


namespace Isles
{
    #region Goal
    public abstract class Goal : BaseState
    {
        public float ArbitrateInterval = 2;
        double arbitrateTimer;

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
    #endregion
   
    #region GoalDevelop
    public class GoalDevelop : Goal
    {
        GameWorld world;
        ComputerPlayer owner;

        float WorkerFactor = 1.0f;
        float FarmFactor = 1.0f;
        float HeroFactor = 1.0f;
        float TownhallFactor = 1.0f;
        float AltarFactor = 1.0f;
        float BarracksFactor = 1.0f;
        float MilitiaFactor = 1.0f;
        float HunterFactor = 1.0f;
        float AttackUpgradeFactor = 1.0f;
        float DefenseUpgradeFactor = 1.0f;
        float LumbermillFactor = 1.0f;

        public GoalDevelop(GameWorld world, ComputerPlayer player)
        {
            if (player == null || world == null)
                throw new ArgumentNullException();

            this.owner = player;
            this.world = world;

            RandomizeFactors();
        }

        private void RandomizeFactors()
        {
            WorkerFactor = Helper.RandomInRange(0.9f, 1.1f);
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

        int arbitrateCounter = 0;

        public override void Arbitrate()
        {
            if (++arbitrateCounter > 10)
                RandomizeFactors();

            GatherResource();

            FeedRequests();

            SpendMoney();

            ArbitrateInterval = Helper.RandomInRange(1, 2);
        }

        private void GatherResource()
        {
            int index = 0;
            int count = 0;
            int goldmineDiggerCount = 0;
            int lumberHarvesterCount = 0;
            int builderCount = 0;

            const int MineDiggerCount = 5;
            const int HarvesterCount = 7;

            List<Worker> idles = new List<Worker>();

            // Check peon states
            foreach (Entity e in owner.EnumerateObjects(owner.WorkerName))
            {
                Worker o = e as Worker;

                if (o == null || o.Owner != owner)
                    continue;

                if (o.State is StateHarvestGold)
                    goldmineDiggerCount++;
                else if (o.State is StateHarvestLumber)
                    lumberHarvesterCount++;
                else if (o.State is StateConstruct)
                    builderCount++;
                else
                    idles.Add(o);

                count++;
            }

            // Harvest gold
            if (goldmineDiggerCount < MineDiggerCount)
            {
                int number = MineDiggerCount - goldmineDiggerCount;
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
                int number = MineDiggerCount - goldmineDiggerCount;
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
                (2.0f + 1.0f * (HarvesterCount + MineDiggerCount - count) / 5 ));
        }

        private void FeedRequests()
        {
            // Check for food capacity
            float lack = owner.Food + 5;
            if (lack > Player.MaxFoodCapacity)
                lack = Player.MaxFoodCapacity;
            lack /= GameDefault.Singleton.GetFood(owner.HouseName);

            if (lack < 0)
                lack = 0;

            owner.Request(owner.HouseName, (int)lack,
                (1.2f + 2.3f * (owner.Food - owner.FoodCapacity + 5) / 5) * FarmFactor);

            // Currently requests are fixed :(
            int militiaCount = 0, hunterCount = 0, barracksCount = 0;

            owner.FutureObjects.TryGetValue(owner.Race == Race.Islander ? "Militia" : "Swordman", out militiaCount);
            owner.FutureObjects.TryGetValue(owner.Race == Race.Islander ? "Hunter" : "Rifleman", out hunterCount);
            owner.FutureObjects.TryGetValue(owner.Race == Race.Islander ? "Barracks" : "TrainingCenter", out barracksCount);

            owner.Request(owner.HeroName, 1, 2.5f * HeroFactor);
            owner.Request(owner.TownhallName, 1, 3.5f * TownhallFactor);
            owner.Request(owner.Race == Race.Islander ? "Altar" : "SteamFactory", 1, 2 * AltarFactor);

            owner.Request(owner.Race == Race.Islander ? "Militia" : "Swordman", 20,
                (1.5f + 1.0f * (4 - militiaCount) / 4) * MilitiaFactor);

            owner.Request(owner.Race == Race.Islander ? "Hunter" : "Rifleman", 20, 
                (1.5f + 1.0f * (4 - hunterCount) / 4) * HunterFactor);

            owner.Request(owner.Race == Race.Islander ? "Barracks" : "TrainingCenter", 2,
                ((barracksCount == 0) ? 1.5f : (militiaCount + hunterCount > 6 ? 2.0f : 1.2f)) * BarracksFactor);

            owner.Request(owner.Race == Race.Islander ? "Lumbermill" : "Regenerator", 1, 2 * LumbermillFactor);

            owner.Request("AttackUpgrade", 1, 
                (1.0f + 1.5f * (militiaCount + hunterCount) / 4) * AttackUpgradeFactor);

            owner.Request("DefenseUpgrade", 1,
                (1.0f + 1.5f * (militiaCount + hunterCount) / 4) * DefenseUpgradeFactor);

            if (owner.Race == Race.Islander)
            {
                //owner.Request("LiveOfNature", 1, Helper.RandomInRange(1.8f, 2.2f));
                owner.Request("PunishOfNatureUpgrade", 1, (militiaCount + hunterCount >= 8 ? 2.0f : 0.0f));
            }

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
                float max = float.MinValue;

                foreach (KeyValuePair<string, float> pair in owner.Requests)
                {
                    if (owner.CheckDependency(pair.Key) && pair.Value > max && pair.Value >= 1)
                    {
                        max = pair.Value;
                        type = pair.Key;
                    }
                }

                if (type == null || !owner.HasEnoughMoney(type))
                    return;

                if (Player.IsBuilding(type))
                    owner.Construct(type);
                else // FIXME
                    owner.Train(type);
            }
        }
    }
    #endregion

    #region GoalAttack
    public class GoalAttack : Goal
    {
        GameWorld world;
        ComputerPlayer owner;
        public float MilitaryAdvantage;
        int advantageCounter = 0;

        public GoalAttack(GameWorld world, ComputerPlayer player)
        {
            if (player == null || world == null)
                throw new ArgumentNullException();

            this.owner = player;
            this.world = world;
        }

        //public override StateResult Update(GameTime gameTime)
        //{
        //    BaseGame.Singleton.Graphics2D.DrawShadowedString(
        //        "Advantage: " + militaryAdvantage + "\nCounter: " + advantageCounter,
        //        20, new Vector2(0, 300), Color.Pink, Color.Black);

        //    return base.Update(gameTime);
        //}

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
                float threat = ComputeThreat(o);
                enermyForce += (o is Worker ? threat / 5 : threat);
            }

            foreach (GameObject o in owner.EnumerateObjects())
            {
                float threat = ComputeThreat(o);
                ourForce += (o is Worker ? threat / 5 : threat);
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
            float threat = (o.AttackPoint.X + o.AttackPoint.Y) / 2 +
                           (o.DefensePoint.X + o.DefensePoint.Y) * 1.0f / 300.0f;

            return threat > 1 ? 1 : 0;
        }

        int squadCount = 5;

        public void Attack()
        {
            List<Charactor> charactors = new List<Charactor>();

            // Find all the soldiers we've got
            foreach (GameObject o in owner.EnumerateObjects())
            {
                if (o is Charactor && !(o is Worker))
                    charactors.Add(o as Charactor);
            }

            if (charactors.Count >= squadCount)
            {
                Vector3 target = GetAttackTarget(owner.Enermy);

                foreach (Charactor c in charactors)
                    c.AttackTo(target, false);

                squadCount += 5;
                if (squadCount > 20)
                    squadCount = 20;
            }
        }

        Vector3 GetAttackTarget(Player enermy)
        {
            GameObject target = null;
            float min = float.MaxValue;

            foreach (GameObject o in enermy.EnumerateObjects())
            {
                float priority = o.Priority;
                if (o is Building)
                    priority -= 200;

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
    #endregion

    #region GoalDefend
    public class GoalDefend : Goal
    {
        GameWorld world;
        ComputerPlayer owner;

        public GoalDefend(GameWorld world, ComputerPlayer player)
        {
            if (player == null || world == null)
                throw new ArgumentNullException();

            this.owner = player;
            this.world = world;
        }

        public override void Arbitrate()
        {
            int attackerCount;
            Vector3? attacker = UnderAttack(out attackerCount);

            if (attacker.HasValue)
            {
                // Defense
                int defenderCounter = 0;

                foreach (IWorldObject o in
                    world.GetNearbyObjects(owner.Townhall.Position, 200))
                {
                    Charactor c = o as Charactor;

                    if (c != null && c.Owner == owner && !(c is Worker))
                    {
                        defenderCounter++;
                        c.AttackTo(attacker.Value, false);
                    }
                }

                if (defenderCounter < attackerCount)
                {
                    // Peons, attack!
                    int number = (attackerCount - defenderCounter) * 2;

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
                        attacker = o as GameObject;
                }
            }

            if (attacker != null)
                return attacker.Position;
            return null;
        }
    }
    #endregion
}