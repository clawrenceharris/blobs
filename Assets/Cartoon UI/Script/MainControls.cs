using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Controls
{
    public class MainControls : MonoBehaviour
    {
        public GameObject Store;
        public GameObject Ranking;
        public GameObject Mail;
        public GameObject Settings;
        public GameObject Lose;
        public GameObject Victory;
        public GameObject Mission;
        public GameObject DailyRewards;
        public GameObject SelectLevel;


        public Button StoreBtn;
        public Button RankingBtn;
        public Button DailyRewardsBtn;
        public Button MailBtn;
        public Button SettingsBtn;
        public Button MissionBtn;
        public Button SelectLevelBtn;


        public void Start()
        {
            StoreBtn.onClick.AddListener(OpenStore);
            RankingBtn.onClick.AddListener(OpenRanking);
            DailyRewardsBtn.onClick.AddListener(OpenDailyRewards);
            MailBtn.onClick.AddListener(OpenMail);
            SettingsBtn.onClick.AddListener(OpenSettings);
            MissionBtn.onClick.AddListener(OpenMission);
            SelectLevelBtn.onClick.AddListener(OpenLevelSelect);
        }

        void OpenStore()
        {
            Store.SetActive(true);
        }
        void OpenMail()
        {
            Mail.SetActive(true);
        }
        void OpenDailyRewards()
        {
            DailyRewards.SetActive(true);
        }
        void OpenRanking()
        {
            Ranking.SetActive(true);
        }
        void OpenSettings()
        {
            Settings.SetActive(true);
        }
        void OpenMission()
        {
            Mission.SetActive(true);
        }

        void OpenLevelSelect()
        {
            SelectLevel.SetActive(true);
        }
    }
}
