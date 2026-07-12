using System.Security.Cryptography;
using DiceRoller.Models;

namespace DiceRoller.Services;

/// <summary>
/// All rolls use <see cref="RandomNumberGenerator.GetInt32(int, int)"/> — a cryptographically
/// secure RNG with internal rejection sampling, so every face is exactly equally likely
/// (no modulo bias, no seed-based predictability like System.Random).
/// </summary>
public class DiceService
{
    public int Roll(int sides) => RandomNumberGenerator.GetInt32(1, sides + 1);

    public int[] Roll(int count, int sides) =>
        Enumerable.Range(0, count).Select(_ => Roll(sides)).ToArray();

    public AttackResult ResolveAttack(AttackRow row, CritRule critRule = CritRule.Default)
    {
        int first = Roll(20);
        int? second = row.Mode == RollMode.Normal ? null : Roll(20);

        int natural = row.Mode switch
        {
            RollMode.Advantage => Math.Max(first, second!.Value),
            RollMode.Disadvantage => Math.Min(first, second!.Value),
            _ => first,
        };

        bool isCrit = natural == 20;
        bool isFumble = natural == 1;

        // Default (RAW): crit doubles the number of damage dice, modifier added once.
        // Perkins: crit adds the maximized damage dice on top of a normal roll instead.
        int diceCount = isCrit && critRule == CritRule.Default
            ? row.DamageDiceCount * 2
            : row.DamageDiceCount;
        int[] damageRolls = Roll(diceCount, row.DamageDieSize);
        int critBonus = isCrit && critRule == CritRule.Perkins
            ? row.DamageDiceCount * row.DamageDieSize
            : 0;

        return new AttackResult
        {
            D20First = first,
            D20Second = second,
            NaturalRoll = natural,
            AttackTotal = natural + row.AttackMod,
            IsCrit = isCrit,
            IsFumble = isFumble,
            DamageRolls = damageRolls,
            CritBonus = critBonus,
            DamageTotal = damageRolls.Sum() + critBonus + row.DamageMod,
        };
    }
}
