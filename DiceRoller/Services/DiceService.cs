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

    /// <summary>
    /// Monte Carlo simulation of many turns of a set of attacks (each rolled its × count
    /// per turn). Uses Random.Shared (xoshiro) rather than the crypto RNG: for aggregate
    /// statistics the speed matters and its distribution quality is more than sufficient;
    /// real rolls stay crypto.
    /// </summary>
    public SimulationResult Simulate(IReadOnlyList<AttackRow> attacks, CritRule critRule, int? targetAc, int iterations)
    {
        var rng = Random.Shared;
        var totals = new int[iterations];
        long hits = 0, crits = 0, fumbles = 0;
        int attacksPerTurn = attacks.Sum(r => Math.Clamp(r.Count, 1, 50));

        for (int i = 0; i < iterations; i++)
        {
            int total = 0;
            foreach (var row in attacks)
            {
                int count = Math.Clamp(row.Count, 1, 50);
                for (int a = 0; a < count; a++)
                {
                    int first = rng.Next(1, 21);
                    int natural = row.Mode switch
                    {
                        RollMode.Advantage => Math.Max(first, rng.Next(1, 21)),
                        RollMode.Disadvantage => Math.Min(first, rng.Next(1, 21)),
                        _ => first,
                    };
                    bool isCrit = natural == 20;
                    bool isFumble = natural == 1;
                    if (isCrit) crits++;
                    if (isFumble) fumbles++;

                    bool lands = isCrit
                        || (!isFumble && (targetAc is not int ac || natural + row.AttackMod >= ac));
                    if (lands)
                    {
                        hits++;
                        total += RollDamageFast(rng, row, isCrit, critRule);
                    }
                }
            }
            totals[i] = total;
        }

        double attackCount = (double)iterations * attacksPerTurn;
        int min = totals.Min();
        int max = totals.Max();
        int range = max - min + 1;
        int bucketSize = Math.Max(1, (int)Math.Ceiling(range / 24.0));
        var buckets = new int[(range + bucketSize - 1) / bucketSize];
        foreach (int t in totals)
        {
            buckets[(t - min) / bucketSize]++;
        }

        return new SimulationResult
        {
            Iterations = iterations,
            TargetAc = targetAc,
            HitRate = hits / attackCount,
            CritRate = crits / attackCount,
            FumbleRate = fumbles / attackCount,
            AvgDamage = totals.Average(),
            MinDamage = min,
            MaxDamage = max,
            Buckets = buckets,
            BucketSize = bucketSize,
            MaxBucketCount = buckets.Max(),
        };
    }

    private static int RollDamageFast(Random rng, AttackRow row, bool isCrit, CritRule critRule)
    {
        int multiplier = isCrit && critRule == CritRule.Default ? 2 : 1;
        int sum = row.DamageMod;
        foreach (var group in row.Damage)
        {
            int dice = group.Count * multiplier;
            for (int d = 0; d < dice; d++)
            {
                sum += rng.Next(1, group.Size + 1);
            }
        }
        if (isCrit && critRule == CritRule.Perkins)
        {
            sum += row.Damage.Sum(g => g.Count * g.Size);
        }
        return sum;
    }

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
        int diceMultiplier = isCrit && critRule == CritRule.Default ? 2 : 1;
        var damageGroups = row.Damage
            .Select(g => new DamageGroupResult
            {
                Size = g.Size,
                Rolls = Roll(g.Count * diceMultiplier, g.Size),
            })
            .ToList();
        int critBonus = isCrit && critRule == CritRule.Perkins
            ? row.Damage.Sum(g => g.Count * g.Size)
            : 0;

        return new AttackResult
        {
            D20First = first,
            D20Second = second,
            NaturalRoll = natural,
            AttackTotal = natural + row.AttackMod,
            IsCrit = isCrit,
            IsFumble = isFumble,
            DamageGroups = damageGroups,
            CritBonus = critBonus,
            DamageTotal = damageGroups.Sum(g => g.Rolls.Sum()) + critBonus + row.DamageMod,
        };
    }
}
