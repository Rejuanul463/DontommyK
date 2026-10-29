using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    [Header("Splash Screen")] 
    [SerializeField] private Image OvertimeImage;
    [SerializeField] private Button playButton;
    [SerializeField] private GameObject mainMenu;

    [Header("Main Menu")] 
    [SerializeField] private Button startDay;
    [SerializeField] private Button journal;
    [SerializeField] private Button stats;
    [SerializeField] private Button settings;

    [SerializeField] private GameObject mainMenuPannel;
    [SerializeField] private GameObject statPannel;
    [SerializeField] private GameObject settingPannel;
    [SerializeField] private GameObject journalPannel;
    [SerializeField] private GameObject IngamePannel;

    [SerializeField]
    private GameObject character;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        registerButtons();
    }

    public void playButtonOnSplashScreen()
    {
        OvertimeImage.gameObject.GetComponent<Animator>().Play("move");
        playButton.gameObject.GetComponent<Animator>().Play("move");
        
        StartCoroutine(ActiveDeactiveItems(new GameObject[] {mainMenu}, 
                                                new GameObject[] {playButton.gameObject}, 
                                                        0.5f));
    }

    private IEnumerator ActiveDeactiveItems(GameObject[] activeItem, GameObject[] deactiveItem, float delay = 0)
    {
        yield return new WaitForSeconds(delay);
        foreach (GameObject item in activeItem) item.SetActive(true);
        foreach(GameObject item in deactiveItem) item.SetActive(false);
    }


    public void registerButtons()
    {
        playButton.onClick.AddListener(playButtonOnSplashScreen);
        
        settings.onClick.AddListener(() =>
        {
            settingPannel.SetActive(true);
            mainMenuPannel.SetActive(false);
        });
        
        journal.onClick.AddListener(() => 
        {
            if (Align360.Align360App.OpenPage("journal")) return;
            journalPannel.SetActive(true);
            mainMenuPannel.SetActive(false);
        });
        stats.onClick.AddListener(() => 
        {
            if (Align360.Align360App.OpenPage("stats")) return;
            statPannel.SetActive(true);
            mainMenuPannel.SetActive(false);
        });
        
        startDay.onClick.AddListener(() =>
        {
            character.SetActive(true);
            mainMenuPannel.SetActive(false);
            IngamePannel.SetActive(true);
            Align360.Align360App.OpenPage("scenarios");
        });
        
    }

    public void closeAll()
    {
        character.SetActive(false);
        mainMenuPannel.SetActive(true);
        settingPannel.SetActive(false);
        statPannel.SetActive(false);
        journalPannel.SetActive(false);
        IngamePannel.SetActive(false);
    }
}
