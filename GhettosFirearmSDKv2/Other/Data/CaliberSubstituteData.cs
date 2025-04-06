using System;
using System.Collections.Generic;
using System.Linq;
using ThunderRoad;

namespace GhettosFirearmSDKv2;

public class CaliberSubstituteData : CustomData
{
    private static List<Tuple<string, string>> _allSubstitutes;

    public static List<Tuple<string, string>> AllSubstitutes
    {
        get
        {
            if (_allSubstitutes is null)
            {
                _allSubstitutes = Catalog.GetData<CaliberSubstituteData>("GhettosFirearmSDKv2CaliberSubstituteData").Substitutes.ToList();
            }
            return _allSubstitutes.ToList();
        }
    }

    // ReSharper disable once CollectionNeverUpdated.Global - Loaded from json
    public List<Tuple<string, string>> Substitutes;

    public static bool AllowSubstitution(string caliberInQuestion, string targetCaliber)
    {
        return AllSubstitutes.Any(x => x.Item1.Equals(caliberInQuestion) && x.Item2.Equals(targetCaliber));
    }
}