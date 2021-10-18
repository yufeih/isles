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
using Isles.Pipeline;
using Isles.Engine;
using Isles.Graphics;


namespace Isles
{
    #region StateHarvestGold
    public class StateHarvestGold : BaseState
    {
        /// <summary>
        /// For now, MaxPeonsPerGoldmine should always be set to 1
        /// </summary>
        public const int MaxPeonsPerGoldmine = 1;
        public const int GoldHarvestedPerTime = 10;
        public const double WorkTime = 1.0f;

        /// <summary>
        /// State transitions
        /// </summary>
        enum StateType
        {
            MoveToGoldmine, Wait, Harvest, BackToDeposit
        }

        StateType state;

        /// <summary>
        /// Common stuff
        /// </summary>
        Worker owner;
        GameWorld world;
        Goldmine goldmine;
        Building deposit;
        StateMoveToPosition move;
        double elapsedWorkTime;

        public StateHarvestGold(GameWorld world, Worker peon, Goldmine goldmine)
        {
            if (world == null || peon == null)
                throw new ArgumentNullException();

            this.owner = peon;
            this.world = world;
            this.goldmine = goldmine;
            this.owner.LumberCarried = 0;

            // Initialize state
            goldmine = FindAnotherGoldmine();
            state = StateType.MoveToGoldmine;
        }

        public StateHarvestGold(GameWorld world, Worker peon, Building deposit)
        {
            if (world == null || peon == null || deposit == null)
                throw new ArgumentNullException();

            if (peon.Owner != deposit.Owner ||
                deposit.State != Building.BuildingState.Normal)
            {
                throw new InvalidOperationException();
            }

            this.owner = peon;
            this.world = world;
            this.deposit = deposit;
            this.owner.LumberCarried = 0;

            // Initialize state
            deposit = owner.Owner.FindNearestObject(
                      owner.Position, owner.Owner.TownhallName, null) as Building;
            state = StateType.BackToDeposit;
        }

        public override void Activate() { }

        public override void Terminate()
        {
            if (state == StateType.Harvest || state == StateType.Wait)
            {
                // Respawn our charactor
                owner.Spawn(goldmine.Position + goldmine.SpawnPoint);
            }

            if (state == StateType.Harvest && goldmine != null)
            {
                goldmine.HarvesterCount--;
                goldmine = null;
            }

            // Make sure we do not ignore dynamic obstacles when we leave this state
            if (owner.IgnoreDynamicObstacles)
                owner.IgnoreDynamicObstacles = false;

            owner.Stop();
            state = StateType.MoveToGoldmine;
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (state == StateType.MoveToGoldmine)
            {
                // Checks if the target tree can be harvested
                if (!CanGoldmineBeHarvested(goldmine))
                {
                    goldmine = FindAnotherGoldmine();

                    // No tree found, return failure
                    if (goldmine == null)
                    {
                        if (owner.GoldCarried == 0)
                            return StateResult.Failed;

                        // If we have some lumber in our pocket, send them back to deposit
                        state = StateType.BackToDeposit;
                        return StateResult.Active;
                    }

                    // Move to the new tree
                    move = null;
                }

                // Start move if we're stopped
                if (move == null)
                {
                    Vector2 target;
                    target.X = goldmine.Position.X + goldmine.SpawnPoint.X;
                    target.Y = goldmine.Position.Y + goldmine.SpawnPoint.Y;
                    move = new StateMoveToPosition(target, owner, owner.Priority, world.PathManager);
                }

                // Update move
                StateResult result = move.Update(gameTime);

                // Return failure if we can't get there?
                if (result == StateResult.Failed)
                    return StateResult.Failed;

                if (owner.TargetReached(goldmine))
                {
                    // Stop moving and wait to get into the goldmine
                    owner.Stop();
                    owner.Unspawn();
                    move = null;
                    state = StateType.Wait;
                }
                else if (result == StateResult.Completed)
                {
                    // If we completed moving to the tree but failed to
                    // reach it, find another tree
                    goldmine = FindAnotherGoldmine();
                    move = null;
                }
            }
            else if (state == StateType.Wait)
            {
                // Checks if the goldmine has collapsed
                if (goldmine == null || goldmine.Gold <= 0)
                    return StateResult.Failed;

                // Checks if we can start to work
                if (goldmine.HarvesterCount < MaxPeonsPerGoldmine)
                {
                    // Start working
                    goldmine.HarvesterCount++;
                    elapsedWorkTime = 0;
                    state = StateType.Harvest;
                }
            }
            else if (state == StateType.Harvest)
            {
                // See if we've worked enough time
                if ((elapsedWorkTime += gameTime.ElapsedGameTime.TotalSeconds) >= WorkTime)
                {
                    int harvested = GoldHarvestedPerTime;
                    if (harvested > goldmine.Gold)
                        harvested = goldmine.Gold;
                    if (harvested > owner.GoldCapacity - owner.GoldCarried)
                        harvested = owner.GoldCapacity - owner.GoldCarried;
                    goldmine.Gold -= harvested;
                    owner.GoldCarried += harvested;

                    // Back to home happily
                    owner.Spawn(goldmine.Position + goldmine.SpawnPoint);
                    goldmine.HarvesterCount--;
                    elapsedWorkTime = 0;

                    // Let agents ignore dynamic obstacles when moving back
                    // home. This will make the journey more efficient :)
                    owner.IgnoreDynamicObstacles = true;

                    state = StateType.BackToDeposit;
                }
            }
            else if (state == StateType.BackToDeposit)
            {
                // Find a new deposit if we currently don't have one
                if (deposit == null)
                {
                    deposit = owner.Owner.FindNearestObject(
                              owner.Position, owner.Owner.TownhallName, null) as Building;

                    if (deposit == null)
                    {
                        // Do forget to set IgnoreDynamicObstacles to false
                        // anytime we leave BackToDeposit state, otherwise
                        // this bug can be used to create flying charactors :(
                        owner.IgnoreDynamicObstacles = false;

                        return StateResult.Failed;
                    }

                    // Move to the new deposit
                    move = null;
                }

                // Start move if we're stopped
                if (move == null)
                {
                    owner.Facing = deposit.Position - owner.Position;
                    move = new StateMoveToPosition(
                           new Vector2(deposit.Position.X, deposit.Position.Y),
                                       owner, owner.Priority, world.PathManager);
                }

                // Update move
                StateResult result = move.Update(gameTime);

                // Return failure if we can't get there?
                if (result == StateResult.Failed)
                    return StateResult.Failed;

                if (owner.TargetReached(deposit))
                {
                    if (owner.Visible && !owner.InFogOfWar)
                    {
                        GameUI.Singleton.ShowMessage("+" + owner.GoldCarried, owner.TopCenter,
                                             MessageType.None, MessageStyle.BubbleUp, Color.Gold);
                    }

                    owner.Owner.Gold += owner.GoldCarried;

                    // Cheat! Cheat!
                    if (owner.Owner is ComputerPlayer && BaseGame.Singleton.Settings.Cheat)
                        owner.Owner.Gold += owner.GoldCarried;

                    owner.GoldCarried = 0;

                    // Make sure we alway go back to the nearest available deposit
                    deposit = null;
                    move = null;
                    state = StateType.MoveToGoldmine;
                }
                else if (result == StateResult.Completed)
                {
                    deposit = owner.Owner.FindNearestObject(
                              owner.Position, owner.Owner.TownhallName, deposit) as Building;

                    if (deposit == null)
                        return StateResult.Failed;

                    move = null;
                }
            }

            return StateResult.Active;
        }

        private bool CanGoldmineBeHarvested(Goldmine mine)
        {
            return mine != null && mine.Gold > 0;
        }

        private Goldmine FindAnotherGoldmine()
        {
            Goldmine minGoldmine = null;
            float distanceSq = float.MaxValue;

            foreach (IWorldObject o in world.GetNearbyObjects(owner.Position, 500))
            {
                if (o is Goldmine)
                {
                    Goldmine anotherGoldmine = o as Goldmine;

                    if (anotherGoldmine == null || anotherGoldmine == goldmine ||
                        !CanGoldmineBeHarvested(anotherGoldmine))
                        continue;

                    float lenSq = Vector3.Subtract(anotherGoldmine.Position, owner.Position).LengthSquared();
                    if (lenSq < distanceSq)
                    {
                        minGoldmine = anotherGoldmine;
                        distanceSq = lenSq;
                    }
                }
            }

            return minGoldmine;
        }
    }
    #endregion

    #region StateHarvestLumber
    public class StateHarvestLumber : BaseState
    {
        /// <summary>
        /// Constants or properties
        /// </summary>
        public const int MaxPeonsPerTree = 2;
        public const int LumberHarvestedPerHit = 1;

        /// <summary>
        /// State transitions
        /// </summary>
        enum StateType
        {
            MoveToTree, Harvest, BackToDeposit
        }

        StateType state;

        /// <summary>
        /// Common stuff
        /// </summary>
        Worker owner;
        GameWorld world;
        Tree tree;
        Building deposit;
        StateMoveToPosition move;
        KeyValuePair<TimeSpan, EventHandler>[] trigger;

        public StateHarvestLumber(GameWorld world, Worker peon, Tree tree)
        {
            if (world == null || peon == null)
                throw new ArgumentNullException();

            this.owner = peon;
            this.tree = tree;
            this.world = world;
            this.owner.GoldCarried = 0;

            AnimationClip clip = owner.Model.GetAnimationClip(owner.AttackAnimation);
            if (clip == null)
                throw new InvalidOperationException();

            TimeSpan time = new TimeSpan((long)(clip.Duration.Ticks * 13.0f / 20));
            trigger = new KeyValuePair<TimeSpan, EventHandler>[]
            {
                new KeyValuePair<TimeSpan, EventHandler>(time, HarvestOnce),
            };

            // Initialize state
            tree = FindAnotherTree(tree, owner.Position, world);
            state = StateType.MoveToTree;
        }

        public StateHarvestLumber(GameWorld world, Worker peon, Building deposit)
        {
            if (world == null || peon == null || deposit == null)
                throw new ArgumentNullException();

            if (deposit.Owner != peon.Owner ||
                deposit.State != Building.BuildingState.Normal)
            {
                throw new InvalidOperationException();
            }

            this.owner = peon;
            this.world = world;
            this.owner.GoldCarried = 0;
            this.deposit = deposit;

            AnimationClip clip = owner.Model.GetAnimationClip(owner.AttackAnimation);
            if (clip == null)
                throw new InvalidOperationException();

            TimeSpan time = new TimeSpan((long)(clip.Duration.Ticks * 13.0f / 20));
            trigger = new KeyValuePair<TimeSpan, EventHandler>[]
            {
                new KeyValuePair<TimeSpan, EventHandler>(time, HarvestOnce),
            };

            // Initialize state
            deposit = FindDeposit();
            state = StateType.BackToDeposit;
        }

        private Building FindDeposit()
        {
            Building townhall = owner.Owner.FindNearestObject(
                      owner.Position, owner.Owner.TownhallName, null) as Building;

            Building lumbermill = owner.Owner.FindNearestObject(
                      owner.Position, owner.Owner.LumbermillName, null) as Building;

            if (lumbermill == null && townhall != null)
                return townhall;

            if (townhall == null && lumbermill != null)
                return lumbermill;

            if (townhall == null && lumbermill == null)
                return null;

            Vector2 v1, v2;

            v1.X = townhall.Position.X - owner.Position.X;
            v1.Y = townhall.Position.Y - owner.Position.Y;

            v2.X = lumbermill.Position.X - owner.Position.X;
            v2.Y = lumbermill.Position.Y - owner.Position.Y;

            if (v1.LengthSquared() < v2.LengthSquared())
                return townhall;

            return lumbermill;
        }

        public override void Activate() { }

        public override void Terminate()
        {
            if (state == StateType.Harvest && tree != null)
            {
                tree.HarvesterCount--;
                tree = null;
            }

            owner.Stop();
            state = StateType.MoveToTree;
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (state == StateType.MoveToTree)
            {
                // Checks if the target tree can be harvested
                if (!CanTreeBeHarvested(tree))
                {
                    tree = FindAnotherTree(tree, owner.Position, world);

                    // No tree found, return failure
                    if (tree == null)
                    {
                        if (owner.LumberCarried == 0)
                            return StateResult.Failed;

                        // If we have some lumber in our pocket, send them back to deposit
                        state = StateType.BackToDeposit;
                        return StateResult.Active;
                    }

                    // Move to the new tree
                    move = null;
                }

                // Start move if we're stopped
                if (move == null)
                {
                    move = new StateMoveToPosition(
                           new Vector2(tree.Position.X, tree.Position.Y),
                                       owner, owner.Priority, world.PathManager);
                }

                // Update move
                StateResult result = move.Update(gameTime);

                // Return failure if we can't get there?
                if (result == StateResult.Failed)
                    return StateResult.Failed;

                if (tree != null && owner.TargetPointReached(tree.Position))
                {
                    // Stop moving and start harvest lumber
                    owner.Stop();
                    owner.Facing = tree.Position - owner.Position;
                    owner.Model.Play(owner.AttackAnimation, true, 0.2f, OnComplete, trigger);

                    move = null;
                    tree.HarvesterCount++;
                    state = StateType.Harvest;
                }
                else if (result == StateResult.Completed)
                {
                    // If we completed moving to the tree but failed to
                    // reach it, find another tree
                    tree = FindAnotherTree(tree, owner.Position, world);
                    move = null;
                }
            }
            // The logic of harvesting will be handled in animation callback
            // instead of this update.
            else if (state == StateType.BackToDeposit)
            {
                // Find a new deposit if we currently don't have one
                if (deposit == null)
                {
                    deposit = FindDeposit();

                    if (deposit == null)
                        return StateResult.Failed;

                    // Move to the new deposit
                    move = null;
                }

                // Start move if we're stopped
                if (move == null)
                {
                    move = new StateMoveToPosition(
                           new Vector2(deposit.Position.X, deposit.Position.Y),
                                       owner, owner.Priority, world.PathManager);
                }

                // Update move
                StateResult result = move.Update(gameTime);

                // Return failure if we can't get there?
                if (result == StateResult.Failed)
                    return StateResult.Failed;

                if (owner.TargetReached(deposit))
                {
                    owner.Stop();

                    if (owner.Visible && !owner.InFogOfWar)
                    {
                        GameUI.Singleton.ShowMessage("+" + owner.LumberCarried, owner.TopCenter,
                                             MessageType.None, MessageStyle.BubbleUp, Color.Green);
                    }

                    owner.Owner.Lumber += owner.LumberCarried;

                    // Cheat! Cheat!
                    if (owner.Owner is ComputerPlayer && BaseGame.Singleton.Settings.Cheat)
                        owner.Owner.Lumber += owner.LumberCarried;

                    owner.LumberCarried = 0;

                    // Make sure we alway go back to the nearest available deposit
                    deposit = null;
                    move = null;
                    state = StateType.MoveToTree;
                }
                else if (result == StateResult.Completed)
                {
                    deposit = FindDeposit();
                    move = null;
                }
            }

            return StateResult.Active;
        }

        void OnComplete(object sender, EventArgs e)
        {
            //string anim = owner.AttackAnimation;

            //AnimationClip clip = owner.Model.GetAnimationClip(anim);

            //TimeSpan time = new TimeSpan((long)(clip.Duration.Ticks * 13.0f / 20));

            //trigger = new KeyValuePair<TimeSpan, EventHandler>[]
            //{
            //    new KeyValuePair<TimeSpan, EventHandler>(time, HarvestOnce),
            //};

            //owner.Model.Play(owner.AttackAnimation, true, 0.2f, OnComplete, trigger);
        }

        private void HarvestOnce(object sender, EventArgs e)
        {
            // Make sure we are in the correct state
            if (state != StateType.Harvest || tree == null || tree.Lumber < 0)
                return;

            if (owner.Owner is LocalPlayer && Helper.Random.Next(2) == 0)
                Audios.Play("ChopWood", owner);

            // Harvest once
            int harvested = LumberHarvestedPerHit;
            if (harvested > tree.Lumber)
                harvested = tree.Lumber;
            if (harvested > owner.LumberCapacity - owner.LumberCarried)
                harvested = owner.LumberCapacity - owner.LumberCarried;
            tree.Lumber -= harvested;
            owner.LumberCarried += harvested;

            // Tell the tree to play hit animation
            tree.Hit(owner);
            
            // Check if the tree falls
            if (tree.Lumber <= 0)
            {
                tree.HarvesterCount--;
                tree = null;
                state = StateType.MoveToTree;
            }

            // Check if our pocket if full
            if (owner.LumberCarried == owner.LumberCapacity)
            {
                if (tree != null)
                    tree.HarvesterCount--;

                move = null;
                state = StateType.BackToDeposit;
            }
        }

        private static bool CanTreeBeHarvested(Tree tree)
        {
            return tree != null && tree.HarvesterCount < MaxPeonsPerTree && tree.Lumber > 0;
        }

        public static Tree FindAnotherTree(Tree existingTree, Vector3 position, GameWorld world)
        {
            Tree minTree = null;
            float distanceSq = float.MaxValue;

            foreach (IWorldObject o in world.GetNearbyObjects(position, 500))
            {
                Tree anotherTree = o as Tree;

                if (anotherTree == null || anotherTree == existingTree || !CanTreeBeHarvested(anotherTree))
                    continue;

                float lenSq = Vector3.Subtract(anotherTree.Position, position).LengthSquared();
                if (lenSq < distanceSq)
                {
                    minTree = anotherTree;
                    distanceSq = lenSq;
                }
            }

            return minTree;
        }
    }
    #endregion

    #region StateConstruct
    public class StateConstruct : BaseState
    {
        /// <summary>
        /// State transitions
        /// </summary>
        enum StateType
        {
            MoveToBuilding, Build
        }

        StateType state;

        /// <summary>
        /// Common stuff
        /// </summary>
        Charactor owner;
        Building building;
        GameWorld world;
        StateMoveToPosition move;

        public StateConstruct(GameWorld world, Charactor builder, Building building)
        {
            if (building == null || builder == null || world == null)
                throw new ArgumentNullException();

            if (building.Owner != builder.Owner ||
                building.State != Building.BuildingState.Constructing)
            {
                throw new InvalidOperationException();
            }

            this.world = world;
            this.owner = builder;
            this.building = building;
            this.state = StateType.MoveToBuilding;
        }

        public override void Activate() { }

        public override void Terminate()
        {
            if (state == StateType.Build)
            {
                owner.Stop();
                building.BuilderCount--;
            }

            move = null;
            state = StateType.MoveToBuilding;
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (state == StateType.MoveToBuilding)
            {
                // Move to target if we're stopped
                if (move == null)
                {
                    Vector2 target;
                    target.X = building.Position.X;
                    target.Y = building.Position.Y;

                    move = new StateMoveToPosition(target, owner, owner.Priority, world.PathManager);
                }

                StateResult result = move.Update(gameTime);

                // Notify failure if we failed to move to the target
                if (result == StateResult.Failed)
                    return StateResult.Failed;

                // Checks if the build is completed
                if (IsBuildCompleted(building))
                    return StateResult.Completed;

                // Checks if we've reached the building
                if (owner.TargetReached(building))
                {
                    // Happy constructing :)
                    building.BuilderCount++;
                    move = null;
                    owner.Stop();
                    owner.Facing = building.Position - owner.Position;
                    owner.Model.Play(owner.AttackAnimation, true, 0.2f, Hit, null);
                    state = StateType.Build;
                }
                else if (result == StateResult.Completed)
                {
                    // There're cases when the move is completed but we
                    // failed to get close enough to the target, so try again
                    move = null;
                }
            }
            else if (state == StateType.Build)
            {
                // Check if the build is completed
                if (IsBuildCompleted(building))
                    return StateResult.Completed;
            }

            return StateResult.Active;
        }

        private void Hit(object sender, EventArgs e)
        {
            if (owner.Owner is LocalPlayer)
                Audios.Play("ChopWood", owner);
        }

        private bool IsBuildCompleted(Building building)
        {
            return building == null || building.State != Building.BuildingState.Constructing;
        }
    }
    #endregion

    #region StateRepair
    public class StateRepair : BaseState
    {
        /// <summary>
        /// State transitions
        /// </summary>
        enum StateType
        {
            MoveToBuilding, Repair
        }

        StateType state;

        /// <summary>
        /// Common stuff
        /// </summary>
        Charactor owner;
        Building building;
        GameWorld world;
        StateMoveToPosition move;

        public StateRepair(GameWorld world, Charactor owner, Building building)
        {
            if (building == null || owner == null || world == null)
                throw new ArgumentNullException();

            if (building.Owner != owner.Owner ||
                building.State != Building.BuildingState.Normal)
            {
                throw new InvalidOperationException();
            }

            this.world = world;
            this.owner = owner;
            this.building = building;
            this.state = StateType.MoveToBuilding;
        }

        public override void Activate() { }

        public override void Terminate()
        {
            if (state == StateType.Repair)
            {
                owner.Stop();
                building.BuilderCount--;
            }

            move = null;
            state = StateType.MoveToBuilding;
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (state == StateType.MoveToBuilding)
            {
                // Move to target if we're stopped
                if (move == null)
                {
                    Vector2 target;
                    target.X = building.Position.X;
                    target.Y = building.Position.Y;

                    move = new StateMoveToPosition(target, owner, owner.Priority, world.PathManager);
                }

                StateResult result = move.Update(gameTime);

                // Notify failure if we failed to move to the target
                if (result == StateResult.Failed)
                    return StateResult.Failed;

                // Checks if the build is completed
                if (IsRepairCompleted(building))
                    return StateResult.Completed;

                // Checks if we've reached the building
                if (owner.TargetReached(building))
                {
                    // Happy constructing :)
                    building.BuilderCount++;
                    move = null;
                    owner.Stop();
                    owner.Facing = building.Position - owner.Position;
                    owner.Model.Play(owner.AttackAnimation, true, 0.2f, Hit, null);
                    state = StateType.Repair;
                }
                else if (result == StateResult.Completed)
                {
                    // There're cases when the move is completed but we
                    // failed to get close enough to the target, so try again
                    move = null;
                }
            }
            else if (state == StateType.Repair)
            {
                // Just check if the repair is completed
                if (IsRepairCompleted(building))
                    return StateResult.Completed;
            }

            return StateResult.Active;
        }

        private void Hit(object sender, EventArgs e)
        {
            if (owner.Owner is LocalPlayer)
                Audios.Play("ChopWood", owner);
        }

        private bool IsRepairCompleted(Building building)
        {
            return building == null || building.State != Building.BuildingState.Normal ||
                                      (building.State == Building.BuildingState.Normal &&
                                       building.Health == building.MaximumHealth);
        }
    }
    #endregion

    #region StateCharactorIdle
    public class StateCharactorIdle : BaseState
    {
        double arbitrateTimer = 0;
        Random random = new Random();
        Charactor owner;

        public StateCharactorIdle(Charactor owner)
        {
            if (owner == null)
                throw new ArgumentNullException();

            this.owner = owner;
        }

        public override void Activate() { }
        public override void Terminate() { }

        public override StateResult Update(GameTime gameTime)
        {
            // Check for queued states
            if (owner.QueuedStates.Count > 0)
                return StateResult.Completed;

            arbitrateTimer -= gameTime.ElapsedGameTime.TotalSeconds;

            if (arbitrateTimer <= 0)
            {
                arbitrateTimer = random.NextDouble() * 1 + 1;

                // Checks if we can attack somebody
                foreach (IWorldObject worldObject in
                    owner.World.GetNearbyObjects(owner.Position, owner.ViewDistance))
                {
                    GameObject o = worldObject as GameObject;

                    if (o != null && owner.IsOpponent(o) && 
                        o.IsAlive && o.Visible && !o.InFogOfWar)
                    {
                        owner.AttackTo(o, false);
                        break;
                    }
                }
            }

            return StateResult.Active;
        }
    }
    #endregion

    #region StateCharactorDie
    public class StateCharactorDie : BaseState
    {
        bool sink = false;
        float height = 5.0f;
        float baseHeight;
        Charactor owner;

        public StateCharactorDie(Charactor owner)
        {
            this.owner = owner;
        }

        public override void Activate()
        {
            if (owner.Model != null)
                owner.Model.Play("Die", false, 0.0f, BeginSink, null);
            else
                BeginSink(null, EventArgs.Empty);
        }

        private void BeginSink(object sender, EventArgs e)
        {
            if (owner.Model != null)
                owner.Model.Pause();

            sink = true;
            baseHeight = owner.World.Landscape.GetHeight(
                owner.Position.X, owner.Position.Y) - height;
        }

        public override void Terminate() { }

        public override StateResult Update(GameTime gameTime)
        {
            ActivateIfInactive();

            if (sink)
            {
                height -= (float)(gameTime.ElapsedGameTime.TotalSeconds);
                owner.Position = new Vector3(owner.Position.X, owner.Position.Y, baseHeight + height);

                if (height <= 0)
                {
                    GameServer.Singleton.Destroy(owner);
                    return StateResult.Completed;
                }
            }

            return StateResult.Active;
        }
    }
    #endregion

    #region StateAttack
    public class StateAttack : BaseState
    {
        /// <summary>
        /// State transitions
        /// </summary>
        enum StateType
        {
            MoveToTarget, MoveToPosition, Attack
        }

        StateType state;

        /// <summary>
        /// Common stuff
        /// </summary>
        Charactor owner;
        GameWorld world;
        GameObject target;
        Vector3? targetPosition;
        SpellCombat spell;
        StateMoveToPosition moveToPosition;
        StateMoveToTarget moveToTarget;
        double arbitrateTimer = 0;

        public StateAttack(GameWorld world, Charactor owner, GameObject target, SpellCombat spell)
        {
            if (world == null || owner == null || spell == null)
                throw new ArgumentNullException();

            this.owner = owner;
            this.target = target;
            this.world = world;
            this.spell = spell;
            this.state = StateType.MoveToTarget;

            if (this.target == null)
                this.target = FindAnotherTarget(world, owner, target, spell);

            if (this.target != null)
                this.targetPosition = this.target.Position;
        }

        public StateAttack(GameWorld world, Charactor owner, Vector3 target, SpellCombat spell)
        {
            if (world == null || owner == null || spell == null)
                throw new ArgumentNullException();

            this.owner = owner;
            this.world = world;
            this.spell= spell;
            this.targetPosition = target;
            this.state = StateType.MoveToPosition;
        }

        public override void Activate() { }

        public override void Terminate()
        {
            target = null;
            targetPosition = null;
            if (moveToPosition != null)
                moveToPosition.Terminate();
            moveToPosition = null;
            if (moveToTarget != null)
                moveToTarget.Terminate();
            moveToTarget = null;

            owner.Stop();

            state = StateType.MoveToPosition;
        }

        public override StateResult Update(GameTime gameTime)
        {
            if (state == StateType.MoveToPosition)
            {
                // Find a target if we don't have one
                if (!spell.CanAttakTarget(target))
                    target = FindAnotherTarget(world, owner, target, spell);

                if (targetPosition == null)
                    return StateResult.Completed;

                if (target != null)
                {
                    // Attack the target if we have one
                    state = StateType.MoveToTarget;
                    moveToTarget = null;
                    return StateResult.Active;
                }

                // Start moving to destination
                if (moveToPosition == null)
                {
                    moveToPosition = new StateMoveToPosition(
                                     new Vector2(targetPosition.Value.X,
                                                 targetPosition.Value.Y),
                                                 owner, owner.Priority, world.PathManager);
                }

                StateResult result = moveToPosition.Update(gameTime);

                if (result == StateResult.Failed)
                    return StateResult.Failed;

                if (result == StateResult.Completed)
                    return StateResult.Completed;
            }
            else if (state == StateType.MoveToTarget)
            {
                // Checks if we have a target and the target can be attacked
                if (!spell.CanAttakTarget(target))
                {
                    state = StateType.MoveToPosition;
                    return StateResult.Active;
                }

                // Start moving if we're stopped
                if (moveToTarget == null)
                {
                    moveToTarget = new StateMoveToTarget(owner, target,
                                                         owner.Priority, world.PathManager);
                    moveToTarget.FollowDistance = 0;
                }

                // Update move
                StateResult result = moveToTarget.Update(gameTime);

                // Return to position if we cannot get there
                if (result == StateResult.Failed)
                {
                    state = StateType.MoveToPosition;
                    return StateResult.Active;
                }

                if (spell.TargetWithinRange(target))
                {
                    // Stop moving and press the attack
                    spell.Cast(target);
                    moveToTarget = null;
                    state = StateType.Attack;
                }
                else if (result == StateResult.Completed)
                {
                    // If we completed moving to the target but failed to
                    // reach it, reactivate the whole process
                    state = StateType.MoveToPosition;
                    moveToTarget = null;
                }
            }
            else if (state == StateType.Attack)
            {
                // Let spell handle all those attack stuff.
                // We only care about it the target is been hunt down.
                if (!spell.CanAttakTarget(target) || !spell.TargetWithinRange(target))
                {
                    target = null;
                    moveToPosition = null;
                    state = StateType.MoveToPosition;
                }

                // Cast the spell whenever we can. The spell will handle those
                // cool down, aimation stuff.
                spell.Cast(target);


                const double ArbitrateInterval = 1.0;

                arbitrateTimer += gameTime.ElapsedGameTime.TotalSeconds;

                // Checks to see if there are any other units if we're attacking a building
                if (arbitrateTimer >= ArbitrateInterval && target is Building)
                {
                    FindAnotherTarget(world, owner, null, spell);
                    arbitrateTimer = 0;
                }
            }

            //if (target != null)
            //    BaseGame.Singleton.Graphics2D.DrawLine(target.Position, owner.Position, Color.Wheat);
            //if (targetPosition.HasValue)
            //    BaseGame.Singleton.Graphics2D.DrawLine(owner.Position, targetPosition.Value, Color.Green);

            return StateResult.Active;
        }

        public static GameObject FindAnotherTarget(
            GameWorld world, GameObject owner, GameObject existingTarget, SpellCombat spell)
        {
            GameObject minTarget = null;
            float distanceSq = float.MaxValue;

            foreach (IWorldObject o in world.GetNearbyObjects(owner.Position, owner.ViewDistance))
            {
                GameObject anotherTarget = o as GameObject;

                if (anotherTarget == existingTarget || anotherTarget == owner ||
                    !owner.IsOpponent(anotherTarget) || !spell.CanAttakTarget(anotherTarget))
                    continue;

                float lenSq = Vector3.Subtract(anotherTarget.Position, owner.Position).LengthSquared();
                if (lenSq < distanceSq)
                {
                    // Attack object that can attack us
                    if (anotherTarget.AttackPoint.X > 0 ||
                       (anotherTarget.AttackPoint.X <= 0 && minTarget == null))
                    {
                        minTarget = anotherTarget;
                        distanceSq = lenSq;
                    }
                }
            }

            return minTarget;
        }
    }
    #endregion
}
