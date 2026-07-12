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

public class AttackRow
{
    public string Name { get; set; } = "Attack";
    public RollMode Mode { get; set; } = RollMode.Normal;
    public int AttackMod { get; set; }
    public int DamageDiceCount { get; set; } = 1;
    public int DamageDieSize { get; set; } = 6;
    public int DamageMod { get; set; }

    /// <summary>How many times this attack is rolled (e.g. 10 for Animate Objects).</summary>
    public int Count { get; set; } = 1;

    [JsonIgnore]
    public List<AttackResult> Results { get; } = [];
}

public class AttackResult
{
    public int D20First { get; set; }
    public int? D20Second { get; set; }
    public int NaturalRoll { get; set; }
    public int AttackTotal { get; set; }
    public bool IsCrit { get; set; }
    public bool IsFumble { get; set; }
    public int[] DamageRolls { get; set; } = [];

    /// <summary>Flat bonus from the Perkins crit rule (maximized dice); 0 otherwise.</summary>
    public int CritBonus { get; set; }

    public int DamageTotal { get; set; }
}
