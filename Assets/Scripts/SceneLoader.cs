using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Rendering.Universal;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader instance;
    private void Awake()
    {
        instance = this;
    }

    public Animator transition;
    public float transitionTime = 1f;

    public GameObject playerPrefab;
    public GameObject playerStartingPosition;

    public GameObject[] enemyPrefabs;
    private List<string> enemyNames = new List<string>();
    private List<Vector3> enemyStartingPositions = new List<Vector3>();
    private List<Quaternion> enemyStartingRotations = new List<Quaternion>();
    private List<int> enemyStartingHealthValues = new List<int>();
    private List<int> enemyDifficultyValues = new List<int>();
    private List<EnemyReward> enemyRewards = new List<EnemyReward>();    

    public GameObject exitCostPrefab;
    public Vector3 exitCostStartingPosition = new Vector3(0f, 10f, 0f);
    public Quaternion exitCostStartingRotation = Quaternion.identity;

    public Camera canvasCamera;

    void Start()
    {        
        InstantiatePlayer();
        InstantiateEnemy();
        InstantiateExitCost();        
    }

    public void InstantiatePlayer()
    {
        GameObject proxyPlayer = GameObject.FindGameObjectWithTag("ProxyPlayer");
        if (proxyPlayer != null)
        {
            Destroy(proxyPlayer);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Instantiate(playerPrefab, playerStartingPosition.transform.position, playerStartingPosition.transform.rotation);                     
        }
        else
        {
            if (PlayerMovement.instance.moveToStartingPosition)
            {
                player.transform.position = playerStartingPosition.transform.position;
                player.transform.rotation = playerStartingPosition.transform.rotation;
                PlayerMovement.instance.moveToStartingPosition = false;
            }
        }

        if (SceneManager.GetActiveScene().name == "Poker")
        {
            PlayerMovement.instance.FreezePlayer();
            PlayerCamera.instance.DisablePlayerCamera();
        }
        else
        {
            PlayerMovement.instance.UnfreezePlayer();
            PlayerCamera.instance.EnablePlayerCamera();

            player = GameObject.FindGameObjectWithTag("Player");
            Camera mainCam = player.GetComponentsInChildren<Camera>().First();
            mainCam.GetUniversalAdditionalCameraData().cameraStack.Add(canvasCamera);
        }
    }

    public void InstantiateEnemy()
    {
        GameObject[] proxyEnemies = GameObject.FindGameObjectsWithTag("ProxyEnemy");
        if (proxyEnemies.Length > 0)
        {
            foreach (GameObject e in proxyEnemies)
            {                
                enemyNames.Add(e.name);
                enemyStartingPositions.Add(e.transform.position);
                enemyStartingRotations.Add(e.transform.rotation);
                enemyStartingHealthValues.Add(e.GetComponent<DealerHealth>().currentHealth);
                enemyDifficultyValues.Add(e.GetComponent<DealerHealth>().pcntChanceOfRandomHand);
                enemyRewards.Add(e.GetComponent<EnemyReward>());
                Destroy(e);
            }            
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            for (int i = 0; i < enemyStartingPositions.Count; i++)
            {
                GameObject enemyPrefab = enemyPrefabs.Where(prefab => prefab.name == enemyNames[i]).ToList<GameObject>().First();
                GameObject enemy = Instantiate(enemyPrefab, enemyStartingPositions[i], enemyStartingRotations[i]);
                enemy.GetComponent<DealerHealth>().currentHealth = enemyStartingHealthValues[i];
                enemy.GetComponent<DealerHealth>().SetHealthText();
                enemy.GetComponent<DealerHealth>().pcntChanceOfRandomHand = enemyDifficultyValues[i];
                enemy.GetComponent<EnemyReward>().baseSO = enemyRewards[i].baseSO;
                enemy.GetComponent<EnemyReward>().powerCardType = enemyRewards[i].powerCardType;
                enemy.GetComponent<EnemyReward>().SetUpCard();
            }
        }
    }

    public void InstantiateExitCost()
    {             
        GameObject exit = GameObject.FindGameObjectWithTag("ExitCost");
        if (exit == null)
        {
            Instantiate(exitCostPrefab, exitCostStartingPosition, exitCostStartingRotation);
            
            int exitHealthCost = int.Parse(GameObject.FindGameObjectWithTag("Exit").GetComponentInChildren<TextMeshPro>().text);

            if (ExitCost.instance != null)
            {
                ExitCost.instance.SetHealth(exitHealthCost);
            }
        }
    }

    public void LoadRoom(string nextSceneName)
    {        
        if (SceneManager.GetActiveScene().name.StartsWith("Room") && nextSceneName.StartsWith("Room"))
        {            
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length > 0)
            {
                foreach (GameObject e in enemies) { Destroy(e); }
            }

            GameObject exit = GameObject.FindGameObjectWithTag("ExitCost");
            if (exit != null) { Destroy(exit); }

            PlayerMovement.instance.FreezePlayer();
            PlayerMovement.instance.moveToStartingPosition = true;            
        }

        StartCoroutine(LoadNextScene(nextSceneName));
    }

    public void LoadPoker()
    {                
        PlayerInventory.instance.prevScene = SceneManager.GetActiveScene().name;        
        StartCoroutine(LoadNextScene("Poker"));
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadNextScene("MainMenu"));
    }

    IEnumerator LoadNextScene(string sceneName)
    {
        transition.SetTrigger("Start");        
        
        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(sceneName);

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (sceneName == "MainMenu")
        {
            GameObject exit = GameObject.FindGameObjectWithTag("ExitCost");

            foreach (GameObject enemy in enemies)
            {
                if (enemy != null) { Destroy(enemy); }
            }

            if (player != null) { Destroy(player); }

            if (exit != null) { Destroy(exit); }
        }
        else if (sceneName == "Poker")
        {

            foreach (GameObject enemy in enemies)
            {
                if (enemy != null)
                {
                    MeshRenderer[] enemyMeshRenderers = enemy.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mr in enemyMeshRenderers) { mr.enabled = false; };

                    enemy.GetComponentInChildren<UIDealerController>().gameObject.SetActive(false);
                    enemy.GetComponentInChildren<CapsuleCollider>().enabled = false;
                }
            }

            if (player != null)
            {
                player.GetComponentInChildren<MeshRenderer>().enabled = false;
                player.GetComponent<Interactor>().interactionPromptUI.Close();
                player.GetComponent<Interactor>().enabled = false;

            }

            PlayerMovement.instance.FreezePlayer();
            PlayerCamera.instance.DisablePlayerCamera();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            foreach (GameObject enemy in enemies)
            {
                if (enemy != null)
                {
                    MeshRenderer[] enemyMeshRenderers = enemy.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mr in enemyMeshRenderers) { mr.enabled = true; };

                    enemy.GetComponentInChildren<UIDealerController>(true).gameObject.SetActive(true);
                    enemy.GetComponentInChildren<CapsuleCollider>().enabled = true;

                    enemy.GetComponent<DealerHealth>().SetHealthText();
                }
            }

            if (player != null)
            {
                player.GetComponentInChildren<MeshRenderer>().enabled = true;
                player.GetComponent<Interactor>().enabled = true;
            }            
        }
    }    
}