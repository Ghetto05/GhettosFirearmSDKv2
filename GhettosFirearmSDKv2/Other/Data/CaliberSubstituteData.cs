using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class CaliberSubstituteData : CustomData
    {
        private static List<Tuple<string,string>> _substitutes;

        public static List<Tuple<string,string>> Substitutes
        {
            get
            {
                if (_substitutes == null)
                    _substitutes = Catalog.GetData<CaliberSubstituteData>("GhettosFirearmSDKv2CaliberSubstituteData").substitutes.ToList();
                return _substitutes.ToList();
            }
        }

        public List<Tuple<string, string>> substitutes;

        public static bool AllowSubstitution(string caliberInQuestion, string targetCaliber)
        {
            return Substitutes.Any(x => x.Item1.Equals(caliberInQuestion) && x.Item2.Equals(targetCaliber));
        }
    }
}