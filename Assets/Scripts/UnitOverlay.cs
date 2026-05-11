using UnityEngine;
using UnityEngine.UI;

namespace AutoChess
{
    public class UnitOverlay : MonoBehaviour
    {
        public GameObject hpBarRoot;
        public Image      hpBarFill;
        public Image[]    tierDots = new Image[3];
    }
}
