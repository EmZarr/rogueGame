using UnityEngine;

public class TelemetryManager : MonoBehaviour
{
    bool timerStarted;
    float timePlayed;

    float playerDeaths;

    float lootTaken;

    float enemiesKilled;

    float totalEnemies;

    float totalLoot;

    float timeSpentKnight;
    float timeSpentBerserker;
    float timeSpentBowMan;

    int loadOutNumber;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (timerStarted)
        {
            timePlayed+= Time.deltaTime;
        }
        switch (loadOutNumber)
        {
            case 1: 
                timeSpentBowMan+= Time.deltaTime; 
                break;
            case 2:
                timeSpentKnight+= Time.deltaTime;
                break;
            case 3:
                timeSpentBerserker+= Time.deltaTime;
                break;
        }
    }

    public void SetTotalEnemies(float enemies)
    {
        totalEnemies = enemies;
    }

    public void SetTotalLoot(float loot)
    {
        totalLoot = loot;
    }

    public void EnemyKilled()
    {
        enemiesKilled +=1;
    }

    public void LootPickedUp()
    {
        lootTaken += 1;
    }

    public void StartTimer()
    {
        timerStarted = true;
    }

    public void UploadData()
    {
        timerStarted = false;
        float eneKilledPerc = EnemiesKilledPercentage();
        float lootGathPerc = LootTakenPercentage();
        Debug.Log("Time played: " + timePlayed);
        Debug.Log("Enemies killed % " + eneKilledPerc);
        Debug.Log("Loot taken % " + lootGathPerc);
        Debug.Log("Player deaths: " + playerDeaths);

        timePlayed = 0f;
        playerDeaths = 0;
        totalEnemies = 0;
        totalLoot = 0;
        lootTaken = 0;
        enemiesKilled = 0;

    }

    public float LootTakenPercentage()
    {
        return lootTaken/totalLoot * 100.0f;
    }

    public float EnemiesKilledPercentage()
    {
        return enemiesKilled/totalEnemies * 100.0f;
    }

    public void PlayerDied()
    {
        playerDeaths+=1;
    }

    public void SetLoadOut(int loadout)
    {
        loadOutNumber = loadout;
    }
}
