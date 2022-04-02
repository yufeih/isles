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
        Spell.RegisterCreator("Townhall", world => new SpellConstruct("Townhall"));
        Spell.RegisterCreator("Farmhouse", world => new SpellConstruct("Farmhouse"));
        Spell.RegisterCreator("Lumbermill", world => new SpellConstruct("Lumbermill"));
        Spell.RegisterCreator("Tower", world => new SpellConstruct("Tower"));
        Spell.RegisterCreator("Barracks", world => new SpellConstruct("Barracks"));
        Spell.RegisterCreator("Altar", world => new SpellConstruct("Altar"));

        Spell.RegisterCreator("Follower", world => new SpellTraining("Follower"));
        Spell.RegisterCreator("Militia", world => new SpellTraining("Militia"));
        Spell.RegisterCreator("Hunter", world => new SpellTraining("Hunter"));
        Spell.RegisterCreator("FireSorceress", world => new SpellTraining("FireSorceress"));
        Spell.RegisterCreator("Hellfire", world => new SpellTraining("Hellfire"));

        Spell.RegisterCreator("LiveOfNature", world => new SpellUpgrade("LiveOfNature", Upgrades.LiveOfNature));
        Spell.RegisterCreator("PunishOfNatureUpgrade", world => new SpellUpgrade("PunishOfNatureUpgrade", Upgrades.PunishOfNature));
        Spell.RegisterCreator("AttackUpgrade", world => new SpellUpgrade("AttackUpgrade", Upgrades.Attack));
        Spell.RegisterCreator("DefenseUpgrade", world => new SpellUpgrade("DefenseUpgrade", Upgrades.Defense));
        Spell.RegisterCreator("PunishOfNature", world => new SpellPunishOfNature());
        Spell.RegisterCreator("SummonHellfire", world => new SpellSummon("Hellfire"));
    }
}
