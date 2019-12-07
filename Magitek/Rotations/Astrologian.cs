﻿using System.Linq;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.Astrologian;
using Magitek.Models.Astrologian;
using Magitek.Utilities;

namespace Magitek.Rotations
{
    public static class Astrologian
    {
        public static async Task<bool> Rest()
            {
                return false;
            }

        public static async Task<bool> PreCombatBuff()
        {
            if (WorldManager.InSanctuary)
                return false;

            if (Core.Me.IsMounted)
                return false;

            if (Core.Me.IsCasting)
                return true;

            await Casting.CheckForSuccessfulCast();
            
            if (Globals.OnPvpMap)
                return false;

            if (ActionResourceManager.Astrologian.Arcana == ActionResourceManager.Astrologian.AstrologianCard.None)
                await Spells.Draw.Cast(Core.Me);

            return await Buff.Sect();
        }

        public static async Task<bool> Pull()
        {
            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                {
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20);
                }
            }
            else
            {
                if (Globals.InParty)
                {
                    if (!AstrologianSettings.Instance.DoDamage)
                        return false;
                }
            }

            if (Core.Me.InCombat)
                return false;

            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            if (WorldManager.InSanctuary)
                return false;

            if (Core.Me.IsMounted)
                return false;

            if (await Casting.TrackSpellCast()) return true;
            await Casting.CheckForSuccessfulCast();

            Casting.DoHealthChecks = false;

            if (await GambitLogic.Gambit()) return true;
            
            if (Globals.PartyInCombat && Globals.InParty)
            {
                if (await TankBusters.Execute()) return true;
            }

            if (await Logic.Astrologian.Heal.Ascend()) return true;
            if (await Logic.Astrologian.Heal.EssentialDignity()) return true;
            if (await Dispel.Execute()) return true;
            if (await Buff.LucidDreaming()) return true;
            if (await Buff.Lightspeed()) return true;
            if (await Buff.Synastry()) return true;
            if (await Buff.NeutralSect()) return true;

            if (Globals.InActiveDuty || Core.Me.InCombat)
            {
                if (Globals.InParty)
                {
                    if (await Logic.Astrologian.Heal.EssentialDignity()) return true;
                    if (await Logic.Astrologian.Heal.CelestialIntersection()) return true;
                    if (await Logic.Astrologian.Heal.CelestialOpposition()) return true;
                    if (await Logic.Astrologian.Heal.Horoscope()) return true;
                    if (await Logic.Astrologian.Heal.HoroscopePop()) return true;
                    if (await Logic.Astrologian.Heal.AspectedHelios()) return true;
                    if (await Logic.Astrologian.Heal.CollectiveUnconscious()) return true;
                    if (await Logic.Astrologian.Heal.Helios()) return true;
                    if (await Logic.Astrologian.Heal.Benefic2()) return true;
                }

                if (await Logic.Astrologian.Heal.Benefic2()) return true;
                if (await Logic.Astrologian.Heal.AspectedBenefic()) return true;
                if (await Logic.Astrologian.Heal.EarthlyStar()) return true;
            }

            return await Logic.Astrologian.Heal.Benefic();
        }

        public static async Task<bool> CombatBuff()
        {
            return await Cards.PlayCards();
        }

        public static async Task<bool> Combat()
        {
            if (Globals.InParty)
            {
                if (!AstrologianSettings.Instance.DoDamage)
                    return true;

                if (Core.Me.CurrentManaPercent < AstrologianSettings.Instance.MinimumManaPercentToDoDamage && Core.Target.CombatTimeLeft() > AstrologianSettings.Instance.DoDamageIfTimeLeftLessThan)
                    return true;
            }

            if (!GameSettingsManager.FaceTargetOnAction && !Core.Me.CurrentTarget.InView()) return false;

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                {
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 20);
                }
            }

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (Globals.OnPvpMap)
            {
                if (await Pvp.Disable()) return true; //Damage
                return await Pvp.Malefic(); //Damage
            }

            if (await Aoe.Gravity()) return true;
            if (await SingleTarget.Combust()) return true;
            if (await SingleTarget.CombustMultipleTargets()) return true;
            return await SingleTarget.Malefic();
        }

        public static async Task<bool> PvP()
        {
            if (Globals.OnPvpMap)
            {
                if (await Pvp.EssentialDignity()) return true; //Heal
                if (await Pvp.Purify()) return true; //Dispel/Heal
                if (await Pvp.Muse()) return true; //Self-Buff
                if (await Pvp.Lightspeed()) return true; //CombatBuff
                if (await Pvp.Synastry()) return true; //Heal
                if (await Pvp.Deorbit()) return true; //Heal
                if (await Pvp.EmpyreanRain()) return true; //Heal
                if (await Pvp.Recuperate()) return true; //Self-Heal
                if (await Pvp.Benefic2()) return true; //Heal
                if (await Pvp.Benefic()) return true; //Heal
                if (await Pvp.Concentrate()) return true; //CombatBuff/Heal
                return await Pvp.Safeguard();
            }

            return false;
        }
    }
}
