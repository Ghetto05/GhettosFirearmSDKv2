using System;
using System.Collections.Generic;
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

        public static ModOptionFloat[] zeroToOneModifier = new[]
                                                           {
                                                               new ModOptionFloat("0.00", 0.00f), //0
                                                               new ModOptionFloat("0.05", 0.05f), //1
                                                               new ModOptionFloat("0.10", 0.10f), //2
                                                               new ModOptionFloat("0.15", 0.15f), //3
                                                               new ModOptionFloat("0.20", 0.20f), //4
                                                               new ModOptionFloat("0.25", 0.25f), //5
                                                               new ModOptionFloat("0.30", 0.30f), //6
                                                               new ModOptionFloat("0.35", 0.35f), //7
                                                               new ModOptionFloat("0.40", 0.40f), //8
                                                               new ModOptionFloat("0.45", 0.45f), //9
                                                               new ModOptionFloat("0.50", 0.50f), //10
                                                               new ModOptionFloat("0.55", 0.55f), //11
                                                               new ModOptionFloat("0.60", 0.60f), //12
                                                               new ModOptionFloat("0.65", 0.65f), //13
                                                               new ModOptionFloat("0.70", 0.70f), //14
                                                               new ModOptionFloat("0.75", 0.75f), //15
                                                               new ModOptionFloat("0.80", 0.80f), //16
                                                               new ModOptionFloat("0.85", 0.85f), //17
                                                               new ModOptionFloat("0.90", 0.90f), //18
                                                               new ModOptionFloat("0.95", 0.95f), //19
                                                               new ModOptionFloat("1.00", 1.00f) //20
                                                           };

        public static ModOptionFloat[] zeroToFiveModifier = new[]
                                                           {
                                                               new ModOptionFloat("0.00", 0.00f), //0
                                                               new ModOptionFloat("0.05", 0.05f), //1
                                                               new ModOptionFloat("0.10", 0.10f), //2
                                                               new ModOptionFloat("0.15", 0.15f), //3
                                                               new ModOptionFloat("0.20", 0.20f), //4
                                                               new ModOptionFloat("0.25", 0.25f), //5
                                                               new ModOptionFloat("0.30", 0.30f), //6
                                                               new ModOptionFloat("0.35", 0.35f), //7
                                                               new ModOptionFloat("0.40", 0.40f), //8
                                                               new ModOptionFloat("0.45", 0.45f), //9
                                                               new ModOptionFloat("0.50", 0.50f), //10
                                                               new ModOptionFloat("0.55", 0.55f), //11
                                                               new ModOptionFloat("0.60", 0.60f), //12
                                                               new ModOptionFloat("0.65", 0.65f), //13
                                                               new ModOptionFloat("0.70", 0.70f), //14
                                                               new ModOptionFloat("0.75", 0.75f), //15
                                                               new ModOptionFloat("0.80", 0.80f), //16
                                                               new ModOptionFloat("0.85", 0.85f), //17
                                                               new ModOptionFloat("0.90", 0.90f), //18
                                                               new ModOptionFloat("0.95", 0.95f), //19
                                                               new ModOptionFloat("1.00", 1.00f), //20
                                                               new ModOptionFloat("1.05", 1.05f), //21
                                                               new ModOptionFloat("1.10", 1.10f), //22
                                                               new ModOptionFloat("1.15", 1.15f), //23
                                                               new ModOptionFloat("1.20", 1.20f), //24
                                                               new ModOptionFloat("1.25", 1.25f), //25
                                                               new ModOptionFloat("1.30", 1.30f), //26
                                                               new ModOptionFloat("1.35", 1.35f), //27
                                                               new ModOptionFloat("1.40", 1.40f), //28
                                                               new ModOptionFloat("1.45", 1.45f), //29
                                                               new ModOptionFloat("1.50", 1.50f), //30
                                                               new ModOptionFloat("1.55", 1.55f), //31
                                                               new ModOptionFloat("1.60", 1.60f), //32
                                                               new ModOptionFloat("1.65", 1.65f), //33
                                                               new ModOptionFloat("1.70", 1.70f), //34
                                                               new ModOptionFloat("1.75", 1.75f), //35
                                                               new ModOptionFloat("1.80", 1.80f), //36
                                                               new ModOptionFloat("1.85", 1.85f), //37
                                                               new ModOptionFloat("1.90", 1.90f), //38
                                                               new ModOptionFloat("1.95", 1.95f), //39
                                                               new ModOptionFloat("2.00", 2.00f), //40
                                                               new ModOptionFloat("2.05", 2.05f), //41
                                                               new ModOptionFloat("2.10", 2.10f), //42
                                                               new ModOptionFloat("2.15", 2.15f), //43
                                                               new ModOptionFloat("2.20", 2.20f), //44
                                                               new ModOptionFloat("2.25", 2.25f), //45
                                                               new ModOptionFloat("2.30", 2.30f), //46
                                                               new ModOptionFloat("2.35", 2.35f), //47
                                                               new ModOptionFloat("2.40", 2.40f), //48
                                                               new ModOptionFloat("2.45", 2.45f), //49
                                                               new ModOptionFloat("2.50", 2.50f), //50
                                                               new ModOptionFloat("2.55", 2.55f), //51
                                                               new ModOptionFloat("2.60", 2.60f), //52
                                                               new ModOptionFloat("2.65", 2.65f), //53
                                                               new ModOptionFloat("2.70", 2.70f), //54
                                                               new ModOptionFloat("2.75", 2.75f), //55
                                                               new ModOptionFloat("2.80", 2.80f), //56
                                                               new ModOptionFloat("2.85", 2.85f), //57
                                                               new ModOptionFloat("2.90", 2.90f), //58
                                                               new ModOptionFloat("2.95", 2.95f), //59
                                                               new ModOptionFloat("3.00", 3.00f), //60
                                                               new ModOptionFloat("3.05", 3.05f), //61
                                                               new ModOptionFloat("3.10", 3.10f), //62
                                                               new ModOptionFloat("3.15", 3.15f), //63
                                                               new ModOptionFloat("3.20", 3.20f), //64
                                                               new ModOptionFloat("3.25", 3.25f), //65
                                                               new ModOptionFloat("3.30", 3.30f), //66
                                                               new ModOptionFloat("3.35", 3.35f), //67
                                                               new ModOptionFloat("3.40", 3.40f), //68
                                                               new ModOptionFloat("3.45", 3.45f), //69
                                                               new ModOptionFloat("3.50", 3.50f), //70
                                                               new ModOptionFloat("3.55", 3.55f), //71
                                                               new ModOptionFloat("3.60", 3.60f), //72
                                                               new ModOptionFloat("3.65", 3.65f), //73
                                                               new ModOptionFloat("3.70", 3.70f), //74
                                                               new ModOptionFloat("3.75", 3.75f), //75
                                                               new ModOptionFloat("3.80", 3.80f), //76
                                                               new ModOptionFloat("3.85", 3.85f), //77
                                                               new ModOptionFloat("3.90", 3.90f), //78
                                                               new ModOptionFloat("3.95", 3.95f), //79
                                                               new ModOptionFloat("4.00", 4.00f), //80
                                                               new ModOptionFloat("4.05", 4.05f), //81
                                                               new ModOptionFloat("4.10", 4.10f), //82
                                                               new ModOptionFloat("4.15", 4.15f), //83
                                                               new ModOptionFloat("4.20", 4.20f), //84
                                                               new ModOptionFloat("4.25", 4.25f), //85
                                                               new ModOptionFloat("4.30", 4.30f), //86
                                                               new ModOptionFloat("4.35", 4.35f), //87
                                                               new ModOptionFloat("4.40", 4.40f), //88
                                                               new ModOptionFloat("4.45", 4.45f), //89
                                                               new ModOptionFloat("4.50", 4.50f), //90
                                                               new ModOptionFloat("4.55", 4.55f), //91
                                                               new ModOptionFloat("4.60", 4.60f), //92
                                                               new ModOptionFloat("4.65", 4.65f), //93
                                                               new ModOptionFloat("4.70", 4.70f), //94
                                                               new ModOptionFloat("4.75", 4.75f), //95
                                                               new ModOptionFloat("4.80", 4.80f), //96
                                                               new ModOptionFloat("4.85", 4.85f), //97
                                                               new ModOptionFloat("4.90", 4.90f), //98
                                                               new ModOptionFloat("4.95", 4.95f), //99
                                                               new ModOptionFloat("5.00", 5.00f) //100
                                                           };
    }
}