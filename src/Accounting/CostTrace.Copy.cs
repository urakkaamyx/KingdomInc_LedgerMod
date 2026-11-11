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
        internal CostTrace Copy(decimal scale=1){var result=new CostTrace();result.Merge(this,scale);return result;}
    }
}
