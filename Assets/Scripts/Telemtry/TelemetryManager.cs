using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class TelemetryManager : MonoBehaviour
{

    TelemetrySender telemetrySender;
    bool timerStarted;
    float timePlayed;

    float playerDeaths;

    float lootTaken;

    float powerUpsTaken;

    float healthBarrelsTaken;

    float totalMapPowerUps;

    float enemiesKilled;
    float totalEnemies;

    float totalLoot;

    float averageEnemiesAliveOnLootPickup;

    float timeSpentKnight;
    float timeSpentBerserker;
    float timeSpentBowMan;

    //1 Bowman, 2 Knight, 3 Berserker
    public int loadOutNumber;

    public string levelPlayID;


    //1 light, 2 heavy, 3 heavy dash
    public int mostRecentAttackType;

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

    public int[,,] loadoutToEnemy = new int[3,3,5];

    float[] mapBehaviors = new float[5];

    float AttackSpeedMultiplier;
    float MovementSpeedMultiplier;
    float DamageMultiplier;

    public int[,] defenseToEnemy = new int[3,5];

    float averageDistanceToEnemies;
    float averageDistanceToWall;
    float averageDistanceToMainPath;
    
    float optionalComponentsEntered;

    float amountOptionalComponentsOnMap = 0;

    int formChangeAmount;
    int formChangeAmountInCombat;

    int distanceCounterRoad = 1;

    int distanceCounterWall = 1;
    int distanceCounterEnemy = 1;

    float TotalMapScore;
    float CurrentMapScore;
    float totalScore;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        telemetrySender = FindFirstObjectByType<TelemetrySender>();
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

    public void IncreaseMapPowerupCounter()
    {
        totalMapPowerUps +=1;
    }

    public void PowerUpTaken()
    {
        powerUpsTaken+=1;
        averageEnemiesAliveOnLootPickup += (totalEnemies-enemiesKilled);
    }

    public void EnemyKilled()
    {
        enemiesKilled +=1;
    }

    public void LootPickedUp()
    {
        lootTaken += 1;
    }

    public void HealthBarrelTaken()
    {
        healthBarrelsTaken +=1;
    }

    public void StartTimer()
    {
        timerStarted = true;
    }

    public void IncreaseTotalMapScore(float score)
    {
        TotalMapScore += score;
    }

    public void IncreaseCurrentMapScore(float score)
    {
        CurrentMapScore += score;
    }

    public void SetTotalScore(float score)
    {
        totalScore = score;
    }
    public void GenerateRandomLevelID(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new System.Random();
        levelPlayID = new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    public void UploadData()
    {
        timerStarted = false;
        float eneKilledPerc = EnemiesKilledPercentage();
        float lootGathPerc = LootTakenPercentage();

        SaveTelemetryToCSV(timePlayed, eneKilledPerc, lootGathPerc,
                       playerDeaths, timeSpentBowMan,
                       timeSpentKnight, timeSpentBerserker, bowmanLightAttacks, bowmanHeavyAttacks, bowmanLightDashes, bowmanHeavyDashes, bowmanDefense,
                       knightLightAttacks, knightHeavyAttacks, knightLightDashes, knightHeavyDashes, knightDefense,
                       berserkerLightAttacks, berserkerHeavyAttacks, berserkerLightDashes, berserkerHeavyDashes, berserkerDefense,
                       damageTaken);

        var telemetryData = new TelemetryData
        {
            levelPlayID = levelPlayID,
            playerId = SystemInfo.deviceUniqueIdentifier,
            timePlayed = timePlayed,
            TotalScore = totalScore,
            MapScore = CurrentMapScore,
            MapScorePercentage = CurrentMapScore/TotalMapScore * 100f,
            enemiesKilledPct = eneKilledPerc,
            HealthBarrelsTaken = healthBarrelsTaken,
            PowerUpsTaken = powerUpsTaken,
            AvgEnemiesAliveOnPowerUpTaken = powerUpsTaken > 0? averageEnemiesAliveOnLootPickup/powerUpsTaken : 0,
            lootTakenPct = lootGathPerc,
            deaths = playerDeaths,
            bowmanTime = timeSpentBowMan,
            knightTime = timeSpentKnight,
            berserkerTime = timeSpentBerserker,
            bowLightAtk = bowmanLightAttacks,
            bowHeavyAtk = bowmanHeavyAttacks,
            bowLightDash = bowmanLightDashes,
            bowHeavyDash = bowmanHeavyDashes,
            bowDefense = bowmanDefense,
            knightLightAtk = knightLightAttacks,
            knightHeavyAtk = knightHeavyAttacks,
            knightLightDash = knightLightDashes,
            knightHeavyDash = knightHeavyDashes,
            knightDefense = knightDefense,
            berserkLightAtk = berserkerLightAttacks,
            berserkHeavyAtk = berserkerHeavyAttacks,
            berserkLightDash = berserkerLightDashes,
            berserkHeavyDash = berserkerHeavyDashes,
            berserkDefense = berserkerDefense,
            damageTakenMelee = damageTaken[0],
            damageTakenRanged = damageTaken[1],
            damageTakenGuardianShield = damageTaken[2],
            damageTakenTraps = damageTaken[3],
            BowLightAttacksMeleeEnemy = loadoutToEnemy[0, 0, 0],
            BowHeavyAttacksMeleeEnemy = loadoutToEnemy[0, 1, 0],
            BowHeavyDashesMeleeEnemy = loadoutToEnemy[0, 2, 0],
            BowLightAttacksRangedEnemy = loadoutToEnemy[0, 0, 1],
            BowHeavyAttacksRangedEnemy = loadoutToEnemy[0, 1, 1],
            BowHeavyDashesRangedEnemy = loadoutToEnemy[0, 2, 1],
            BowLightAttacksBomberEnemy = loadoutToEnemy[0, 0, 2],
            BowHeavyAttacksBomberEnemy = loadoutToEnemy[0, 1, 2],
            BowHeavyDashesBomberEnemy = loadoutToEnemy[0, 2, 2],
            BowLightAttacksAssassinEnemy = loadoutToEnemy[0, 0, 3],
            BowHeavyAttacksAssassinEnemy = loadoutToEnemy[0, 1, 3],
            BowHeavyDashesAssassinEnemy = loadoutToEnemy[0, 2, 3],
            BowLightAttacksGuardianEnemy = loadoutToEnemy[0, 0, 4],
            BowHeavyAttacksGuardianEnemy = loadoutToEnemy[0, 1, 4],
            BowHeavyDashesGuardianEnemy = loadoutToEnemy[0, 2, 4],
            KnightLightAttacksMeleeEnemy = loadoutToEnemy[1, 0, 0],
            KnightHeavyAttacksMeleeEnemy = loadoutToEnemy[1, 1, 0],
            KnightHeavyDashesMeleeEnemy = loadoutToEnemy[1, 2, 0],
            KnightLightAttacksRangedEnemy = loadoutToEnemy[1, 0, 1],
            KnightHeavyAttacksRangedEnemy = loadoutToEnemy[1, 1, 1],
            KnightHeavyDashesRangedEnemy = loadoutToEnemy[1, 2, 1],
            KnightLightAttacksBomberEnemy = loadoutToEnemy[1, 0, 2],
            KnightHeavyAttacksBomberEnemy = loadoutToEnemy[1, 1, 2],
            KnightHeavyDashesBomberEnemy = loadoutToEnemy[1, 2, 2],
            KnightLightAttacksAssassinEnemy = loadoutToEnemy[1, 0, 3],
            KnightHeavyAttacksAssassinEnemy = loadoutToEnemy[1, 1, 3],
            KnightHeavyDashesAssassinEnemy = loadoutToEnemy[1, 2, 3],
            KnightLightAttacksGuardianEnemy = loadoutToEnemy[1, 0, 4],
            KnightHeavyAttacksGuardianEnemy = loadoutToEnemy[1, 1, 4],
            KnightHeavyDashesGuardianEnemy = loadoutToEnemy[1, 2, 4],
            BerserkerLightAttacksMeleeEnemy = loadoutToEnemy[2, 0, 0],
            BerserkerHeavyAttacksMeleeEnemy = loadoutToEnemy[2, 1, 0],
            BerserkerHeavyDashesMeleeEnemy = loadoutToEnemy[2, 2, 0],
            BerserkerLightAttacksRangedEnemy = loadoutToEnemy[2, 0, 1],
            BerserkerHeavyAttacksRangedEnemy = loadoutToEnemy[2, 1, 1],
            BerserkerHeavyDashesRangedEnemy = loadoutToEnemy[2, 2, 1],
            BerserkerLightAttacksBomberEnemy = loadoutToEnemy[2, 0, 2],
            BerserkerHeavyAttacksBomberEnemy = loadoutToEnemy[2, 1, 2],
            BerserkerHeavyDashesBomberEnemy = loadoutToEnemy[2, 2, 2],
            BerserkerLightAttacksAssassinEnemy = loadoutToEnemy[2, 0, 3],
            BerserkerHeavyAttacksAssassinEnemy = loadoutToEnemy[2, 1, 3],
            BerserkerHeavyDashesAssassinEnemy = loadoutToEnemy[2, 2, 3],
            BerserkerLightAttacksGuardianEnemy = loadoutToEnemy[2, 0, 4],
            BerserkerHeavyAttacksGuardianEnemy = loadoutToEnemy[2, 1, 4],
            BerserkerHeavyDashesGuardianEnemy = loadoutToEnemy[2, 2, 4],
            GeometryBehavior = mapBehaviors[0],
            FurnishingBehaviorSpread = mapBehaviors[1],
            FurnishingBehaviorRatio = mapBehaviors[2],
            EnemyBehaviorRatio = mapBehaviors[3],
            EnemyBehaviorDifficulty = mapBehaviors[4],
            AttackSpeedMultiplier = AttackSpeedMultiplier,
            MovementSpeedMultiplier = MovementSpeedMultiplier,
            DamageMultiplier = DamageMultiplier,
            FormChangeCount = formChangeAmount,
            FormChangeCountInCombat = formChangeAmountInCombat,
            OptionalRoomPercentage = amountOptionalComponentsOnMap != 0 ? optionalComponentsEntered / amountOptionalComponentsOnMap * 100f : 0,
            AverageDistanceToMainPath = averageDistanceToMainPath / distanceCounterRoad,
            AverageDistanceToWall = averageDistanceToWall / distanceCounterWall,
            AverageDistanceToEnemies = averageDistanceToEnemies / distanceCounterEnemy,
            BowDefenseToMelee = defenseToEnemy[0, 0],
            BowDefenseToRanged = defenseToEnemy[0, 1],
            BowDefenseToBomber = defenseToEnemy[0, 2],
            BowDefenseToAssasssin = defenseToEnemy[0, 3],
            BowDefenseToGuardian = defenseToEnemy[0, 4],
            KnightDefenseToMelee = defenseToEnemy[1, 0],
            KnightDefenseToRanged = defenseToEnemy[1, 1],
            KnightDefenseToBomber = defenseToEnemy[1, 2],
            KnightDefenseToAssasssin = defenseToEnemy[1, 3],
            KnightDefenseToGuardian = defenseToEnemy[1, 4],
            BerserkerDefenseToMelee = defenseToEnemy[2, 0],
            BerserkerDefenseToRanged = defenseToEnemy[2, 1],
            BerserkerDefenseToBomber = defenseToEnemy[2, 2],
            BerserkerDefenseToAssasssin = defenseToEnemy[2, 3],
            BerserkerDefenseToGuardian = defenseToEnemy[2, 4]
        };
        telemetrySender.SendTelemetry(telemetryData);

        ResetStats(true);
        
    
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
        formChangeAmount+=1;
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

    public void SetMostRecentAttack(int type)
    {
        mostRecentAttackType = type;
    }

    public void SetBehavior(float[] behaviors)
    {
        mapBehaviors = behaviors;
    }

    public void SetPlayerStats(float attackSpeed, float movementSpeed, float attackDamage)
    {
        AttackSpeedMultiplier = attackSpeed;
        MovementSpeedMultiplier = movementSpeed;
        DamageMultiplier = attackDamage;
    }

    public void ChangedFormInCombat()
    {
        formChangeAmountInCombat +=1;
    }

    public void OptionalComponentEntered()
    {
        optionalComponentsEntered+=1;
    }

    public void SetTotalAmountOfOptionalComponents(int amount)
    {
        amountOptionalComponentsOnMap = amount;
    }

    public void DistanceToPath(float distance)
    {
        averageDistanceToMainPath +=distance;
        distanceCounterRoad += 1;
    }

    public void DistanceToWall(float distance)
    {
        averageDistanceToWall += distance;
        distanceCounterWall +=1;
    }

    public void DistanceToEnemy(float distance)
    {
        averageDistanceToEnemies += distance;
        distanceCounterEnemy +=1;
    }

    public void ResetStats(bool levelCleared)
    {
        timePlayed = 0f;
        CurrentMapScore = 0f;
        TotalMapScore = 0f;
        if (levelCleared)
        {
            playerDeaths = 0;
        }
        averageEnemiesAliveOnLootPickup = 0;
        powerUpsTaken = 0;
        totalEnemies = 0;
        totalLoot = 0;
        lootTaken = 0;
        enemiesKilled = 0;
        timeSpentBowMan = 0f;
        timeSpentBerserker = 0;
        timeSpentKnight = 0;
        bowmanLightAttacks = 0;
        bowmanHeavyAttacks = 0;
        bowmanLightDashes = 0;
        bowmanHeavyDashes = 0;
        bowmanDefense = 0;
        knightLightAttacks = 0;
        knightHeavyAttacks = 0;
        knightLightDashes = 0;
        knightHeavyDashes = 0;
        knightDefense = 0;
        berserkerLightAttacks = 0;
        berserkerHeavyAttacks = 0;
        berserkerLightDashes = 0;
        berserkerHeavyDashes = 0;
        berserkerDefense = 0;
        damageTaken[0] = 0;
        damageTaken[1] = 0;
        damageTaken[2] = 0;
        damageTaken[3] = 0;
        loadoutToEnemy[0,0,0] = 0;
        loadoutToEnemy[0,1,0] = 0;
        loadoutToEnemy[0,2,0] = 0;
        loadoutToEnemy[0,0,1] = 0;
        loadoutToEnemy[0,1,1] = 0;
        loadoutToEnemy[0,2,1] = 0;
        loadoutToEnemy[0,0,2] = 0;
        loadoutToEnemy[0,1,2] = 0;
        loadoutToEnemy[0,2,2] = 0;
        loadoutToEnemy[0,0,3] = 0;
        loadoutToEnemy[0,1,3] = 0;
        loadoutToEnemy[0,2,3] = 0;
        loadoutToEnemy[0,0,4] = 0;
        loadoutToEnemy[0,1,4] = 0;
        loadoutToEnemy[0,2,4] = 0;
        loadoutToEnemy[1,0,0] = 0;
        loadoutToEnemy[1,1,0] = 0;
        loadoutToEnemy[1,2,0] = 0;
        loadoutToEnemy[1,0,1] = 0;
        loadoutToEnemy[1,1,1] = 0;
        loadoutToEnemy[1,2,1] = 0;
        loadoutToEnemy[1,0,2] = 0;
        loadoutToEnemy[1,1,2] = 0;
        loadoutToEnemy[1,2,2] = 0;
        loadoutToEnemy[1,0,3] = 0;
        loadoutToEnemy[1,1,3] = 0;
        loadoutToEnemy[1,2,3] = 0;
        loadoutToEnemy[1,0,4] = 0;
        loadoutToEnemy[1,1,4] = 0;
        loadoutToEnemy[1,2,4] = 0;
        loadoutToEnemy[2,0,0] = 0;
        loadoutToEnemy[2,1,0] = 0;
        loadoutToEnemy[2,2,0] = 0;
        loadoutToEnemy[2,0,1] = 0;
        loadoutToEnemy[2,1,1] = 0;
        loadoutToEnemy[2,2,1] = 0;
        loadoutToEnemy[2,0,2] = 0;
        loadoutToEnemy[2,1,2] = 0;
        loadoutToEnemy[2,2,2] = 0;
        loadoutToEnemy[2,0,3] = 0;
        loadoutToEnemy[2,1,3] = 0;
        loadoutToEnemy[2,2,3] = 0;
        loadoutToEnemy[2,0,4] = 0;
        loadoutToEnemy[2,1,4] = 0;
        loadoutToEnemy[2,2,4] = 0;
        mapBehaviors[0] = 0;
        mapBehaviors[1] = 0;
        mapBehaviors[2] = 0;
        mapBehaviors[3] = 0;
        mapBehaviors[4] = 0;
        AttackSpeedMultiplier = 0;
        MovementSpeedMultiplier = 0;
        DamageMultiplier = 0;
        formChangeAmount = 0;
        formChangeAmountInCombat = 0;
        amountOptionalComponentsOnMap = 0;
        optionalComponentsEntered = 0;
        distanceCounterRoad = 1;
        averageDistanceToMainPath = 0;
        averageDistanceToWall = 0;
        distanceCounterWall = 1;
        averageDistanceToEnemies = 0;
        defenseToEnemy[0,0] = 0;
        defenseToEnemy[0,1] = 0;
        defenseToEnemy[0,2] = 0;
        defenseToEnemy[0,3] = 0;
        defenseToEnemy[0,4] = 0;
        defenseToEnemy[1,0] = 0;
        defenseToEnemy[1,1] = 0;
        defenseToEnemy[1,2] = 0;
        defenseToEnemy[1,3] = 0;
        defenseToEnemy[1,4] = 0;
        defenseToEnemy[2,0] = 0;
        defenseToEnemy[2,1] = 0;
        defenseToEnemy[2,2] = 0;
        defenseToEnemy[2,3] = 0;
        defenseToEnemy[2,4] = 0;
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
}


}


[System.Serializable]
public struct TelemetryData
{
    public string levelPlayID;    
    public string playerId;

    public float timePlayed;
    public float TotalScore;
    public float MapScore;
    public float MapScorePercentage;
    public float enemiesKilledPct;
    public float HealthBarrelsTaken;
    public float PowerUpsTaken;
    public float AvgEnemiesAliveOnPowerUpTaken;
    public float lootTakenPct;
    public float deaths;

    public float bowmanTime;
    public float knightTime;
    public float berserkerTime;

    public int bowLightAtk;
    public int bowHeavyAtk;
    public int bowLightDash;
    public int bowHeavyDash;
    public int bowDefense;

    public int knightLightAtk;
    public int knightHeavyAtk;
    public int knightLightDash;
    public int knightHeavyDash;
    public int knightDefense;

    public int berserkLightAtk;
    public int berserkHeavyAtk;
    public int berserkLightDash;
    public int berserkHeavyDash;
    public int berserkDefense;

    public float damageTakenMelee;

    public float damageTakenRanged;
    public float damageTakenGuardianShield;
    public float damageTakenTraps;
    public int BowLightAttacksMeleeEnemy;
    public int BowHeavyAttacksMeleeEnemy;
    public int BowHeavyDashesMeleeEnemy;
    public int BowLightAttacksRangedEnemy;
    public int BowHeavyAttacksRangedEnemy;
    public int BowHeavyDashesRangedEnemy;
    public int BowLightAttacksBomberEnemy;
    public int BowHeavyAttacksBomberEnemy;
    public int BowHeavyDashesBomberEnemy;
    public int BowLightAttacksAssassinEnemy;
    public int BowHeavyAttacksAssassinEnemy;
    public int BowHeavyDashesAssassinEnemy;
    public int BowLightAttacksGuardianEnemy;
    public int BowHeavyAttacksGuardianEnemy;
    public int BowHeavyDashesGuardianEnemy;
    public int KnightLightAttacksMeleeEnemy;
    public int KnightHeavyAttacksMeleeEnemy;
    public int KnightHeavyDashesMeleeEnemy;
    public int KnightLightAttacksRangedEnemy;
    public int KnightHeavyAttacksRangedEnemy;
    public int KnightHeavyDashesRangedEnemy;
    public int KnightLightAttacksBomberEnemy;
    public int KnightHeavyAttacksBomberEnemy;
    public int KnightHeavyDashesBomberEnemy;
    public int KnightLightAttacksAssassinEnemy;
    public int KnightHeavyAttacksAssassinEnemy;
    public int KnightHeavyDashesAssassinEnemy;
    public int KnightLightAttacksGuardianEnemy;
    public int KnightHeavyAttacksGuardianEnemy;
    public int KnightHeavyDashesGuardianEnemy;
    public int BerserkerLightAttacksMeleeEnemy;
    public int BerserkerHeavyAttacksMeleeEnemy;
    public int BerserkerHeavyDashesMeleeEnemy;
    public int BerserkerLightAttacksRangedEnemy;
    public int BerserkerHeavyAttacksRangedEnemy;
    public int BerserkerHeavyDashesRangedEnemy;
    public int BerserkerLightAttacksBomberEnemy;
    public int BerserkerHeavyAttacksBomberEnemy;
    public int BerserkerHeavyDashesBomberEnemy;
    public int BerserkerLightAttacksAssassinEnemy;
    public int BerserkerHeavyAttacksAssassinEnemy;
    public int BerserkerHeavyDashesAssassinEnemy;
    public int BerserkerLightAttacksGuardianEnemy;
    public int BerserkerHeavyAttacksGuardianEnemy;
    public int BerserkerHeavyDashesGuardianEnemy;
    public float GeometryBehavior;
    public float FurnishingBehaviorSpread;
    public float FurnishingBehaviorRatio;
    public float EnemyBehaviorRatio;
    public float EnemyBehaviorDifficulty;
    public float AttackSpeedMultiplier;
    public float MovementSpeedMultiplier;
    public float DamageMultiplier;
    public int FormChangeCount;
    public int FormChangeCountInCombat;
    public float OptionalRoomPercentage;
    public float AverageDistanceToMainPath;
    public float AverageDistanceToWall;
    public float AverageDistanceToEnemies;
    public int BowDefenseToMelee;
    public int BowDefenseToRanged;
    public int BowDefenseToBomber;
    public int BowDefenseToAssasssin;
    public int BowDefenseToGuardian;
    public int KnightDefenseToMelee;
    public int KnightDefenseToRanged;
    public int KnightDefenseToBomber;
    public int KnightDefenseToAssasssin;
    public int KnightDefenseToGuardian;
    public int BerserkerDefenseToMelee;
    public int BerserkerDefenseToRanged;
    public int BerserkerDefenseToBomber;
    public int BerserkerDefenseToAssasssin;
    public int BerserkerDefenseToGuardian;


}
