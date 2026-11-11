using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Behaviour.Flow;
using Assets.Behaviour.Flow.Elements;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;
using Behaviour.UI.Tooltip;
using BepInEx;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using KingdomFoundation.Api;

namespace KingdomLedger
{
    public sealed partial class LedgerPlugin:BaseUnityPlugin
    {
        void Awake()
        {
         instance=this;
         try
         {
          context=FoundationApi.ForMod(Id);
          RegisterFoundation();
          harmony=new Harmony(Id);harmony.PatchAll(Assembly.GetExecutingAssembly());
          LedgerObserver.Install(harmony);
         }
         catch{context?.Dispose();harmony?.UnpatchSelf();throw;}
        }
    }
}
