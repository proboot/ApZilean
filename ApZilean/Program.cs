using System;
using System.Collections.Generic;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK.Events;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using SharpDX;
using EloBuddy.SDK.Rendering;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Spells;
using System.Threading;
using System.Threading.Tasks;

namespace ApZilean
{
    class Program
    {
        private static AIHeroClient Zilean = Player.Instance;
        private static Menu ApZileanMenu, ComboMenu, MiscMenu, DrawingsMenu;
        public static Spell.Skillshot Q;
        public static Spell.Active W;
        public static Spell.Targeted E;
        public static Spell.Targeted R;

        public static SpellSlot Flash;

        private static List<Spell.SpellBase> SpellList = new List<Spell.SpellBase>();

        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;
            Game.OnEnd += Game_OnEnd;
        }

        #region trash
        private static void Game_OnEnd(GameEndEventArgs args)
        {
            Environment.Exit(0);
        }

        public static bool HasQZileanBuff(Obj_AI_Base target)
        {
            if (target.IsEnemy)
            {
                return target.HasBuff("ZileanQEnemyBomb");
            }
            else
            {
                return target.HasBuff("ZileanQAllyBomb");
            }
        }

        #endregion

        private static void Loading_OnLoadingComplete(EventArgs args)
        {
            if (Zilean.ChampionName != "Zilean")
            {
                Chat.Print(Zilean.ChampionName + " is not supported", System.Drawing.Color.WhiteSmoke);
                return;
            }
            else Chat.Print("Good luck and have fun with Ap Zilean", System.Drawing.Color.WhiteSmoke);

            if (Zilean.GetSpellSlotFromName("summonerflash") == SpellSlot.Summoner1)
                Flash = SpellSlot.Summoner1;
            else if (Zilean.GetSpellSlotFromName("summonerflash") == SpellSlot.Summoner2)
                Flash = SpellSlot.Summoner2;
            else Flash = SpellSlot.Unknown;

            InitializeOrDie();
        }

        private static void InitializeOrDie()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 880, SkillShotType.Circular) { AllowedCollisionCount = int.MaxValue };
            W = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 750);
            R = new Spell.Targeted(SpellSlot.R, 880);

            SpellList.Add(Q);
            SpellList.Add(E);
            SpellList.Add(R);

            ApZileanMenu = MainMenu.AddMenu("ApZilean", "ApZilean");

            ComboMenu = ApZileanMenu.AddSubMenu("Combo");
            ComboMenu.Add("Q", new Label("Q used by default"));
            ComboMenu.Add("W", new Label("W used by default"));
            ComboMenu.Add("E", new CheckBox("Use E"));
            ComboMenu.Add("focusby", new EloBuddy.SDK.Menu.Values.Slider("Focus by AttackDamage(0) or MagicalDamage(1)", 0, 0, 1));
            //ComboMenu.Add("R", new CheckBox("Use R"));
            //ComboMenu.Add("gunblade", new CheckBox("Use Hextech Gunblade"));

            MiscMenu = ApZileanMenu.AddSubMenu("Misc");
            MiscMenu.Add("autoUlt", new Slider("AutoUlt if n% hp (0 = off)", defaultValue: 20, minValue: 0, maxValue: 100));
            MiscMenu.Add("autoUltAllies", new CheckBox("AutoUlt Allies"));
            MiscMenu.Add("autoq", new Slider("Auto2Q (E + Q + W + Q) if n enemies", 7, 3, 0, 5));
            MiscMenu.Add("antigapcloser", new CheckBox("AntiGapCloser with E"));
            MiscMenu.Add("bombtomouse", new KeyBind("DoubleBomb to mouse", false, KeyBind.BindTypes.HoldActive, 'Z'));
            MiscMenu.Add("blockr", new CheckBox("Block Flash when zilean has ult on yourself"));

            DrawingsMenu = ApZileanMenu.AddSubMenu("Drawings");
            DrawingsMenu.Add("enabled", new CheckBox("Enabled"));

            foreach (var spell in SpellList)
            {
                DrawingsMenu.Add(spell.Slot.ToString(), new CheckBox("Draw " + spell.Slot));
            }

            DrawingsMenu.Add("damage", new CheckBox("Damage indicator"));

            Gapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
        }

        //flash blocking
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            var blockR = MiscMenu["blockr"].Cast<CheckBox>().CurrentValue;

            if (blockR)
            {
                if (Flash != SpellSlot.Unknown)
                {
                    if (args.Slot == Flash && Zilean.HasUndyingBuff())
                    {
                        Chat.Print("Flash blocked", System.Drawing.Color.Chartreuse);
                        args.Process = false;
                    }
                }
            }

        }

        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                ComboVombo();
            }

            misc();
        }

        #region dont look this shit plz :`(

        private async static void ComboVombo()
        {
            try
            {
                var ise = ComboMenu["E"].Cast<CheckBox>().CurrentValue;
                var focusby = ComboMenu["focusby"].Cast<Slider>().CurrentValue;

                var Enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget() && Q.IsInRange(x)).OrderBy(x => focusby == 0 ? x.TotalAttackDamage : x.TotalMagicalDamage);

                if (Enemies.Count() == 0) return;

                var enemy = Enemies.Last();

                //full combo
                if (Q.IsReady() && W.IsReady() && E.IsReady())
                {
                    if (E.IsInRange(enemy) && ise)
                    {
                        Vector3 pos;
                        E.Cast(enemy);
                        Q.Cast(pos = Q.GetPrediction(enemy).CastPosition);
                        await Task.Delay(350); //q delay fix
                        W.Cast();
                        Q.Cast(pos);
                    }
                    else
                    {
                        E.Cast(Zilean);
                        Q.Cast(Q.GetPrediction(enemy).CastPosition);
                        await Task.Delay(350); //q delay fix
                        W.Cast();
                        Q.Cast(Q.GetPrediction(enemy).CastPosition);
                    }
                }

                // Combo without E
                else if (Q.IsReady() && W.IsReady())
                {
                    Q.Cast(Q.GetPrediction(enemy).CastPosition);
                    W.Cast();
                    Q.Cast(Q.GetPrediction(enemy).CastPosition);
                }

                //Combo without Q, but with W

                else if (!Q.IsReady() && W.IsReady())
                {
                    W.Cast();
                    Q.Cast(Q.GetPrediction(enemy).CastPosition);
                }

                // lazy combo
                else if (Q.IsReady() && !W.IsReady())
                {
                    Q.Cast(Q.GetPrediction(enemy).CastPosition);
                }

            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        #endregion

        private static void misc()
        {
            try
            {
                var ise = ComboMenu["E"].Cast<CheckBox>().CurrentValue;
                var autoUltValue = MiscMenu["autoUlt"].Cast<Slider>().CurrentValue;
                var doubleQValue = MiscMenu["bombtomouse"].Cast<KeyBind>().CurrentValue;
                var autoqValue = MiscMenu["autoq"].Cast<Slider>().CurrentValue;

                if (doubleQValue && Q.IsReady() && W.IsReady())
                {
                    var pos = Game.CursorPos;
                    if (Q.IsInRange(pos))
                    {
                        Q.Cast(pos);
                        W.Cast();
                        Q.Cast(pos);
                    }
                }

                if (autoUltValue != 0 && R.IsReady() && R.ManaCost <= Zilean.Mana)
                {
                    if (Zilean.HealthPercent <= autoUltValue)
                    {
                        if (Zilean.CountEnemyChampionsInRange(880) >= 1)
                            R.Cast(Zilean);
                    }
                    if (MiscMenu["autoUltAllies"].Cast<CheckBox>().CurrentValue)
                    {
                        var Allies = EntityManager.Heroes.Allies.Where(x => !x.IsDead && x.IsValidTarget() && x.HealthPercent <= autoUltValue && R.IsInRange(x)).OrderByDescending(x => x.Health);
                        foreach (var t in Allies)
                        {
                            //Кто успел тот и съел, но себя люблю больше - translate it xD
                            if (t.CountEnemyChampionsInRange(880) >= 1)
                            {
                                if (t.HealthPercent <= autoUltValue)
                                {
                                    if (R.IsReady())
                                    {
                                        R.Cast(t);
                                    }
                                    else
                                    {
                                        if (ise && E.IsReady() && E.IsInRange(t)) E.Cast(t);
                                    }
                                }
                            }
                        }
                    }
                }

                if (ise && autoUltValue != 0 && !Zilean.HasUndyingBuff() && !R.IsReady() && Zilean.HealthPercent <= autoUltValue)
                {
                    if (Zilean.CountEnemyChampionsInRange(880) >= 1)
                        E.Cast(Zilean);
                }

                if (!Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
                {
                    if (autoqValue != 0 && (Q.ManaCost + W.ManaCost + Q.ManaCost <= Zilean.Mana))
                    {
                        var Enemies = EntityManager.Heroes.Enemies.Where(x => !x.IsDead && x.IsValidTarget() && Q.IsInRange(x)).OrderByDescending(x => x.Health);
                        if (Enemies.Count() >= autoqValue)
                        {
                            var enemy = Enemies.Last();
                            var QPrediction = Q.GetPrediction(enemy);

                            if (Q.IsReady() && W.IsReady()) Q.Cast(QPrediction.CastPosition);
                            else if (Q.IsReady() && !W.IsReady() && HasQZileanBuff(enemy)) Q.Cast(QPrediction.CastPosition);
                            else if (!Q.IsReady() && W.IsReady() && HasQZileanBuff(enemy)) W.Cast();
                        }
                    }
                }

            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserEventArgs e)
        {
            if (sender.IsEnemy && e.End.Distance(ObjectManager.Player.Position) < 501 && !sender.IsDead && E.IsReady() && MiscMenu["antigapcloser"].Cast<CheckBox>().CurrentValue)
            {
                E.Cast(sender);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (!DrawingsMenu["enabled"].Cast<CheckBox>().CurrentValue) return;
            foreach (var Spell in SpellList.Where(spell => DrawingsMenu[spell.Slot.ToString()].Cast<CheckBox>().CurrentValue))
            {
                Circle.Draw(Spell.IsReady() ? Color.Chartreuse : Color.OrangeRed, Spell.Range, Zilean);
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            if (DrawingsMenu["enabled"].Cast<CheckBox>().CurrentValue && DrawingsMenu["damage"].Cast<CheckBox>().CurrentValue)
            {
                foreach (var unit in EntityManager.Heroes.Enemies.Where(u => u.IsValidTarget() && u.IsHPBarRendered))
                {
                    var damage = DamageIndicator.Damagefromspell(unit);

                    if (damage <= 0)
                    {
                        continue;
                    }
                    var Special_X = unit.ChampionName == "Jhin" || unit.ChampionName == "Annie" ? -12 : 0;
                    var Special_Y = unit.ChampionName == "Jhin" || unit.ChampionName == "Annie" ? -3 : 9;

                    var DamagePercent = ((unit.TotalShieldHealth() - damage) > 0
                        ? (unit.TotalShieldHealth() - damage)
                        : 0) / (unit.MaxHealth + unit.AllShield + unit.AttackShield + unit.MagicShield);
                    var currentHealthPercent = unit.TotalShieldHealth() / (unit.MaxHealth + unit.AllShield + unit.AttackShield + unit.MagicShield);

                    var StartPoint = new Vector2((int)(unit.HPBarPosition.X + Special_X + DamagePercent * 107) + 1,
                        (int)unit.HPBarPosition.Y + Special_Y);
                    var EndPoint = new Vector2((int)(unit.HPBarPosition.X + Special_X + currentHealthPercent * 107) + 1,
                        (int)unit.HPBarPosition.Y + Special_Y);
                    var Color = System.Drawing.Color.DarkOliveGreen;
                    Drawing.DrawLine(StartPoint, EndPoint, 9.82f, Color);
                }
            }
        }
    }
}
