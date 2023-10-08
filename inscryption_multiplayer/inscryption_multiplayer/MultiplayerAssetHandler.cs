using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using GBC;
using DiskCardGame;
using inscryption_multiplayer_proxies;

namespace inscryption_multiplayer
{
    public static class MultiplayerAssetHandler
    {
        private static readonly Dictionary<string, Type> InternalTypes = new()
        {
            { nameof(GenericUIButton), typeof(GenericUIButton) },
            { nameof(PixelText), typeof(PixelText) },
            { nameof(AnimatingSprite), typeof(AnimatingSprite) },
            { nameof(PixelSnapText), typeof(PixelSnapText) },
            { nameof(PixelSnapSprite), typeof(PixelSnapSprite) },
            { nameof(LocalizeFont), typeof(LocalizeFont) },
            { nameof(LocalizeUI), typeof(LocalizeUI) },
            { nameof(CursorTrailEffect), typeof(CursorTrailEffect) },
            { nameof(MenuCard), typeof(MenuCard) }
        };

        public static GameObject MultiplayerSettingsUI;
        public static GameObject MultiplayerMenuCard;
        public static GameObject MultiplayerErrorUI;
        public static Texture2D LifeCounterTexture;

        public static InscryptionMultiplayerMenuUI MultiplayerSettingsUIInstance;
        public static GameObject MultiplayerMenuCardInstance;
        public static GameObject MultiplayerErrorUIInstance;

        internal static void LoadBundle()
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(Plugin.Directory, "mod.bundle"));
            MultiplayerSettingsUI = bundle.LoadAsset<GameObject>("MultiplayerSettingsUI");
            MultiplayerMenuCard = bundle.LoadAsset<GameObject>("MenuCard_Online");
            MultiplayerErrorUI = bundle.LoadAsset<GameObject>("MultiplayerErrorUI");
            LifeCounterTexture = bundle.LoadAsset<Texture2D>("Life_Counter");
            bundle.Unload(false);
        }

        static MultiplayerAssetHandler()
        {
            InscryptionProxies.GetTypeFromName = name => InternalTypes[name];
        }
    }
}
