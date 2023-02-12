using System.Collections;
using UnityEngine;
using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class Score_LevelModule : LevelModule
    {
        public int shotsFired;
        public int shotsHit;
        public int headshots;

        public float CalculateAccuracy()
        {
            return (shotsHit / shotsFired) * 100f;
        }
    }
}