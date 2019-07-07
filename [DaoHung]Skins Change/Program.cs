using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skins_Change
{
    class Program
    {

        private static Menu _menu;


        #region

        private static readonly MenuSlider skinsMenu = new MenuSlider("Skins", "nothing", 0, 0, 20);

        #endregion


        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoad;
        }
        public static void OnLoad()
        {
            //ObjectManager.Player.SetSkin(9);
            CreateMenu();
            Game.OnUpdate += OnTick;
        }

        private static void CreateMenu()
        {
            _menu = new Menu("skinschange", "[DaoHung] SkinsChange", true);

            var _skins = new Menu("skinschange", "Skin change");
            _skins.Add(skinsMenu);

            _menu.Add(_skins);
            _menu.Attach();
        }

        public static void OnTick(EventArgs args)
        {
            ObjectManager.Player.SetSkin(skinsMenu.Value);
            skinsMenu.DisplayName = ObjectManager.Player.CharacterData.SkinName;
        }
        
    }
}
