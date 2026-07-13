using System.Text.Json.Serialization;

namespace DiceRoller.Models;

public enum RollMode
{
    Normal,
    Advantage,
    Disadvantage,
}

public enum CritRule
{
    /// <summary>RAW 5e: roll all damage dice twice, add the modifier once.</summary>
    Default,

    /// <summary>Perkins house rule: normal damage roll + maximized damage dice + modifier.</summary>
    Perkins,
}

/// <summary>One group of identical damage dice, e.g. 2d8.</summary>
public class DamageDice
{
    public int Count { get; set; } = 1;
    public int Size { get; set; } = 6;
}

public class AttackRow
{
    public string Name { get; set; } = "Attack";
    public RollMode Mode { get; set; } = RollMode.Normal;
    public int AttackMod { get; set; }

    /// <summary>Damage dice groups, e.g. [1d6, 2d8]; always at least one.</summary>
    public List<DamageDice> Damage { get; set; } = [new()];

    public int DamageMod { get; set; }

    // Set-only: lets presets saved before multi-dice damage deserialize into the first group.
    public int DamageDiceCount { set => Damage[0].Count = value; }
    public int DamageDieSize { set => Damage[0].Size = value; }

    /// <summary>How many times this attack is rolled (e.g. 10 for Animate Objects).</summary>
    public int Count { get; set; } = 1;

    [JsonIgnore]
    public List<AttackResult> Results { get; } = [];

    [JsonIgnore]
    public SimulationResult? Sim { get; set; }
}

/// <summary>Aggregate stats from simulating many turns of one attack row.</summary>
public class SimulationResult
{
    public int Iterations { get; set; }
    public int? TargetAc { get; set; }
    public double HitRate { get; set; }
    public double CritRate { get; set; }
    public double FumbleRate { get; set; }

    /// <summary>Damage per turn = all attacks of the row (its × count) in one iteration.</summary>
    public double AvgDamage { get; set; }

    public int MinDamage { get; set; }
    public int MaxDamage { get; set; }

    /// <summary>Damage histogram: Buckets[i] counts totals in [MinDamage + i*BucketSize, +BucketSize).</summary>
    public int[] Buckets { get; set; } = [];

    public int BucketSize { get; set; } = 1;
    public int MaxBucketCount { get; set; }
}

public class AttackResult
{
    public int D20First { get; set; }
    public int? D20Second { get; set; }
    public int NaturalRoll { get; set; }
    public int AttackTotal { get; set; }
    public bool IsCrit { get; set; }
    public bool IsFumble { get; set; }
    public List<DamageGroupResult> DamageGroups { get; set; } = [];

    /// <summary>Flat bonus from the Perkins crit rule (maximized dice); 0 otherwise.</summary>
    public int CritBonus { get; set; }

    public int DamageTotal { get; set; }
}

/// <summary>Rolled values for one damage dice group.</summary>
public class DamageGroupResult
{
    public int Size { get; set; }
    public int[] Rolls { get; set; } = [];
}
