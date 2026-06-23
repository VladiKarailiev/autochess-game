using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutoChess
{
    public class MainMenu : MonoBehaviour
    {
        public string gameSceneName = "SampleScene";
        public Button playButton;
        public Button howToButton;
        public Button quitButton;
        public Button closeHowToButton;
        public GameObject howToPlayPanel;

        void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (howToButton != null) howToButton.onClick.AddListener(ToggleHowToPlay);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (closeHowToButton != null) closeHowToButton.onClick.AddListener(CloseHowToPlay);
            if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        }

        public void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        public void ToggleHowToPlay()
        {
            if (howToPlayPanel != null)
                howToPlayPanel.SetActive(!howToPlayPanel.activeSelf);
        }

        public void CloseHowToPlay()
        {
            if (howToPlayPanel != null)
                howToPlayPanel.SetActive(false);
        }

        public void Quit()
        {
            Application.Quit();
        }
    }
}
