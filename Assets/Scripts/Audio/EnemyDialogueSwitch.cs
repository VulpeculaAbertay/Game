using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDialogueSwitch : MonoBehaviour
{
    public List<AK.Wwise.Switch> MySwitches = new List<AK.Wwise.Switch>();
    public string enemySwitch;

    // Start is called before the first frame update
    void Start()
    {

        enemySwitch = PokerUIController.instance.enemyIcon.sprite.ToString();

        if (enemySwitch.Contains("RatIcon"))
        {
            MySwitches.Find(x => x.ToString() == "Rat").SetValue(gameObject);
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
