using ThunderRoad;

namespace GhettosFirearmSDKv2
{
    public class FirearmsScore : ThunderScript
    {
        public static FirearmsScore local;

        public int shotsFired;
        public int shotsHit;
        public int headshots;

        public override void ScriptEnable()
        {
            local = this;
        }

        public float CalculateAccuracy()
        {
            return (float)shotsHit / (float)shotsFired * 100f;
        }
    }
}