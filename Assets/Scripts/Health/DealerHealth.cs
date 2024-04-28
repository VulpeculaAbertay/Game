using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;
using static System.TimeZoneInfo;

public class DealerHealth : MonoBehaviour
{

    public AK.Wwise.Event SmokePuffSFX;


    private void Awake()
    {
    }

    public int currentHealth;
    public bool activeEnemy;
    public int pcntChanceOfRandomHand;
    public Sprite uiIcon;
    public VisualEffect smokePuff;
    public GameObject model;
    public GameObject ui;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Update()
    {
    }

    public int GetHealth() { return currentHealth; }

    public void SetHealthText() { this.gameObject.GetComponentInChildren<UIDealerController>().SetHealthText(currentHealth); }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Dealer dealerInteractable = GetComponentInChildren<Dealer>();
            dealerInteractable.prompt = "";
        }

        if (SceneManager.GetActiveScene().name == "Poker")
        {
            PokerUIController.instance.SetEnemyHealthText(currentHealth);
            PokerUIController.instance.ShowHitSprite();
            PokerUIController.instance.ShowHealthChangeText(-damage, true);
        }
    }

    public void IncreaseHealth(int health)
    {
        currentHealth += health;

        if (SceneManager.GetActiveScene().name == "Poker")
        {
            PokerUIController.instance.SetEnemyHealthText(currentHealth);
            PokerUIController.instance.ShowHealthChangeText(health, true);
        }
    }

    public void DestroySelf()
    {
        StartCoroutine(DestroySelfEffect());
    }

    IEnumerator DestroySelfEffect()
    {
        yield return new WaitForSeconds(2);

        model.SetActive(false);
        ui.SetActive(false);
        
        smokePuff.Play();
        SmokePuffSFX.Post(gameObject);

        yield return new WaitForSeconds(3);

        Destroy(this.gameObject);
    }
}
