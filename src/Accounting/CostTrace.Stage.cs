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
        internal void Stage(string name,Dictionary<string,decimal> spend,decimal quantity=1)
        {
         Add(Stages,name,quantity);if(!StageMoney.TryGetValue(name,out var row))StageMoney[name]=row=new Dictionary<string,decimal>();
         foreach(var p in spend){Add(Money,p.Key,p.Value);Add(row,p.Key,p.Value);}
        }
    }
}
