// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Isles;

public class Level : IEventListener
{
    public virtual void Load(LevelModel model, ILoading progress)
    {
        var playerInfoFromMap = new List<PlayerInfo>();

        foreach (var spawnPoint in model.SpawnPoints)
        {
            var info = new PlayerInfo
            {
                SpawnPoint = spawnPoint,
            };

            playerInfoFromMap.Add(info);
        }

        CreatePlayers(playerInfoFromMap);

        // Create actual player instances
        Player.LocalPlayer = null;
        foreach (var playerInfo in playerInfoFromMap)
        {
            Player player = playerInfo.Type switch
            {
                PlayerType.Local => new LocalPlayer(),
                PlayerType.Computer => new ComputerPlayer(),
                _ => throw new InvalidOperationException(),
            };
            
            player.Name = playerInfo.Name;
            player.Team = playerInfo.Team;
            player.TeamColor = playerInfo.TeamColor;
            player.SpawnPoint = playerInfo.SpawnPoint;

            Player.AllPlayers.Add(player);
        }

        if (Player.LocalPlayer == null)
        {
            Player.LocalPlayer = new LocalPlayer();
        }
    }

    protected virtual void CreatePlayers(List<PlayerInfo> info) { }

    public virtual void Start(GameWorld world)
    {
        // Reset gameTime
        world.Game.ResetElapsedTime();

        // Initialize local player
        foreach (var player in Player.AllPlayers)
        {
            player.Start(world);
        }

        foreach (var o in world.WorldObjects)
        {
            if (o is GameObject)
            {
                (o as GameObject).Start(world);
            }
        }

        Audios.PlayMusic("Islander", true, 40, 10);
    }

    public virtual void Update(GameTime gameTime) { }

    public virtual EventResult HandleEvent(EventType type, object sender, object tag)
    {
        return EventResult.Unhandled;
    }
}

/// <summary>
/// Respresents a level in the game.
/// </summary>
public class Skirmish : Level
{
    private readonly GameScreen screen;
    private readonly List<PlayerInfo> playerInfos;

    /// <summary>
    /// Create a new stone.
    /// </summary>
    public Skirmish(GameScreen screen, IEnumerable<PlayerInfo> info)
    {
        if (screen == null || info == null)
        {
            throw new ArgumentNullException();
        }

        this.screen = screen;
        playerInfos = new List<PlayerInfo>(info);
    }

    /// <summary>
    /// Load a game level.
    /// </summary>
    protected override void CreatePlayers(List<PlayerInfo> playerInfoFromMap)
    {
        if (playerInfos.Count > playerInfoFromMap.Count)
        {
            throw new Exception("This map only support " + playerInfoFromMap.Count + " players");
        }

        // Randomize player position
        var random = new Random();

        for (var i = 0; i < playerInfos.Count; i++)
        {
            var index = random.Next(playerInfoFromMap.Count);
            // int index = i;
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

        world.Flush();

        base.Start(world);

        // Select peons
        Player.LocalPlayer.SelectMultiple(
            Player.LocalPlayer.EnumerateObjects(
                Player.LocalPlayer.WorkerName), false);

        // Create box of pandora
        for (var i = 0; i < 12; i++)
        {
            var box = world.Create("BoxOfPandora");

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

        world.Flush();
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

    private void CreateStartup(GameWorld world, Player player, Vector3 position)
    {
        // Create a townhall and 4 followers if this player is not used for quest
        var building = world.Create("Townhall") as Building;

        if (building != null)
        {
            building.Position = position;
            building.Owner = player;
            world.Add(building);
        }

        for (var i = 0; i < 5; i++)
        {
            if (world.Create("Follower") is Worker peon)
            {
                peon.Position = building.Position + building.SpawnPoint;
                peon.Owner = player;
                world.Add(peon);

                position.X += 4;
            }
        }

        player.Gold = 500;
        player.Lumber = 500;
    }

    private double checkTimer = 5;

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
                    {
                        screen.ShowDefeated();
                    }
                    else
                    {
                        screen.ShowVictory();
                    }

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
            {
                return false;
            }
        }

        return true;
    }
}
