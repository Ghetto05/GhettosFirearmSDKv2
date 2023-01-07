using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class Score_LevelModule : LevelModule
    {
        public float accuracy;
        public int shotsFired;
        public int shotsHit;
        public int headshots;

        public void CalculateAccuracy()
        {
            accuracy = (shotsHit / shotsFired) * 100f;
        }
    }
}