using System;

namespace GhettosFirearmSDKv2;

public class EnumDescriptionAttribute : Attribute
{
    public EnumDescriptionAttribute(string name)
    {
        Name = name;
    }

    public string Name;
}