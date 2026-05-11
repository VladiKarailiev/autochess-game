using UnityEngine;

namespace AutoChess
{
    public class Inspector : MonoBehaviour
    {
        Unit current;

        public Unit Current => current;
        public bool HasUnit => current != null;

        public void Set(Unit u) { current = u; }
        public void Clear()     { current = null; }

        public void Toggle(Unit u)
        {
            current = (current == u) ? null : u;
        }
    }
}
