using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using SharpDX;

namespace DH.Orianna
{
    public static class BallManager
    {
        public static Vector3 BallPosition { get; private set; }
        private static int _sTick = Variables.GameTimeTickCount;

        static BallManager()
        {
            Game.OnUpdate += Game_OnGameUpdate;
            AIHeroClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
            BallPosition = ObjectManager.Player.Position;
        }

        static void AIBaseClientProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                var objs = GameObjects.AllGameObjects.Where(x => x.Name =="Orianna_Base_Z_ball_glow_green");
                switch (args.SData.Name)
                {
                    case "OrianaIzunaCommand":
                        BallPosition = args.To;
                        _sTick = Variables.GameTimeTickCount;
                        break;

                    case "OrianaRedactCommand":
                        BallPosition = Vector3.Zero;
                        _sTick = Variables.GameTimeTickCount;
                    break;
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Variables.GameTimeTickCount - _sTick > 300 && ObjectManager.Player.HasBuff("orianaghostself"))
            {
                BallPosition = ObjectManager.Player.Position;
            }

            foreach (var ally in GameObjects.AllyHeroes)
            {
                if (ally.HasBuff("orianaghost"))
                {
                    BallPosition = ally.Position;
                }
            }
        }
    }
}
