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
        // Native deserialization restores IO positions after DataFromJson. Normalize legacy
        // 40-unit spacing only after that step, keeping saved pipe/lane identities intact.
















        // ClearIO normally writes directly into adjacent buffers, bypassing InputItem.
        // Keep native deletion/recipe-change behavior from placing items inside Ledger.




        internal static void PrepareClear(FlowElement element)
        {
         foreach(var input in element.Inputs)
         {
          var origin=input.ConnectedPipe?.ConnectedOutput;
          if(!(origin?.Parent is Ledger))continue;
          var seen=new HashSet<Ledger>();
          while(origin?.Parent is Ledger previous)
          {
           if(!seen.Add(previous)){origin=null;break;}
           origin=previous.Inputs[origin.ParentIndex].ConnectedPipe?.ConnectedOutput;
          }
          // Return queued arrivals to the real producer, never a Ledger output.
          if(origin!=null)for(int i=input.BufferedItems.Count-1;i>=0;i--)origin.BufferedItems.Insert(0,input.BufferedItems[i]);
          input.BufferedItems.Clear();
         }
         foreach(var output in element.Outputs)
         {
          var target=output.ConnectedPipe?.ConnectedInput;
          if(!(target?.Parent is Ledger))continue;
          while(output.HasBuffer&&target.CanAcceptItem(output.AvailableItem)&&target.InputItem(output.AvailableItem))output.ConsumeItem();
         }
        }
    }
}
