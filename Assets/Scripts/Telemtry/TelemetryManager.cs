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

    int bowmanLightAttacks;

    int bowmanHeavyAttacks;

    int bowmanLightDashes;

    int bowmanHeavyDashes;

    int bowmanDefense;

    int knightLightAttacks;

    int knightHeavyAttacks;

    int knightLightDashes;

    int knightHeavyDashes;

    int knightDefense;

    int berserkerLightAttacks;

    int berserkerHeavyAttacks;

    int berserkerLightDashes;

    int berserkerHeavyDashes;

    int berserkerDefense;

    float[] damageTaken = new float[4];

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
                       timeSpentKnight, timeSpentBerserker, bowmanLightAttacks, bowmanHeavyAttacks, bowmanLightDashes, bowmanHeavyDashes, bowmanDefense,
                       knightLightAttacks, knightHeavyAttacks, knightLightDashes, knightHeavyDashes, knightDefense,
                       berserkerLightAttacks, berserkerHeavyAttacks, berserkerLightDashes, berserkerHeavyDashes, berserkerDefense,
                       damageTaken);


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

    public void LightAttackCount(int loadout)
    {
        switch (loadout)
        {
            case 1:
                bowmanLightAttacks+=1;
                break;
            case 2:
                knightLightAttacks+=1;
                break;
            case 3:
                berserkerLightAttacks+=1;
                break;
        }
    }

    public void HeavyAttackCount(int loadout)
    {
        switch (loadout)
        {
            case 1:
                bowmanHeavyAttacks+=1;
                break;
            case 2:
                knightHeavyAttacks+=1;
                break;
            case 3:
                berserkerHeavyAttacks+=1;
                break;
        }
    }

    public void LightDashCount(int loadout)
    {
        switch (loadout)
        {
            case 1:
                bowmanLightDashes+=1;
                break;
            case 2:
                knightLightDashes+=1;
                break;
            case 3:
                berserkerLightDashes+=1;
                break;
        }
    }

    public void HeavyDashCount(int loadout)
    {
        switch (loadout)
        {
            case 1:
                bowmanHeavyDashes+=1;
                break;
            case 2:
                knightHeavyDashes+=1;
                break;
            case 3:
                berserkerHeavyDashes+=1;
                break;
        }
    }

    public void DefenseCount(int loadout)
    {
        switch (loadout)
        {
            case 1:
                bowmanDefense +=1;
                break;
            case 2:
                knightDefense +=1;
                break;
            case 3:
                berserkerDefense +=1;
                break;
        }
    }
    //Types:
    //1: MeleeDamage/BombDamage:
    //2: Projectile damage
    //3: Guardian Shield damage
    //4: Traps
    public void DamageTrack(int type, float damage)
    {
        damageTaken[type]+= damage;
    }



    void SaveTelemetryToCSV(
    float timePlayed,
    float enemiesKilledPct,
    float lootTakenPct,
    float deaths,
    float bowmanTime,
    float knightTime,
    float berserkerTime,
    int bowLightAtk,
    int bowHeavyAtk,
    int bowLightDash,
    int bowHeavyDash,
    int bowDefense,
    int knightLightAtk,
    int knightHeavyAtk,
    int knightLightDash,
    int knightHeavyDash,
    int knightDefense,
    int berserkLightAtk,
    int beserkHeavyAtk,
    int beserkLightDash,
    int beserkHeavyDash,
    int beserkDefense,
    float[] damageTaken)

    {
    string path = Application.dataPath + "/telemetry.csv";

    bool fileExists = File.Exists(path);

    StringBuilder sb = new StringBuilder();

    // If file doesn't exist, write header first
    if (!fileExists)
    {
        sb.AppendLine("session,time_played,enemies_killed_pct,loot_taken_pct,deaths,time_bowman,time_knight,time_berserker, bowman_lightAttacks, bowman_heavyAttacks, bowman_lightDashes, bowman_heavyDashes, bowman_defense, knight_lightAttacks, knight_heavyAttacks, knight_lightDashes, knight_heavyDashes, knight_defense, berserker_lightAttacks, berserker_heavyAttacks, berserker_lightDashes, berserker_heavyDashes, beserker_defense, player_damageTaken_melee_bombs, player_damageTaken_projectiles, player_damageTaken_guardianShields, player_damageTaken_traps");
    }

    // Session number = number of lines in file
    int sessionNumber = fileExists ? File.ReadAllLines(path).Length : 1;

    string row = string.Format(CultureInfo.InvariantCulture,
    "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26}",
        sessionNumber,
        timePlayed,
        enemiesKilledPct,
        lootTakenPct,
        deaths,
        bowmanTime,
        knightTime,
        berserkerTime,
        bowLightAtk,
        bowHeavyAtk,
        bowLightDash,
        bowHeavyDash,
        bowDefense,
        knightLightAtk,
        knightHeavyAtk,
        knightLightDash,
        knightHeavyDash,
        knightDefense,
        berserkLightAtk,
        beserkHeavyAtk,
        beserkLightDash,
        beserkHeavyDash,
        beserkDefense,
        damageTaken[0],
        damageTaken[1],
        damageTaken[2],
        damageTaken[3]);

    sb.AppendLine(row);

    File.AppendAllText(path, sb.ToString());

    Debug.Log("Telemetry saved to: " + path);
}

}
