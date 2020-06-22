using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using Keys = System.Windows.Forms.Keys;
using SPrediction;
using DaoHungAIO.Helpers;
using EnsoulSharp.SDK.Events;

namespace DaoHungAIO.Plugins
{
    class Developer
    {

        private static Menu Config;
        private static Menu Types;
        private static int _lastUpdateTick = 0;
        private static int _lastMovementTick = 0;
        public Developer()
        {
                InitMenu();
            if (Config.Item("enable").GetValue<MenuBool>()) {
                Tick.OnTick += OnUpdate;
                Drawing.OnDraw += OnDraw;
                AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            }
            
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                Game.Print("Detected Spell Name: " + args.SData.Name + " Issued By: " + sender.CharacterName);
            }
        }

        private static void InitMenu()
        {
            Config = new Menu("developersharp", "Developer# (imsosharp)", true);
            Config.Add(new MenuBool("enable", "Enable(need reload)", false));
            Config.Add(new MenuSlider("range", "Max object dist from cursor").SetValue(new Slider(400, 100, 1000)));
            Config.Attach();
        }

        private static void OnUpdate(EventArgs args)
        {
            if (Environment.TickCount - _lastMovementTick > 140000)
            {
                ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo,
                    ObjectManager.Player.Position.Extend(ObjectManager.Player.Position, 1000));
                _lastMovementTick = Environment.TickCount;
            }
        }

        private static void OnDraw(EventArgs args)
        {
            foreach (var obj in ObjectManager.Get<GameObject>().Where(o => o.Position.Distance(Game.CursorPos) < Config.Item("range").GetValue<MenuSlider>().Value && !(o is Obj_Turret) && o.Name != "missile" && !(o is GrassObject) && !(o is DrawFX) && !(o is LevelPropSpawnerPoint) && !(o is EffectEmitter) && !o.Name.Contains("MoveTo")))
            {
                if (!obj.IsValid<GameObject>()) return;
                var X = Drawing.WorldToScreen(obj.Position).X;
                var Y = Drawing.WorldToScreen(obj.Position).Y;
                Drawing.DrawText(X, Y, Color.DarkTurquoise, (obj is AIHeroClient) ? ((AIHeroClient)obj).CharacterName : (obj is AIMinionClient) ? (obj as AIMinionClient).CharacterName : (obj is AITurretClient) ? (obj as AITurretClient).CharacterName : obj.Name);
                Drawing.DrawText(X, Y + 10, Color.DarkTurquoise, obj.Type.ToString());
                Drawing.DrawText(X, Y + 20, Color.DarkTurquoise, "NetworkID: " + obj.NetworkId);
                Drawing.DrawText(X, Y + 30, Color.DarkTurquoise, obj.Position.ToString());
                if (obj is AIBaseClient)
                {
                    var aiobj = obj as AIBaseClient;
                    Drawing.DrawText(X, Y + 40, Color.DarkTurquoise, "Health: " + aiobj.Health + "/" + aiobj.MaxHealth + "(" + aiobj.HealthPercent + "%)");
                }
                if (obj is AIHeroClient)
                {
                    var hero = obj as AIHeroClient;
                    Drawing.DrawText(X, Y + 50, Color.DarkTurquoise, "Spells:");
                    Drawing.DrawText(X, Y + 60, Color.DarkTurquoise, "(Q): " + hero.Spellbook.Spells[0].Name);
                    Drawing.DrawText(X, Y + 70, Color.DarkTurquoise, "(W): " + hero.Spellbook.Spells[1].Name);
                    Drawing.DrawText(X, Y + 80, Color.DarkTurquoise, "(E): " + hero.Spellbook.Spells[2].Name);
                    Drawing.DrawText(X, Y + 90, Color.DarkTurquoise, "(R): " + hero.Spellbook.Spells[3].Name);
                    Drawing.DrawText(X, Y + 100, Color.DarkTurquoise, "(D): " + hero.Spellbook.Spells[4].Name);
                    Drawing.DrawText(X, Y + 110, Color.DarkTurquoise, "(F): " + hero.Spellbook.Spells[5].Name);
                    var buffs = hero.Buffs;
                    if (buffs.Any())
                    {
                        Drawing.DrawText(X, Y + 120, Color.DarkTurquoise, "Buffs:");
                    }
                    for (var i = 0; i < buffs.Count() * 10; i += 10)
                    {
                        Drawing.DrawText(X, (Y + 130 + i), Color.DarkTurquoise, buffs[i / 10].Count + "x " + buffs[i / 10].Name);
                    }

                }
                if (obj is MissileClient && obj.Name != "missile")
                {
                    var missile = obj as MissileClient;
                    Drawing.DrawText(X, Y + 40, Color.DarkTurquoise, "Missile Speed: " + missile.SData.MissileSpeed);
                    Drawing.DrawText(X, Y + 50, Color.DarkTurquoise, "Cast Range: " + missile.SData.CastRange);
                }
            }
        }
    }
}
