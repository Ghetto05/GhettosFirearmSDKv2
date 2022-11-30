using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GhettosFirearmSDKv2
{
    public class SwitchRelation : MonoBehaviour
    {
        public Transform switchObject;
        public bool usePositionsAsDifferentObjects = false;
        public List<Transform> modePositions;
    }
}
