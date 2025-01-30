using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using System;
using OVR.OpenVR;
using Unity.VisualScripting;


public class Deck : NetworkBehaviour
{
    private List<Card> decklist;

    int cardsInDeck = 100;

    [SerializeField]
    private GameObject model;

    [SerializeField]
    private GameObject cardPrefab;

    [SerializeField]
    private GameObject drawZone;

    UpdateDrawZone drawZoneUpdater;

    public GameObject currentCard;

    [SerializeField]
    IHand hand;

    [SerializeField]
    IHandGrabInteractor interactor;

    public ulong deckID;

    private bool newCardNeeded = false;

    public DeckData deckData;
    private Stack<string> currentDeck; 

    ClientRpcParams clientRpcParams;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentDeck = new Stack<string>();

        deckData = LocalPlayerManager.instance.GetLocalPlayerDeck();

        foreach (string card in deckData.cardsInDeck)
        {
            currentDeck.Push(card);
        }
        shuffle();

        drawZoneUpdater = drawZone.GetComponent<UpdateDrawZone>();

        cardsInDeck--;
        model.transform.localScale = new Vector3(100, 100, 100 * currentDeck.Count());
        //creates a first card to be grabbable on top of the deck

    }

    void shuffle()
    {
       
        //fisher-yates shuffle algo

        string[] deckShuffle = currentDeck.ToArray();

        for (int i = deckShuffle.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (deckShuffle[i], deckShuffle[j]) = (deckShuffle[j], deckShuffle[i]);
        }

        currentDeck.Clear();
        for (int i = 0; i< deckShuffle.Length; i++)
        {
            currentDeck.Push(deckShuffle[i]);
        }
            
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentCard)
        {
            Vector3 diff = currentCard.transform.position - transform.position;
            if (diff.magnitude > 0.01)
            {
                currentCard.GetComponent<Card>().SetLocked(false);
                newCardNeeded = true;
            }
        }
        
    }


    /// <summary>
    /// Method is called from Poses Manager when DrawCard Pose is detected. Checks if the hand is overlapping the draw zone and creates a new card on top when a new card is taken
    /// </summary>
    public void DrawFromDeck()
    {
            //checks if the hand is w
            if (drawZoneUpdater.GetIsInDrawZone())
            {

                    //checks if currentCard no longer in the spawn position
                    if (newCardNeeded)
                    {
                        if (NetworkManager.Singleton.IsServer)
                        {

                        SpawnNewCard();
                        //spawn a new card to be grabbed and change scale of deck to reflect cards left to be drawn

                        model.transform.localScale = new Vector3(100, 100, 100 * cardsInDeck);
                        newCardNeeded = false;

                        }
                        else
                        {
                        SpawnNewCardServerRpc();
                        }
                    }
             }

    }

    public void SpawnFirstCard()
    {
        currentCard = Instantiate(cardPrefab);
        
        currentCard.transform.position = transform.position;
        currentCard.transform.eulerAngles = new Vector3(90, 0,0) + transform.eulerAngles;
        Card currentCardObj = currentCard.GetComponent<Card>();
        currentCardObj.SetLocked(true);
        currentCardObj.lockPos = transform.position;

        string cardName = currentDeck.Pop();

        Texture2D tex = deckData.cardImages[cardName];
        CardData cardData = deckData.cardData[cardName];


        //set card face to the current texture
        List<Material> materials = new List<Material>();
        currentCard.GetComponent<Renderer>().GetMaterials(materials);
        materials[2].mainTexture = tex;

        currentCardObj.cardData = cardData;

        var cardNetworkObject = currentCard.GetComponent<NetworkObject>();
        cardNetworkObject.Spawn();
        cardNetworkObject.ChangeOwnership(deckID);


        NetworkObjectReference cardNetworkReference = new NetworkObjectReference(currentCard);
        ChangeCardTexClientRpc(cardNetworkReference,cardName);


    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnFirstCardServerRpc()
    {

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { deckID }
            }
        };

        SpawnFirstCard();
        this.NetworkObject.ChangeOwnership(deckID);

        NetworkObjectReference cardNetworkReference = new NetworkObjectReference(currentCard);

        newCardClientRpc(cardNetworkReference, clientRpcParams);

    }

    public void SpawnNewCard()
    {
        currentCard = Instantiate(cardPrefab);
        currentCard.transform.position = transform.position;
        currentCard.transform.eulerAngles = new Vector3(90, 0, 0) + transform.eulerAngles;
        Card currentCardObj = currentCard.GetComponent<Card>();
        currentCardObj.SetLocked(true);
        currentCardObj.lockPos = transform.position;

        string cardName = currentDeck.Pop();
        Texture2D tex = deckData.cardImages[cardName];
        CardData cardData = deckData.cardData[cardName];

        //set card face to the current texture
        List<Material> materials = new List<Material>();
        currentCard.GetComponent<Renderer>().GetMaterials(materials);
        materials[2].mainTexture = tex;

        currentCardObj.cardData = cardData;

        var cardNetworkObject = currentCard.GetComponent<NetworkObject>();
        cardNetworkObject.Spawn();
        cardNetworkObject.ChangeOwnership(deckID);

        NetworkObjectReference cardNetworkReference = new NetworkObjectReference(currentCard);
        ChangeCardTexClientRpc(cardNetworkReference, cardName);

    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnNewCardServerRpc()
    {
        SpawnNewCard();
        //spawn a new card to be grabbed and change scale of deck to reflect cards left to be drawn

        model.transform.localScale = new Vector3(100, 100, 100 * currentDeck.Count());
        newCardNeeded = false;

        NetworkObjectReference cardNetworkReference= new NetworkObjectReference(currentCard);

        newCardClientRpc(cardNetworkReference, clientRpcParams);
    }

    [ClientRpc]
    
    public void newCardClientRpc(NetworkObjectReference cardNetworkReference, ClientRpcParams clientRpcParams = default)
    {
        
        NetworkObject networkObject;
        cardNetworkReference.TryGet(out networkObject);

        currentCard = networkObject.gameObject;



        string cardName  = currentCard.GetComponent<Card>().cardData.name;




    }

    [ClientRpc]
    public void ChangeCardTexClientRpc(NetworkObjectReference cardNetworkReference, string cardName)
    {
        NetworkObject networkObject;
        cardNetworkReference.TryGet(out networkObject);
        GameObject changedCard = networkObject.gameObject;

        Texture2D tex = deckData.cardImages[cardName];
        CardData cardData = deckData.cardData[cardName];

        List<Material> materials = new List<Material>();
        changedCard.GetComponent<Renderer>().GetMaterials(materials);
        materials[2].mainTexture = tex;
    }


    

}
