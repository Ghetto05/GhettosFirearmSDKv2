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
            shotsFired.text = Settings_LevelModule.localScore.shotsFired.ToString();
            hits.text = Settings_LevelModule.localScore.shotsHit.ToString();
            headshots.text = Settings_LevelModule.localScore.headshots.ToString();
            accuracy.text = Settings_LevelModule.localScore.CalculateAccuracy() + "%";
        }
    }
}