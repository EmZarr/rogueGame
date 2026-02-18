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
        timePlayed = 0f;
    }

    public float LootTakenPercentage()
    {
        return lootTaken/totalLoot * 100.0f;
    }

    public float EnemiesKilledPercentage()
    {
        return enemiesKilled/totalEnemies * 100.0f;
    }
}
