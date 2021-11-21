// Copyright (c) Yufei Huang. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Isles;

public class GameIsles : BaseGame
{
    protected override void FirstTimeInitialize()
    {
        Register();

        if (!string.IsNullOrEmpty(EnvironmentVariables.StartupLevel))
        {
            StartScreen(new GameScreen(EnvironmentVariables.StartupLevel));
        }
        else
        {
            StartScreen(new TitleScreen());
        }
    }

    private void Register()
    {
        // Register world object creators
        GameWorld.RegisterCreator("Decoration", world => new Decoration(world));
        GameWorld.RegisterCreator("Tree", world => new Tree(world));
        GameWorld.RegisterCreator("Goldmine", world => new Goldmine(world));
        GameWorld.RegisterCreator("BoxOfPandora", world => new BoxOfPandora(world));

        // Islander architectures
        GameWorld.RegisterCreator("Townhall", world => new Building(world, "Townhall"));
        GameWorld.RegisterCreator("Farmhouse", world => new Building(world, "Farmhouse"));
        GameWorld.RegisterCreator("Lumbermill", world => new Building(world, "Lumbermill"));
        GameWorld.RegisterCreator("Tower", world => new Tower(world, "Tower"));
        GameWorld.RegisterCreator("Barracks", world => new Building(world, "Barracks"));
        GameWorld.RegisterCreator("Altar", world => new Building(world, "Altar"));

        Spell.RegisterCreator("Townhall", world => new SpellConstruct(world, "Townhall"));
        Spell.RegisterCreator("Farmhouse", world => new SpellConstruct(world, "Farmhouse"));
        Spell.RegisterCreator("Lumbermill", world => new SpellConstruct(world, "Lumbermill"));
        Spell.RegisterCreator("Tower", world => new SpellConstruct(world, "Tower"));
        Spell.RegisterCreator("Barracks", world => new SpellConstruct(world, "Barracks"));
        Spell.RegisterCreator("Altar", world => new SpellConstruct(world, "Altar"));

        // Islander units
        GameWorld.RegisterCreator("Follower", world => new Worker(world, "Follower"));
        GameWorld.RegisterCreator("Militia", world => new Charactor(world, "Militia"));
        GameWorld.RegisterCreator("Hunter", world => new Hunter(world, "Hunter"));
        GameWorld.RegisterCreator("FireSorceress", world => new FireSorceress(world, "FireSorceress"));
        GameWorld.RegisterCreator("Hellfire", world => new Hellfire(world, "Hellfire"));

        Spell.RegisterCreator("Follower", world => new SpellTraining(world, "Follower", null));
        Spell.RegisterCreator("Militia", world => new SpellTraining(world, "Militia", null));
        Spell.RegisterCreator("Hunter", world => new SpellTraining(world, "Hunter", null));
        Spell.RegisterCreator("FireSorceress", world => new SpellTraining(world, "FireSorceress", null));
        Spell.RegisterCreator("Hellfire", world => new SpellTraining(world, "Hellfire", null));

        // Register spells
        Spell.RegisterCreator("LiveOfNature", world => new SpellUpgrade(world, "LiveOfNature", Upgrades.LiveOfNature));
        Spell.RegisterCreator("PunishOfNatureUpgrade", world => new SpellUpgrade(world, "PunishOfNatureUpgrade", Upgrades.PunishOfNature));
        Spell.RegisterCreator("AttackUpgrade", world => new SpellUpgrade(world, "AttackUpgrade", Upgrades.Attack));
        Spell.RegisterCreator("DefenseUpgrade", world => new SpellUpgrade(world, "DefenseUpgrade", Upgrades.Defense));
        Spell.RegisterCreator("PunishOfNature", world => new SpellPunishOfNature(world));
        Spell.RegisterCreator("SummonHellfire", world => new SpellSummon(world, "Hellfire"));
    }
}
