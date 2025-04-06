using ThunderRoad;
using UnityEngine;

namespace GhettosFirearmSDKv2;

public class FirearmsSettingsValues
{
    public static ModOptionBool[] spawnBooleanButton =
    {
        new("Spawn (0)", false),
        new("Spawn (1)", true)
    };

    public static ModOptionFloat[] triggerDisciplineTimers =
    {
        new("0.1s", 0.1f),
        new("0.5s", 0.5f),
        new("1s", 1),
        new("2s", 2),
        new("3s", 3),
        new("5s", 5),
        new("7.5s", 7.5f),
        new("10s", 10),
        new("30s", 30),
        new("Never", float.MaxValue)
    };

    public static ModOptionFloat[] firearmDespawnTimeValues =
    {
        new("Disabled", 0.0f),
        new("0.01 Seconds", 0.01f),
        new("5 Seconds", 5),
        new("10 Seconds", 10),
        new("20 Seconds", 20),
        new("30 Seconds", 30),
        new("40 Seconds", 40),
        new("50 Seconds", 50),
        new("1 Minute", 60),
        new("2 Minutes", 120),
        new("3 Minutes", 180),
        new("4 Minutes", 240),
        new("5 Minutes", 300),
        new("10 Minutes", 600)
    };

    public static ModOptionFloat[] incapacitateOnTorsoShotTimers =
    {
        new("Disabled", 0.0f),
        new("10 Seconds", 10),
        new("20 Seconds", 20),
        new("30 Seconds", 30),
        new("40 Seconds", 40),
        new("50 Seconds", 50),
        new("1 Minute", 60),
        new("2 Minutes", 120),
        new("3 Minutes", 180),
        new("4 Minutes", 240),
        new("5 Minutes", 300),
        new("10 Minutes", 600),
        new("Permanent", -1f),
        new("1 Day", 1440),
        new("1 Week", 10080),
        new("1 Year", 525600),
        new("1 Decade", 5256000),
        new("1 Century", 52560000),
        new("1 Millenia", 525600000),
        new("Till Battle State Games adds the Colt M16A4 to EFT", Mathf.Infinity),
        new("Till our LORD AND SAVIOUR, JESUS CHIRST arrives", 3610558080)
    };

    public static ModOptionFloat[] possibleNvgOffsets =
    {
        new("-50mm", -0.05f),
        new("-47.5mm", -0.0475f),
        new("-45mm", -0.045f),
        new("-42.5mm", -0.0425f),
        new("-40mm", -0.04f),
        new("-37.5mm", -0.0375f),
        new("-35mm", -0.035f),
        new("-32.5mm", -0.0325f),
        new("-30mm", -0.03f),
        new("-27.5mm", -0.0275f),
        new("-25mm", -0.025f),
        new("-22.5mm", -0.0225f),
        new("-20mm", -0.02f),
        new("-17.5mm", -0.0175f),
        new("-15mm", -0.015f),
        new("-12.5mm", -0.0125f),
        new("-10mm", -0.01f),
        new("-7.5mm", -0.0075f),
        new("-5mm", -0.005f),
        new("-2.5mm", -00025f),
        new("+/-0mm", 0f),
        new("+2.5mm", 0.0025f),
        new("+5mm", 0.005f),
        new("+7.5mm", 0.0075f),
        new("+10mm", 0.01f),
        new("+12.5mm", 0.0125f),
        new("+15mm", 0.015f),
        new("+17.5mm", 0.0175f),
        new("+20mm", 0.02f),
        new("+22.5mm", 0.0225f),
        new("+25mm", 0.025f),
        new("+27.5mm", 0.0275f),
        new("+30mm", 0.03f),
        new("+32.5mm", 0.0325f),
        new("+35mm", 0.035f),
        new("+37.5mm", 0.0375f),
        new("+40mm", 0.04f),
        new("+42.5mm", 0.0425f),
        new("+45mm", 0.045f),
        new("+47.5mm", 0.0475f),
        new("+50mm", 0.05f)
    };

    public static ModOptionFloat[] waterSplashRange =
    {
        new("Disabled", 0.00f),
        new("1m", 1f),
        new("2m", 2f),
        new("5m", 5f),
        new("10m", 10f),
        new("20m", 20f),
        new("50m", 50f),
        new("100m", 100f),
        new("200m", 200f),
        new("500m", 500f),
        new("1000m", 1000f)
    };

    public static ModOptionFloat[] waterSplashPrecision =
    {
        new("2m", 2f),
        new("1m", 1f),
        new("50cm", 0.5f),
        new("20cm", 0.2f),
        new("15cm", 0.15f),
        new("10cm", 0.1f),
        new("5cm", 0.05f),
        new("2cm", 0.02f),
        new("1cm", 0.01f)
    };

    public static ModOptionFloat[] malfunctionModes =
    {
        new("Disabled", 0f),
        new("Realistic", 1f),
        new("Arcade", 10f),
        new("Gruesome", 33f),
        new("Nightmare", 100f),
        new("Defective", 10000f)
    };
}