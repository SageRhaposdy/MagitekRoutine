﻿using System;
using System.Linq;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;

namespace Magitek.Utilities.Routines
{
    internal static class Ninja
    {
        public static int AoeEnemies5Yards;
        public static int AoeEnemies8Yards;
        public static int TCJState = 0;
        public static bool OnGcd => Spells.SpinningEdge.Cooldown > TimeSpan.FromMilliseconds(100);
        public static bool CanCastNinjutsu => ActionManager.CanCast(Spells.Ninjutsu, null);

        public static void RefreshVars()
        {
            if (!Core.Me.InCombat || !Core.Me.HasTarget)
                return;

            AoeEnemies5Yards = Core.Me.CurrentTarget.EnemiesNearby(5).Count();
            AoeEnemies8Yards = Core.Me.CurrentTarget.EnemiesNearby(8).Count();
        }
    }
}
