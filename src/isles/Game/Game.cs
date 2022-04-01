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
        GameWorld.RegisterCreator("Tree", world => new Tree());
        GameWorld.RegisterCreator("Goldmine", world => new Goldmine());
        GameWorld.RegisterCreator("BoxOfPandora", world => new BoxOfPandora());

        // Islander architectures
        GameWorld.RegisterCreator("Townhall", world => new Building("Townhall"));
        GameWorld.RegisterCreator("Farmhouse", world => new Building("Farmhouse"));
        GameWorld.RegisterCreator("Lumbermill", world => new Building("Lumbermill"));
        GameWorld.RegisterCreator("Tower", world => new Tower("Tower"));
        GameWorld.RegisterCreator("Barracks", world => new Building("Barracks"));
        GameWorld.RegisterCreator("Altar", world => new Building("Altar"));

        Spell.RegisterCreator("Townhall", world => new SpellConstruct("Townhall"));
        Spell.RegisterCreator("Farmhouse", world => new SpellConstruct("Farmhouse"));
        Spell.RegisterCreator("Lumbermill", world => new SpellConstruct("Lumbermill"));
        Spell.RegisterCreator("Tower", world => new SpellConstruct("Tower"));
        Spell.RegisterCreator("Barracks", world => new SpellConstruct("Barracks"));
        Spell.RegisterCreator("Altar", world => new SpellConstruct("Altar"));

        // Islander units
        GameWorld.RegisterCreator("Follower", world => new Worker("Follower"));
        GameWorld.RegisterCreator("Militia", world => new Charactor("Militia"));
        GameWorld.RegisterCreator("Hunter", world => new Hunter("Hunter"));
        GameWorld.RegisterCreator("FireSorceress", world => new FireSorceress("FireSorceress"));
        GameWorld.RegisterCreator("Hellfire", world => new Hellfire("Hellfire"));

        Spell.RegisterCreator("Follower", world => new SpellTraining("Follower"));
        Spell.RegisterCreator("Militia", world => new SpellTraining("Militia"));
        Spell.RegisterCreator("Hunter", world => new SpellTraining("Hunter"));
        Spell.RegisterCreator("FireSorceress", world => new SpellTraining("FireSorceress"));
        Spell.RegisterCreator("Hellfire", world => new SpellTraining("Hellfire"));

        // Register spells
        Spell.RegisterCreator("LiveOfNature", world => new SpellUpgrade("LiveOfNature", Upgrades.LiveOfNature));
        Spell.RegisterCreator("PunishOfNatureUpgrade", world => new SpellUpgrade("PunishOfNatureUpgrade", Upgrades.PunishOfNature));
        Spell.RegisterCreator("AttackUpgrade", world => new SpellUpgrade("AttackUpgrade", Upgrades.Attack));
        Spell.RegisterCreator("DefenseUpgrade", world => new SpellUpgrade("DefenseUpgrade", Upgrades.Defense));
        Spell.RegisterCreator("PunishOfNature", world => new SpellPunishOfNature());
        Spell.RegisterCreator("SummonHellfire", world => new SpellSummon("Hellfire"));
    }
}
