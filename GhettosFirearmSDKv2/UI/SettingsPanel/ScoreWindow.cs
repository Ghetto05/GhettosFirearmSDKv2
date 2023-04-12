using System.Collections;
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
            shotsFired.text = FirearmsScore.local.shotsFired.ToString();
            hits.text = FirearmsScore.local.shotsHit.ToString();
            headshots.text = FirearmsScore.local.headshots.ToString();
            accuracy.text = FirearmsScore.local.CalculateAccuracy() + "%";
        }
    }
}