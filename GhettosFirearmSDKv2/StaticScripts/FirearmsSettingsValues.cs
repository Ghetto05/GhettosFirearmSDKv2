using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class FirearmsSettingsValues
    {
        public static ModOptionFloat[] triggerDisciplineTimers = new [] 
                                                                 {
                                                                    new ModOptionFloat("0.1s", 0.1f),
                                                                    new ModOptionFloat("0.5s", 0.5f),
                                                                    new ModOptionFloat("1s", 1),
                                                                    new ModOptionFloat("2s", 2),
                                                                    new ModOptionFloat("3s", 3),
                                                                    new ModOptionFloat("5s", 5),
                                                                    new ModOptionFloat("7.5s", 7.5f),
                                                                    new ModOptionFloat("10s", 10),
                                                                    new ModOptionFloat("30s", 30),
                                                                    new ModOptionFloat("Never", float.MaxValue)
                                                                };
        
        public static ModOptionFloat[] firearmDespawnTimeValues = new [] 
                                                                  {
                                                                     new ModOptionFloat("Disabled", 0.0f),
                                                                     new ModOptionFloat("0.01 Seconds", 0.01f),
                                                                     new ModOptionFloat("5 Seconds", 5),
                                                                     new ModOptionFloat("10 Seconds", 10),
                                                                     new ModOptionFloat("20 Seconds", 20),
                                                                     new ModOptionFloat("30 Seconds", 30),
                                                                     new ModOptionFloat("40 Seconds", 40),
                                                                     new ModOptionFloat("50 Seconds", 50),
                                                                     new ModOptionFloat("1 Minute", 60),
                                                                     new ModOptionFloat("2 Minutes", 120),
                                                                     new ModOptionFloat("3 Minutes", 180),
                                                                     new ModOptionFloat("4 Minutes", 240),
                                                                     new ModOptionFloat("5 Minutes", 300),
                                                                     new ModOptionFloat("10 Minutes", 600)
                                                                 };
        
        public static ModOptionFloat[] incapacitateOnTorsoShotTimers = new [] 
                                              {
                                                 new ModOptionFloat("Disabled", 0.0f),
                                                 new ModOptionFloat("10 Seconds", 10),
                                                 new ModOptionFloat("20 Seconds", 20),
                                                 new ModOptionFloat("30 Seconds", 30),
                                                 new ModOptionFloat("40 Seconds", 40),
                                                 new ModOptionFloat("50 Seconds", 50),
                                                 new ModOptionFloat("1 Minute", 60),
                                                 new ModOptionFloat("2 Minutes", 120),
                                                 new ModOptionFloat("3 Minutes", 180),
                                                 new ModOptionFloat("4 Minutes", 240),
                                                 new ModOptionFloat("5 Minutes", 300),
                                                 new ModOptionFloat("10 Minutes", 600),
                                                 new ModOptionFloat("Permanent", -1f),
                                                 new ModOptionFloat("1 Day", 1440),
                                                 new ModOptionFloat("1 Week", 10080),
                                                 new ModOptionFloat("1 Year", 525600),
                                                 new ModOptionFloat("1 Decade", 5256000),
                                                 new ModOptionFloat("1 Century", 52560000),
                                                 new ModOptionFloat("1 Millenia", 525600000),
                                                 new ModOptionFloat("Till Battle State Games adds the Colt M16A4 to EFT", Mathf.Infinity),
                                                 new ModOptionFloat("Till our LORD AND SAVIOUR, JESUS CHIRST arrives", 3610558080)
                                             };
        
        public static ModOptionFloat[] possibleNvgOffsets = new [] 
                                                            {
                                                                   new ModOptionFloat("-50mm", -0.05f),
                                                                   new ModOptionFloat("-47.5mm", -0.0475f),
                                                                   new ModOptionFloat("-45mm", -0.045f),
                                                                   new ModOptionFloat("-42.5mm", -0.0425f),
                                                                   new ModOptionFloat("-40mm", -0.04f),
                                                                   new ModOptionFloat("-37.5mm", -0.0375f),
                                                                   new ModOptionFloat("-35mm", -0.035f),
                                                                   new ModOptionFloat("-32.5mm", -0.0325f),
                                                                   new ModOptionFloat("-30mm", -0.03f),
                                                                   new ModOptionFloat("-27.5mm", -0.0275f),
                                                                   new ModOptionFloat("-25mm", -0.025f),
                                                                   new ModOptionFloat("-22.5mm", -0.0225f),
                                                                   new ModOptionFloat("-20mm", -0.02f),
                                                                   new ModOptionFloat("-17.5mm", -0.0175f),
                                                                   new ModOptionFloat("-15mm", -0.015f),
                                                                   new ModOptionFloat("-12.5mm", -0.0125f),
                                                                   new ModOptionFloat("-10mm", -0.01f),
                                                                   new ModOptionFloat("-7.5mm", -0.0075f),
                                                                   new ModOptionFloat("-5mm", -0.005f),
                                                                   new ModOptionFloat("-2.5mm", -00025f),
                                                                   new ModOptionFloat("+/-0mm", 0f),
                                                                   new ModOptionFloat("+2.5mm", 0.0025f),
                                                                   new ModOptionFloat("+5mm", 0.005f),
                                                                   new ModOptionFloat("+7.5mm", 0.0075f),
                                                                   new ModOptionFloat("+10mm", 0.01f),
                                                                   new ModOptionFloat("+12.5mm", 0.0125f),
                                                                   new ModOptionFloat("+15mm", 0.015f),
                                                                   new ModOptionFloat("+17.5mm", 0.0175f),
                                                                   new ModOptionFloat("+20mm", 0.02f),
                                                                   new ModOptionFloat("+22.5mm", 0.0225f),
                                                                   new ModOptionFloat("+25mm", 0.025f),
                                                                   new ModOptionFloat("+27.5mm", 0.0275f),
                                                                   new ModOptionFloat("+30mm", 0.03f),
                                                                   new ModOptionFloat("+32.5mm", 0.0325f),
                                                                   new ModOptionFloat("+35mm", 0.035f),
                                                                   new ModOptionFloat("+37.5mm", 0.0375f),
                                                                   new ModOptionFloat("+40mm", 0.04f),
                                                                   new ModOptionFloat("+42.5mm", 0.0425f),
                                                                   new ModOptionFloat("+45mm", 0.045f),
                                                                   new ModOptionFloat("+47.5mm", 0.0475f),
                                                                   new ModOptionFloat("+50mm", 0.05f),
                                                               };

        public static ModOptionFloat[] waterSplashRange = new[]
                                                          {
                                                              new ModOptionFloat("Disabled", 0.00f),
                                                              new ModOptionFloat("1m", 1f),
                                                              new ModOptionFloat("2m", 2f),
                                                              new ModOptionFloat("5m", 5f),
                                                              new ModOptionFloat("10m", 10f),
                                                              new ModOptionFloat("20m", 20f),
                                                              new ModOptionFloat("50m", 50f),
                                                              new ModOptionFloat("100m", 100f),
                                                              new ModOptionFloat("200m", 200f),
                                                              new ModOptionFloat("500m", 500f),
                                                              new ModOptionFloat("1000m", 1000f)
                                                          };

        public static ModOptionFloat[] waterSplashPrecision = new[]
                                                           {
                                                               new ModOptionFloat("2m", 2f),
                                                               new ModOptionFloat("1m", 1f),
                                                               new ModOptionFloat("50cm", 0.5f),
                                                               new ModOptionFloat("20cm", 0.2f),
                                                               new ModOptionFloat("15cm", 0.15f),
                                                               new ModOptionFloat("10cm", 0.1f),
                                                               new ModOptionFloat("5cm", 0.05f),
                                                               new ModOptionFloat("2cm", 0.02f),
                                                               new ModOptionFloat("1cm", 0.01f),
                                                           };
    }
}