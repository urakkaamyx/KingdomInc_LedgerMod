using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Source.Flow;
using Assets.Source.Items;
using Assets.Source.Player;

namespace KingdomLedger
{
    internal sealed partial class CostTrace
    {
        internal void Merge(CostTrace t,decimal scale=1)
        {
         if(t==null){Complete=false;return;} Complete &=t.Complete;
         foreach(var p in t.Money)Add(Money,p.Key,p.Value*scale);
         foreach(var p in t.Rewards)Add(Rewards,p.Key,p.Value*scale);
         foreach(var p in t.Resources)Add(Resources,p.Key,p.Value*scale);
         foreach(var p in t.Stages)Add(Stages,p.Key,p.Value*scale);
         foreach(var stage in t.StageMoney){if(!StageMoney.TryGetValue(stage.Key,out var values))StageMoney[stage.Key]=values=new Dictionary<string,decimal>();foreach(var p in stage.Value)Add(values,p.Key,p.Value*scale);}
        }
    }
}
