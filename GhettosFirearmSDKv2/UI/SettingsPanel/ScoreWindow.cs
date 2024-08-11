using UnityEngine;
using UnityEngine.UI;

namespace GhettosFirearmSDKv2.UI
{
    public class ScoreWindow : MonoBehaviour
    {
        public Text shotsFired;
        public Text hits;
        public Text headshots;
        public Text accuracy;

        private void Update()
        {
            shotsFired.text = FirearmsScore.local.ShotsFired.ToString();
            hits.text = FirearmsScore.local.ShotsHit.ToString();
            headshots.text = FirearmsScore.local.Headshots.ToString();
            accuracy.text = FirearmsScore.local.CalculateAccuracy() + "%";
        }
    }
}