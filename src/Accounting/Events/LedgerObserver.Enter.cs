using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Assets.Source.Flow;
using Assets.Source.Flow.Elements;
using Assets.Source.Items;
using Assets.Source.Player;
using Assets.Source.Util;
using HarmonyLib;

namespace KingdomLedger
{
    internal static partial class LedgerObserver
    {
        static void Enter(FlowElement __instance,MethodBase __originalMethod,object[] __args,out Scope __state)
        {
         __state=null;
         try
         {
          if(GamePlayer.Current==null)return;
          if(!ReferenceEquals(player,GamePlayer.Current))Reset();
          string method=__originalMethod.Name;
          if(Routers.Contains(__instance.ElementType)||(current?.Element==__instance&&method!="QueueCraft"))return;
          __state=new Scope{Parent=current,Element=__instance,Method=method,
           Outputs=new HashSet<InventoryItem>(__instance.Outputs.SelectMany(o=>o.BufferedItems)),
           Input=(__instance as RecipeElement)?.Input,Human=(__instance as Outfitter)?.CurrentHuman,Equipment=(__instance as Outfitter)?.CurrentEquip,
           Queued=QueueCount(__instance),Delta=method=="Update"&&__args.Length>0&&__args[0] is float delta?delta:0};
          CaptureIncome(__state);
          current=__state;
         }
         catch(Exception e){LedgerPlugin.Warn("Observer start: "+e.Message);}
        }
    }
}
