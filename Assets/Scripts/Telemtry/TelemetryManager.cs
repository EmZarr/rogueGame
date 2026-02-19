using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;

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
        Debug.Log("Time spent Bowman: " + timeSpentBowMan);
        Debug.Log("Time spent Knight: " + timeSpentKnight);
        Debug.Log("Time spent Berserker: " + timeSpentBerserker);

        SaveTelemetryToCSV(timePlayed, eneKilledPerc, lootGathPerc,
                       playerDeaths, timeSpentBowMan,
                       timeSpentKnight, timeSpentBerserker);


        timePlayed = 0f;
        playerDeaths = 0;
        totalEnemies = 0;
        totalLoot = 0;
        lootTaken = 0;
        enemiesKilled = 0;
        timeSpentBowMan = 0f;
        timeSpentBerserker = 0;
        timeSpentKnight = 0;

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


    void SaveTelemetryToCSV(
    float timePlayed,
    float enemiesKilledPct,
    float lootTakenPct,
    float deaths,
    float bowmanTime,
    float knightTime,
    float berserkerTime)
{
    string path = Application.dataPath + "/telemetry.csv";

    bool fileExists = File.Exists(path);

    StringBuilder sb = new StringBuilder();

    // If file doesn't exist, write header first
    if (!fileExists)
    {
        sb.AppendLine("session,time_played,enemies_killed_pct,loot_taken_pct,deaths,time_bowman,time_knight,time_berserker");
    }

    // Session number = number of lines in file
    int sessionNumber = fileExists ? File.ReadAllLines(path).Length : 1;

    string row = string.Format(CultureInfo.InvariantCulture,
    "{0},{1},{2},{3},{4},{5},{6},{7}",
        sessionNumber,
        timePlayed,
        enemiesKilledPct,
        lootTakenPct,
        deaths,
        bowmanTime,
        knightTime,
        berserkerTime);

    sb.AppendLine(row);

    File.AppendAllText(path, sb.ToString());

    Debug.Log("Telemetry saved to: " + path);
}

}
