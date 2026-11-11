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
        internal static string Stage(FlowElement element)
        {
         if(element is RecipeElement r&&r.Recipe!=null)return element.ElementType+" / "+Translation.Translate(r.Recipe.DisplayName);
         if(element is ResourceElement s&&s.Resource!=null)return element.ElementType+" / "+Translation.Translate(s.Resource.PreviewItem.DisplayName);
         if(element is Forest f&&f.SelectedResource!=null)return "Forest / "+Translation.Translate(f.SelectedResource.PreviewItem.DisplayName);
         if(IsIncomeReceiver(element))return element.ElementType+" / "+Translation.Translate(element.Label);
         return element.ElementType;
        }
    }
}
