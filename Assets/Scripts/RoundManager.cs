using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AutoChess
{
    public class RoundManager : MonoBehaviour
    {
        public PlayerEconomy economy;
        public Shop shop;
        public CombatManager combat;
        public ItemManager items;

        [Header("Round state")]
        [Min(1)] public int round = 1;

        [Header("Reward tuning")]
        public int baseReward = 5;
        public int winBonus = 1;

        [Header("Damage tuning (loss)")]
        public int baseLossDamage = 2;

        [Header("Pacing")]
        public float resultDelay = 2.5f;

        public GamePhase Phase { get; private set; } = GamePhase.Prep;

        public int  LastReward       { get; private set; }
        public int  LastBase         { get; private set; }
        public int  LastWin          { get; private set; }
        public int  LastKills        { get; private set; }
        public int  LastInterest     { get; private set; }
        public int  LastDamageTaken  { get; private set; }
        public bool LastWon          { get; private set; }

        void Start()
        {
            shop?.Roll();
            Phase = GamePhase.Prep;
        }

        public void StartBattle()
        {
            if (Phase != GamePhase.Prep) return;
            if (combat == null) return;
            Phase = GamePhase.Combat;
            combat.StartBattle(round);
        }

        public void OnBattleEnded(bool won, int kills, int survivorTierSum)
        {
            int interest = economy.Interest;
            int winB     = won ? winBonus : 0;
            int killB    = kills;
            int total    = baseReward + winB + killB + interest;
            economy.Gain(total);

            int dmg = 0;
            if (!won)
            {
                dmg = baseLossDamage + survivorTierSum;
                economy.TakeDamage(dmg);
                CameraShake.Trigger(0.4f);
            }

            LastBase        = baseReward;
            LastWin         = winB;
            LastKills       = kills;
            LastInterest    = interest;
            LastReward      = total;
            LastDamageTaken = dmg;
            LastWon         = won;

            if (won && items != null) items.RollReward();

            if (economy.IsDead)
            {
                Phase = GamePhase.GameOver;
                return;
            }

            Phase = GamePhase.Result;
            StartCoroutine(NextRoundAfterDelay(resultDelay));
        }

        IEnumerator NextRoundAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            round++;
            shop?.Roll();
            Phase = GamePhase.Prep;
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public string mainMenuScene = "MainMenu";

        public void LoadMainMenu()
        {
            SceneManager.LoadScene(mainMenuScene);
        }
    }
}
