using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public class Card : MonoBehaviour
{
    public CardScriptableObject cardSO;

    public bool isPlayer;

    public int value;
    public string suit;
    public GameObject model;
    public PowerCardController.PowerCardType powerCardType;

    private Vector3 targetPoint;
    private Quaternion targetRot;
    //private Vector3 targetScale;
    public float moveSpeed = 5f;
    public float rotateSpeed = 540f;
    public float growSpeed = 540f;

    public bool inHand;
    public int handPosition;

    private HandController hc;
    private Camera mainCam;

    public bool isSelected;
    private bool isInSelectedPosition;
    private Collider col;

    public LayerMask whatIsDesktop;
    public LayerMask whatIsPlacement;

    public CardPlacePoint assignedPlace;

    private Renderer cardRenderer;
    private Color originalColour;
    private Color targetColour;
    private float transparentAlpha = 0.5f;
    private Color lerpedColour;
    private float alphaChangeSpeed = 6.75f;
    private bool isMouseOverAndHasFadedOut = false;

    // Start is called before the first frame update
    void Start()
    {        
        if(targetPoint == Vector3.zero)
        {
            targetPoint = transform.position;
            targetRot = transform.rotation;
        }
        
        SetUpCard();
        hc = FindObjectOfType<HandController>();
        col = GetComponent<Collider>();
        mainCam = Camera.main;
        cardRenderer = model.GetComponent<MeshRenderer>();
        originalColour = cardRenderer.material.color;
        targetColour = originalColour;
    }

    // Set the value, suit and mesh to display based on scriptable object
    public void SetUpCard()
    {
        value = cardSO.value;
        suit = cardSO.suit;        
        powerCardType = cardSO.powerCardType;

        AddPowerCardMaterial();
    }

    void AddPowerCardMaterial()
    {
        Material[] mats = model.GetComponent<MeshRenderer>().materials;
        mats[0] = cardSO.material;

        switch (powerCardType)
        {
            case PowerCardController.PowerCardType.None:
                mats[1] = PowerCardController.instance.noneMaterial;
                break;

            case PowerCardController.PowerCardType.Wildcard:
                mats[1] = PowerCardController.instance.wildcardMaterial;
                break;

            case PowerCardController.PowerCardType.FreeSwap:
                mats[1] = PowerCardController.instance.freeSwapMaterial;
                break;

            case PowerCardController.PowerCardType.HandSwap:
                mats[1] = PowerCardController.instance.handSwapMaterial;
                break;

            case PowerCardController.PowerCardType.HalfClubs:
                mats[1] = PowerCardController.instance.halfClubsMaterial;
                break;

            case PowerCardController.PowerCardType.HalfSpades:
                mats[1] = PowerCardController.instance.halfSpadesMaterial;
                break;

            case PowerCardController.PowerCardType.HalfHearts:
                mats[1] = PowerCardController.instance.halfHeartsMaterial;
                break;

            case PowerCardController.PowerCardType.HalfDiamonds:
                mats[1] = PowerCardController.instance.halfDiamondsMaterial;
                break;

            case PowerCardController.PowerCardType.AutoPair:
                mats[1] = PowerCardController.instance.autoPairMaterial;
                break;
        }

        model.GetComponent<MeshRenderer>().materials = mats;
    }

    // Update is called once per frame
    void Update()
    {
        // Linear interpolation to target point in moveSpeed increments
        transform.position = Vector3.Lerp(transform.position, targetPoint, moveSpeed * Time.deltaTime);
        // Match target rotation in rotateSpeed increments
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        // Match target colour in alphaChangeSpeed increments
        lerpedColour = Color.Lerp(cardRenderer.material.color, targetColour, alphaChangeSpeed * Time.deltaTime);
        cardRenderer.material.color = lerpedColour;

        // If card has been made transparent, set target colour to opaque again
        if (Mathf.Round(cardRenderer.material.color.a * 10) * 0.1 <= transparentAlpha) { MakeOpaque(); }

        if (isSelected)
        {
            // Cast a ray from the camera to the mouse position
            Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;              

            if (Input.GetMouseButtonDown(0) && BattleController.instance.currentPhase == BattleController.TurnOrder.playerActive)
            {
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider == col)
                    {
                        if (!isInSelectedPosition) { AddToSelection(); } else { ReturnToHand(); }
                    }
                }
            }

            // Right click to return card to hand
            if (Input.GetMouseButtonDown(1)) { ReturnToHand(); }                        
        }

        double zPos = Mathf.Round(transform.position.z);        
        if (zPos == Mathf.Round(BattleController.instance.playerDiscardPosition.position.z) ||
            zPos == Mathf.Round(BattleController.instance.enemyDiscardPosition.position.z))
        {
            Destroy(this.gameObject);
        }
    }

    // Set point and rotation for card
    public void MoveToPoint(Vector3 pointToMoveTo, Quaternion rotToMatch)
    {
        targetPoint = pointToMoveTo;
        targetRot = rotToMatch;        
    }

    //public void GrowToScale(Vector3 scaleToGrowTo)
    //{
    //    targetScale = scaleToGrowTo;
    //}

    // Pop up card towards camera on mouse hover
    private void OnMouseOver()
    {
        if (inHand && isPlayer && !isSelected)
        {
            MoveToPoint(hc.cardPositions[handPosition] + new Vector3(-.1f, .1f, 0), hc.minPos.rotation);

            // Make cards in hand next to this card transparent
            if (!isMouseOverAndHasFadedOut)
            {
                hc.SetTransparency(this, "mouse over");
                // Stop card cycling between transparent and opaque while mouse over
                isMouseOverAndHasFadedOut = true;
            }
        }
    }

    // Move card back down if mouse is no longer hovering over
    private void OnMouseExit()
    {
        if (inHand && isPlayer && !isSelected)
        {
            MoveToPoint(hc.cardPositions[handPosition], hc.minPos.rotation);

            isMouseOverAndHasFadedOut = false;

            // Make cards in hand next to this card transparent
            hc.SetTransparency(this, "mouse exit");
        }       
    }

    // Prevent card being selected again on click
    private void OnMouseDown()
    {
        if (inHand && BattleController.instance.currentPhase == BattleController.TurnOrder.playerActive && isPlayer
            && hc.selectedCards.Count < 5 && !isSelected)
        {            
            hc.SetTransparency(this, "select");

            isSelected = true;
        }
    }

    public void AddToSelection()
    {
        hc.SelectCard(this);
        hc.SortSelectedCards();
        
        MoveToPoint(hc.cardPositions[handPosition] + new Vector3(-.3f, .2f, 0), hc.minPos.rotation);
        
        isInSelectedPosition = true;
    }

    public void ReturnToHand()
    {        
        hc.SetTransparency(this, "return");
        
        hc.selectedCards.Remove(this);
        hc.SortSelectedCards();
        isSelected = false;

        MoveToPoint(hc.cardPositions[handPosition], hc.minPos.rotation);
        
        isInSelectedPosition = false;
    }

    public void MakeTransparent()
    {        
        var r = cardRenderer.material.color.r;
        var g = cardRenderer.material.color.g;
        var b = cardRenderer.material.color.b;

        targetColour = new Color(r, g, b, transparentAlpha);
    }

    public void MakeOpaque()
    {        
        targetColour = originalColour;
    }
}
