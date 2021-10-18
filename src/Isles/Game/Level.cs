//-----------------------------------------------------------------------------
//  Isles v1.0
//  
//  Copyright 2008 (c) Nightin Games. All Rights Reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;
using Isles.Engine;
using Isles.Graphics;

namespace Isles
{
    #region Level
    public class Level : IEventListener
    {
        RuinedLand ruinedLand;
        List<PlayerInfo> playerInfoFromMap = new List<PlayerInfo>();

        public virtual void Load(XmlElement xml, ILoading progress)
        {
            // Read in player info from the map
            XmlNodeList childNodes = xml.SelectNodes("Player");

            foreach (XmlElement child in childNodes)
            {
                PlayerInfo info = new PlayerInfo();

                info.Name = child.GetAttribute("Name");
                if (child.HasAttribute("Team"))
                    info.Team = int.Parse(child.GetAttribute("Team"));
                if (child.HasAttribute("TeamColor"))
                    info.TeamColor = Helper.StringToColor(child.GetAttribute("TeamColor"));
                if (child.HasAttribute("Race"))
                    info.Race = (Race)Enum.Parse(typeof(Race), child.GetAttribute("Race"));
                if (child.HasAttribute("Type"))
                    info.Type = (PlayerType)Enum.Parse(typeof(PlayerType), child.GetAttribute("Type"));
                if (child.HasAttribute("SpawnPoint"))
                    info.SpawnPoint = Helper.StringToVector2(child.GetAttribute("SpawnPoint"));

                playerInfoFromMap.Add(info);
            
                // Remove those tag from xml since the world do not recognize them
                xml.RemoveChild(child);
            }            

            CreatePlayers(playerInfoFromMap);

            // Create actual player instances
            Player.LocalPlayer = null;
            Player player = null;

            for (int i = 0; i < playerInfoFromMap.Count; i++)
            {
                if (playerInfoFromMap[i].Type == PlayerType.Local)
                {
                    if (Player.LocalPlayer != null)
                        throw new Exception("Only one local player is allowed");

                    player = Player.LocalPlayer = new LocalPlayer();
                }
                else if (playerInfoFromMap[i].Type == PlayerType.Computer)
                {
                    player = new ComputerPlayer();
                }
                else if (playerInfoFromMap[i].Type == PlayerType.Dummy)
                {
                    player = new DummyPlayer();
                }
                else throw new NotImplementedException();

                player.Name = playerInfoFromMap[i].Name;
                player.Race = playerInfoFromMap[i].Race;
                player.Team = playerInfoFromMap[i].Team;
                player.TeamColor = playerInfoFromMap[i].TeamColor;
                player.SpawnPoint = playerInfoFromMap[i].SpawnPoint;

                Player.AllPlayers.Add(player);
            }

            // Make sure one local player is created
            if (Player.LocalPlayer == null)
            {
                Player.AllPlayers.Add(Player.LocalPlayer = new LocalPlayer());
            }
        }

        protected virtual void CreatePlayers(List<PlayerInfo> info) { }


        public virtual void Start(GameWorld world)
        {
            // Reset gameTime
            world.Game.ResetElapsedTime();

            // Initialize local player
            bool hasSteamer = false;
            foreach (Player player in Player.AllPlayers)
            {
                if (player.Race == Race.Steamer)
                    hasSteamer = true;

                player.Start(world);
            }

            foreach (IWorldObject o in world.WorldObjects)
            {
                if (o is GameObject)
                    (o as GameObject).Start(world);
            }

            // Create ruined land
            if (hasSteamer)
                ruinedLand = RuinedLand.Create(BaseGame.Singleton, world, world.FogOfWar);

            // Player sound
            if (Player.LocalPlayer.Race == Race.Islander)
                Audios.PlayMusic("Islander", true, 40, 10);
            else
                Audios.PlayMusic("Steamer", true, 40, 10);
        }

        public virtual void Update(GameTime gameTime) { }

        public virtual void Draw(GameTime gameTime)
        {
            if (ruinedLand != null)
                ruinedLand.Draw(gameTime);
        }

        public virtual EventResult HandleEvent(EventType type, object sender, object tag)
        {
            return EventResult.Unhandled;
        }
    }
    #endregion

    #region Skirmish
    /// <summary>
    /// Respresents a level in the game
    /// </summary>
    public class Skirmish : Level
    {
        GameScreen screen;
        List<PlayerInfo> playerInfos;

        /// <summary>
        /// Create a new stone
        /// </summary>
        public Skirmish(GameScreen screen, IEnumerable<PlayerInfo> info)
        {
            if (screen == null || info == null)
                throw new ArgumentNullException();

            this.screen = screen;
            playerInfos = new List<PlayerInfo>(info);
        }

        /// <summary>
        /// Load a game level
        /// </summary>
        protected override void CreatePlayers(List<PlayerInfo> playerInfoFromMap)
        {
            if (playerInfos.Count > playerInfoFromMap.Count)
                throw new Exception("This map only support " + playerInfoFromMap.Count + " players");

            // Randomize player position
            Random random = new Random();

            for (int i = 0; i < playerInfos.Count; i++)
            {
                int index = random.Next(playerInfoFromMap.Count);
                //int index = i;
                playerInfos[i].SpawnPoint = playerInfoFromMap[index].SpawnPoint;
                playerInfoFromMap.RemoveAt(index);
            }

            // Recreate player infos
            playerInfoFromMap.Clear();
            playerInfoFromMap.AddRange(playerInfos);
        }

        public override void Start(GameWorld world)
        {
            foreach (Player player in Player.AllPlayers)
            {
                // Create dependency
                CreateDependency(player);

                // Create startup objects
                CreateStartup(world, player, new Vector3(player.SpawnPoint, 0));   
            }

            base.Start(world);
            
            // Select peons
            Player.LocalPlayer.SelectMultiple(
                Player.LocalPlayer.EnumerateObjects(
                    Player.LocalPlayer.WorkerName), false);

            // Create box of pandora
            for (int i = 0; i < 12; i++)
            {
                IWorldObject box = GameServer.Singleton.Create("BoxOfPandora");

                Vector3 position;

                do
                {
                    position.X = Helper.RandomInRange(0, world.Landscape.Size.X);
                    position.Y = Helper.RandomInRange(0, world.Landscape.Size.Y);
                    position.Z = 0;
                }
                while (world.PathManager.Graph.IsPositionObstructed(position.X, position.Y, true));

                box.Position = position;

                world.Add(box);
            }
        }

        private void CreateDependency(Player player)
        {
            // Islander
            player.AddDependency("Farmhouse", "Townhall");
            player.AddDependency("Lumbermill", "Townhall");
            player.AddDependency("Tower", "Townhall");
            player.AddDependency("Barracks", "Townhall");
            player.AddDependency("Altar", "Townhall");
            player.AddDependency("Follower", "Townhall");
            player.AddDependency("Militia", "Townhall");
            player.AddDependency("Hunter", "Townhall");
            player.AddDependency("FireSorceress", "Townhall");
            player.AddDependency("Militia", "Barracks");
            player.AddDependency("Hunter", "Barracks");
            player.AddDependency("FireSorceress", "Altar");
            player.AddDependency("LiveOfNature", "Lumbermill");
            player.AddDependency("PunishOfNatureUpgrade", "Altar");
            player.AddDependency("AttackUpgrade", "Barracks");
            player.AddDependency("DefenseUpgrade", "Barracks");

            // Steamer
            player.AddDependency("Steamhouse", "SteamFort");
            player.AddDependency("Regenerator", "SteamFort");
            player.AddDependency("SteamCannon", "SteamFort");
            player.AddDependency("TraningCenter", "SteamFort");
            player.AddDependency("SteamFactory", "SteamFort");
            player.AddDependency("Miner", "SteamFort");
            player.AddDependency("Swordman", "SteamFort");
            player.AddDependency("Rifleman", "SteamFort");
            player.AddDependency("Steambot", "SteamFort");
            player.AddDependency("Swordman", "TraningCenter");
            player.AddDependency("Rifleman", "TraningCenter");
            player.AddDependency("Steambot", "SteamFactory");
            player.AddDependency("AttackUpgrade", "TraningCenter");
            player.AddDependency("DefenseUpgrade", "TraningCenter");
        }

        public void CreateStartup(GameWorld world, Player player, Vector3 position)
        {
            string townhall = (player.Race == Race.Islander ? "Townhall" : "SteamFort");
            string worker = (player.Race == Race.Islander ? "Follower" : "Miner");

            // Create a townhall and 4 followers if this player is not used for quest
            Building building = GameServer.Singleton.Create(townhall) as Building;

            if (building != null)
            {
                building.Position = position;
                building.Owner = player;
                building.Fall();
                world.Add(building);
            }

            for (int i = 0; i < 5; i++)
            {
                Worker peon = GameServer.Singleton.Create(worker) as Worker;

                if (peon != null)
                {
                    peon.Position = building.Position + building.SpawnPoint;
                    peon.Owner = player;
                    peon.Fall();
                    world.Add(peon);

                    position.X += 4;
                }
            }

            player.Gold = 500;
            player.Lumber = 500;
        }


        double checkTimer = 5;

        public override void Update(GameTime gameTime)
        {
            checkTimer -= gameTime.ElapsedGameTime.TotalSeconds;

            if (checkTimer < 0)
            {
                checkTimer = 5;

                // Check for victory/failure every 5 seconds
                foreach (Player player in Player.AllPlayers)
                {
                    if (IsPlayerDefeated(player))
                    {
                        if (player is LocalPlayer)
                            screen.ShowDefeated();
                        else
                            screen.ShowVictory();
                        break;
                    }
                }
            }
        }

        private static bool IsPlayerDefeated(Player player)
        {
            // Check for buildings
            foreach (GameObject o in player.EnumerateObjects())
            {
                if (o is Building && o.IsAlive)
                    return false;
            }

            return true;
        }
    }
    #endregion
}
