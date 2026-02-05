using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Store
{
    public class StoreScript : MonoBehaviour
    {
        public Button Gems;
        public Button Coins;
        public GameObject Gems_On;
        public GameObject Gems_Off;
        public GameObject Coins_On;
        public GameObject Coins_Off;
        [Space(10)]

        public GameObject GemView;
        public GameObject CoinView;

        public void Start()
        {
            Gems_On.SetActive(true);
            Gems_Off.SetActive(false);
            Coins_Off.SetActive(true);
            Coins_On.SetActive(false);
            Gems.onClick.AddListener(gemsClick);
            Coins.onClick.AddListener(coinsClick);
            GemView.SetActive(true);
            CoinView.SetActive(false);
        }

        void gemsClick()
        {
            Switch("gems");
        }

        void coinsClick()
        {
            Switch("coins");
        }
        public void Switch(string name)
        {
            if (name == "gems")
            {
                Gems_On.SetActive(true);
                Gems_Off.SetActive(false);
                Coins_Off.SetActive(true);
                Coins_On.SetActive(false);
                GemView.SetActive(true);
                CoinView.SetActive(false);
            }
            else
            {
                Gems_On.SetActive(false);
                Gems_Off.SetActive(true);
                Coins_Off.SetActive(false);
                Coins_On.SetActive(true);
                GemView.SetActive(false);
                CoinView.SetActive(true);
            }
        }

    }
}
