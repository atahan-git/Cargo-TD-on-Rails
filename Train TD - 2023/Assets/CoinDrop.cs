using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinDrop : MonoBehaviour {
    private Vector2 randomDir;

    public GameObject[] coins;
    
    public void SetUp(Vector3 source, int _amount, bool isMiniCoin = false) {
        amount = _amount;
        GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(source);
        
        for (int i = 0; i < coins.Length; i++) {
            coins[i].SetActive(false);
        }
        //GetComponentInChildren<MoneyUIDisplay>().SetAmount(amount);
        if (!isMiniCoin) {
            StartCoroutine(SpawnMiniCoins(source, _amount));
        } else {
            switch (amount) {
                case 1:
                    coins[0].SetActive(true);
                    break;
                case 10:
                    coins[1].SetActive(true);
                    break;
                case 100:
                    coins[2].SetActive(true);
                    break;
                case 1000:
                    coins[3].SetActive(true);
                    break;
            }
        }

        randomDir = Random.insideUnitCircle;
    }

    IEnumerator SpawnMiniCoins(Vector3 source, int count) {
        while(count > 0) {
            var am = 1;
            if (count >= 1000) {
                am = 1000;
            }else if (count >= 100) {
                am = 100;
            }else if (count >= 10) {
                am = 10;
            }

            Instantiate(LevelReferences.s.coinDrop, LevelReferences.s.uiDisplayParent).GetComponent<CoinDrop>().SetUp(source /*+ Random.insideUnitSphere * 0.5f*/, am, true);

            count -= am;
            //yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        yield return null;
    }


    private float spreadTime = 0.5f;
    private int amount;
    private float speed = 7;
    void Update()
    {
        if (spreadTime > 0) {
            if (spreadTime > 0.25f) {
                speed = Mathf.Lerp(speed, 0, 2f * Time.deltaTime);
                var randomVec = randomDir  * Time.deltaTime * speed;
                transform.position += new Vector3(randomVec.x, randomVec.y);
            } else {
                speed = 0;
            }
            spreadTime -= Time.deltaTime;
        } else {
            transform.position = Vector3.MoveTowards(transform.position, MoneyUIDisplay.totalMoney.transform.position, speed * Time.deltaTime);
            speed += Time.deltaTime * 10;
            speed = Mathf.Clamp(speed, 0, 20);

            if (Vector3.Distance(transform.position, MoneyUIDisplay.totalMoney.transform.position) < 0.01f) {
                DataSaver.s.GetCurrentSave().money += amount;
                Destroy(gameObject);
            }
        }
    }
}
