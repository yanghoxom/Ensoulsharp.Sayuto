using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using static EnsoulSharp.SDK.Geometry;
using Utility = EnsoulSharp.SDK.Utility;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;

namespace DaoHungAIO.Champions
{
    class Sion
    {
        private AIHeroClient player = ObjectManager.Player;
        private GameObject unitUlti;
        public Sion()
        {
            Game.OnUpdate += OnUpdate;
        }


        private void OnUpdate(EventArgs args)
        {
            Spell Q = new Spell(SpellSlot.Q, 10);
            Q.SetTargetted(1, float.MaxValue);

            var point = TacticalMap.WorldToMinimap(Game.CursorPos);
            Render.Circle.DrawCircle(point.ToVector3World(), 200, Color.Red, 10);
            var go = new GameObject();
            player.IssueOrder(GameObjectOrder.AttackUnit, go as AIHeroClient);
            MiniMap.DrawCircle(point.ToVector3World(), 10, Color.Green);
            if (player.HasBuff("SionR"))
            {
                //unitUlti = GameObjects.AllGameObjects.Where(o => o.DistanceToPlayer() > Game.CursorPos.DistanceToPlayer() && o.IsVisibleOnScreen && !o.IsDead && o is AIBaseClient).OrderBy(o => o.DistanceToCursor()).First();
                //Game.Print(unitUlti.Name);
                //var point = TacticalMap.WorldToMinimap(Game.CursorPos);
                Render.Circle.DrawCircle(point.ToVector3(), 200, Color.Red, 10);
                //Q.Cast(Game.CursorPos);
                //Game.SendEmote(EmoteId.Dance);
                //Game.SendMasteryBadge();
                //Game.SendSummonerEmote(SummonerEmoteSlot.Victory, true);
                //Game.

                ;
                player.IssueOrder(GameObjectOrder.AttackUnit, GameObjects.AllGameObjects.Where(o => o.DistanceToCursor() < 10).OrderBy(o => o.DistanceToCursor()).First(), true);

            }
        }
    }
}
