using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
//using static HandEvaluator;

public class BattleController : MonoBehaviour
{
    public static BattleController instance;
    public DealerHealth activeEnemy;

    public AK.Wwise.Event WinSound;
    public AK.Wwise.Event LoseSound;

    private void Awake()
    {
        instance = this;
        List<DealerHealth> dealers = FindObjectsOfType<DealerHealth>().Where(dealer => dealer.activeEnemy == true).ToList<DealerHealth>();
        activeEnemy = dealers.First();
    }

    public int startingCardsAmount = 7;    
    public int currentBet;

    public enum TurnOrder { playerBetting, playerActive, enemyActive, resolveHands}
    public TurnOrder currentPhase;

    public Transform playerDiscardPosition;
    public Transform enemyDiscardPosition;
    public bool checkCardsDiscarded;
    
    // Start is called before the first frame update
    void Start()
    {
        PokerUIController.instance.placeBetButton.SetActive(true);        
        PokerUIController.instance.betSlider.gameObject.SetActive(true);
        PokerUIController.instance.playHandButton.SetActive(false);
        PokerUIController.instance.swapCardButton.SetActive(false);
        PokerUIController.instance.playAgainButton.SetActive(false);

        checkCardsDiscarded = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Get number of auto pairs in selected cards
        int numAutoPairs = HandController.instance.selectedCards.Where(card => card.powerCardType == PowerCardController.PowerCardType.AutoPair).ToList().Count;

        if (HandController.instance.selectedCards.Count == (HandController.instance.numCardsRequiredToPlay - numAutoPairs))
        {
            PokerUIController.instance.playHandButton.GetComponent<Button>().interactable = true;
        }
        else
        {
            PokerUIController.instance.playHandButton.GetComponent<Button>().interactable = false;
        }

        if (PokerUIController.instance.betSlider.GetComponent<Slider>().value > 0)
        {
            PokerUIController.instance.placeBetButton.GetComponent<Button>().interactable = true;
        }
        else
        {
            PokerUIController.instance.placeBetButton.GetComponent<Button>().interactable = false;
        }

        // Get number of hand swaps and free swaps in selected cards
        int numHandSwaps = HandController.instance.selectedCards.Where(card => card.powerCardType == PowerCardController.PowerCardType.HandSwap).ToList().Count;
        int numFreeSwaps = HandController.instance.selectedCards.Where(card => card.powerCardType == PowerCardController.PowerCardType.FreeSwap).ToList().Count;
        int swapCost = (HandController.instance.selectedCards.Count - numFreeSwaps);

        // Allow player to swap cards if they have more health minus current bet than number of cards being swapped - free swaps
        if ((HandController.instance.selectedCards.Count > 0 && (PlayerHealth.instance.currentHealth - currentBet) >= swapCost) || numHandSwaps > 0)
        {
            string buttonText = "";

            // Change button text to display cost of swaps            
            if (numHandSwaps > 0)
            {
                buttonText = "Swap Hand";
            }
            else
            {
                buttonText = "Swap Selected\n(-" + swapCost.ToString() + " Heart";
                if (swapCost > 1) { buttonText += "s)"; } else { buttonText += ")"; }                
            }

            PokerUIController.instance.swapCardButton.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
            PokerUIController.instance.swapCardButton.GetComponent<Button>().interactable = true;
        }
        else
        {
            PokerUIController.instance.swapCardButton.GetComponentInChildren<TextMeshProUGUI>().text = "Swap";
            PokerUIController.instance.swapCardButton.GetComponent<Button>().interactable = false;
        }

        if (checkCardsDiscarded)
        {            
            bool allCardsDiscarded = true;

            for (int i = 0; i < HandController.instance.playedCards.Count; i++)
            {
                double playerCardZPos = Mathf.Round(HandController.instance.playedCards[i].transform.position.z);
                double enemyCardZPos = Mathf.Round(EnemyController.instance.playedCards[i].transform.position.z);
                
                if (playerCardZPos != Mathf.Round(playerDiscardPosition.position.z) ||
                    enemyCardZPos != Mathf.Round(enemyDiscardPosition.position.z))
                {
                    allCardsDiscarded = false;
                }
            }

            if (allCardsDiscarded)
            {
                ResetPlayedCards();
                checkCardsDiscarded = false;
            }
        }
    } 

    public void PlayHand()
    {
        HandController.instance.PlayHand();       
        AdvanceTurn();
    }

    public void PlaceBet(int bet)
    {
        currentBet = bet;
        PlayerHealth.instance.PlaceBet(bet);        
        DeckController.instance.DrawMultipleCards(startingCardsAmount - HandController.instance.heldCards.Count);
        EnemyController.instance.DrawMultipleCards(startingCardsAmount - EnemyController.instance.heldCards.Count);
        AdvanceTurn();
    }

    public void PlayAgain()
    {
        currentBet = 0;
        
        PokerUIController.instance.playAgainButton.GetComponent<Button>().interactable = false;

        DiscardPlayedCards();        
    }

    public void DiscardPlayedCards()
    {
        for (int i = 0; i < HandController.instance.playedCards.Count; i++)
        {
            HandController.instance.playedCards[i].moveSpeed = 1.75f;
            HandController.instance.playedCards[i].MakeTransparent();
            HandController.instance.playedCards[i].MoveToPoint(playerDiscardPosition.position, playerDiscardPosition.rotation);

            EnemyController.instance.playedCards[i].moveSpeed = 1.75f;
            EnemyController.instance.playedCards[i].MakeTransparent();
            EnemyController.instance.playedCards[i].MoveToPoint(enemyDiscardPosition.position, enemyDiscardPosition.rotation);            
        }

        HandController.instance.playedCards.Clear();
        EnemyController.instance.playedCards.Clear();
        AdvanceTurn();

        //checkCardsDiscarded = true;
    }

    public void DiscardHeldCards()
    {
        for (int i = 0; i < EnemyController.instance.heldCards.Count; i++)
        {            
            EnemyController.instance.heldCards[i].moveSpeed = 1.75f;
            EnemyController.instance.heldCards[i].MakeTransparent();
            EnemyController.instance.heldCards[i].MoveToPoint(enemyDiscardPosition.position, enemyDiscardPosition.rotation);
        }
    }

    public void ResetPlayedCards()
    {
        for (int i = 0; i < HandController.instance.playedCards.Count; i++)
        {
            Destroy(HandController.instance.playedCards[i].gameObject);
            Destroy(EnemyController.instance.playedCards[i].gameObject);
        }

        HandController.instance.playedCards.Clear();
        EnemyController.instance.playedCards.Clear();
        AdvanceTurn();
    }

    public void ResolveHands(HandEvaluator.HandRank playerHandRank, HandEvaluator.HandRank enemyHandRank)
    {
        PokerUIController.instance.SetHandText(playerHandRank.ToString(), enemyHandRank.ToString());

        if (playerHandRank > enemyHandRank)
        {
            PokerUIController.instance.SetWinnerText("You Win!");
            PlayerHealth.instance.IncreaseHealth(currentBet);           
            activeEnemy.TakeDamage(currentBet);
            LoseSound.Post(gameObject);
        }
        else if (playerHandRank < enemyHandRank)
        {
            PokerUIController.instance.SetWinnerText("Enemy Wins");
            PlayerHealth.instance.TakeDamage(currentBet);
            WinSound.Post(gameObject);
        }
        else
        {
            HandEvaluator.TieWinner tieWinner = HandEvaluator.instance.BreakTie(HandController.instance.playedCards.OrderBy(c => c.value).ToList(),
                                EnemyController.instance.playedCards.OrderBy(c => c.value).ToList(),
                                playerHandRank);

            if (tieWinner == HandEvaluator.TieWinner.Player)
            {
                PokerUIController.instance.SetWinnerText("You Win!");
                PlayerHealth.instance.IncreaseHealth(currentBet);
                activeEnemy.TakeDamage(currentBet);
                LoseSound.Post(gameObject);
            }
            else if (tieWinner == HandEvaluator.TieWinner.Enemy)
            {
                PokerUIController.instance.SetWinnerText("Enemy Wins");
                PlayerHealth.instance.TakeDamage(currentBet);
                WinSound.Post(gameObject);
            }
            else
            {
                PokerUIController.instance.SetWinnerText("Tie");
            }
        }
    }

    public void AdvanceTurn()
    {
        currentPhase++;

        if((int)currentPhase >= System.Enum.GetValues(typeof(TurnOrder)).Length) { currentPhase = 0; }

        switch (currentPhase)
        {
            case TurnOrder.playerBetting:

                RewardCardUI.instance.rewardCardParentObject.SetActive(false);
                PokerUIController.instance.betSlider.GetComponent<BetSlider>().InitSlider();
                PokerUIController.instance.playerHandText.gameObject.SetActive(false);
                PokerUIController.instance.enemyHandText.gameObject.SetActive(false);
                PokerUIController.instance.winnerText.gameObject.SetActive(false);
                PokerUIController.instance.playAgainButton.SetActive(false);
                PokerUIController.instance.leaveButton.SetActive(false);
                PokerUIController.instance.placeBetButton.SetActive(true);
                PokerUIController.instance.betSlider.gameObject.SetActive(true);                
                PokerUIController.instance.playHandButton.SetActive(false);
                PokerUIController.instance.swapCardButton.SetActive(false);

                break;

            case TurnOrder.playerActive:

                PokerUIController.instance.placeBetButton.SetActive(false);
                PokerUIController.instance.betSlider.gameObject.SetActive(false);
                PokerUIController.instance.playHandButton.SetActive(true);
                PokerUIController.instance.playHandButton.GetComponent<Button>().interactable = false;
                PokerUIController.instance.swapCardButton.SetActive(true);                                

                break;

            case TurnOrder.enemyActive:

                PokerUIController.instance.playHandButton.SetActive(false);
                EnemyController.instance.PlayHand();
                AdvanceTurn();
                
                break;

            case TurnOrder.resolveHands:

                HandEvaluator.HandRank playerHandRank = HandEvaluator.instance.EvaluateHand(HandController.instance.playedCards, true);

                // Increase rank by number of UpgradeRank power cards
                if ((int)playerHandRank + PowerCardController.instance.numRanksToUpgrade > 9)
                {
                    playerHandRank = (HandEvaluator.HandRank)9;
                }
                else
                {
                    playerHandRank += PowerCardController.instance.numRanksToUpgrade;
                }

                // Gain heart for each GainHeart power card
                if (PowerCardController.instance.numHeartsToGain > 0)
                {
                    PlayerHealth.instance.IncreaseHealth(PowerCardController.instance.numHeartsToGain);
                }

                HandEvaluator.HandRank enemyHandRank = HandEvaluator.instance.EvaluateHand(EnemyController.instance.playedCards, true);

                ResolveHands(playerHandRank, enemyHandRank);

                PokerUIController.instance.playerHandText.gameObject.SetActive(true);
                PokerUIController.instance.enemyHandText.gameObject.SetActive(true);                
                PokerUIController.instance.HideBetIcons();

                if (PlayerHealth.instance.isGameOver)
                {
                    string winnerText = PokerUIController.instance.winnerText.text;
                    PokerUIController.instance.SetWinnerText(winnerText + "\nYou've Gone Bust!");
                    PokerUIController.instance.winnerText.gameObject.SetActive(true);
                    PokerUIController.instance.swapCardButton.SetActive(false);
                    PokerUIController.instance.endGameButton.SetActive(true);
                    PokerUIController.instance.healthValueText.gameObject.SetActive(false);
                    //PokerUIController.instance.playerHeart.SetActive(false);
                    PokerUIController.instance.playerBet.SetActive(false);
                }
                else if (activeEnemy.GetHealth() != 0)
                {
                    PokerUIController.instance.winnerText.gameObject.SetActive(true);
                    PokerUIController.instance.playAgainButton.SetActive(true);
                    PokerUIController.instance.playAgainButton.GetComponent<Button>().interactable = true;
                    PokerUIController.instance.swapCardButton.SetActive(false);
                    PokerUIController.instance.leaveButton.SetActive(true);
                }                                
                else
                {
                    DiscardHeldCards();
                    VFXController.instance.sparkles.Play();
                    RewardCardUI.instance.rewardCardParentObject.SetActive(true);
                    PlayerInventory.instance.AddRewardCardtoDeck(RewardCardUI.instance.rewardCard.cardSO);
                    PokerUIController.instance.swapCardButton.SetActive(false);
                    PokerUIController.instance.leaveButton.SetActive(true);                    
                    PokerUIController.instance.enemyIconOverlayDefeated.SetActive(true);
                    //PokerUIController.instance.enemyHealthValueText.gameObject.SetActive(false);
                    //PokerUIController.instance.enemyHeart.SetActive(false);
                    PokerUIController.instance.enemyBet.SetActive(false);
                }

                break;
        }
    }    
}
