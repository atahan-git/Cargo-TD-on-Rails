using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinDrop : MonoBehaviour {
    private Vector2 randomDir;
    public void SetUp(Vector3 source, int _amount, bool isMiniCoin = false) {
        amount = _amount;
        GetComponent<UIElementFollowWorldTarget>().OneTimeSetPosition(source);
        //GetComponentInChildren<MoneyUIDisplay>().SetAmount(amount);
        if (!isMiniCoin) {
            StartCoroutine(SpawnMiniCoins(source, _amount));
        }

        randomDir = Random.insideUnitCircle;
    }

    IEnumerator SpawnMiniCoins(Vector3 source, int count) {
        for (int i = 0; i < count; i++) {
            Instantiate(LevelReferences.s.coinDrop, LevelReferences.s.uiDisplayParent).GetComponent<CoinDrop>().SetUp(source /*+ Random.insideUnitSphere * 0.5f*/, 1, true);
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
                DataSaver.s.GetCurrentSave().metaProgress.money += amount;
                Destroy(gameObject);
            }
        }
    }
}
