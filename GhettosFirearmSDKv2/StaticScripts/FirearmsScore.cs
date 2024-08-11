using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class FirearmsScore : ThunderScript
    {
        public static FirearmsScore local;

        public int ShotsFired;
        public int ShotsHit;
        public int Headshots;

        public override void ScriptEnable()
        {
            local = this;
        }

        public float CalculateAccuracy()
        {
            return ShotsHit / (float)ShotsFired * 100f;
        }
    }
}