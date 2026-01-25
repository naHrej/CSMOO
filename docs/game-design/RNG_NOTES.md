# RNG and Probability System Notes

## Status

**Current State**: RNG/deterministic aspects are deliberately left blank or use placeholders.

**Future Plan**: Will integrate a probability engine when available.

## Design Philosophy

- Don't design around a system we don't have yet
- Use placeholders or simple implementations for now
- Mark all RNG/probability references as "TBD - Probability Engine"
- Will refactor when probability engine is integrated

## Areas That Will Use Probability Engine

### Combat System
- Weapon hit/miss calculations
- Damage variance
- Critical hits
- Shield effectiveness
- Evasion chances

### Ship Systems
- Component failures
- System malfunctions
- Efficiency variations
- Fuel consumption variance

### Exploration
- Discovery chances
- Resource finding
- Event triggers
- Random encounters

### Trading/Economy
- Price fluctuations
- Market events
- Trade success rates

### Environmental
- Space weather events
- Asteroid encounters
- Anomaly discoveries

## Placeholder Implementation

Until probability engine is integrated, use simple placeholders:

```csharp
// PLACEHOLDER: Will use probability engine when available
public bool CheckProbability(double chance)
{
    // Simple placeholder - replace with probability engine
    return new Random().NextDouble() < chance;
}

// PLACEHOLDER: Will use probability engine when available
public double GetRandomValue(double min, double max)
{
    // Simple placeholder - replace with probability engine
    return min + (new Random().NextDouble() * (max - min));
}
```

## Integration Notes

When probability engine is integrated:
1. Replace all placeholder RNG calls
2. Use probability engine API
3. Ensure deterministic behavior where needed (for replays, etc.)
4. Update all game design documents
5. Test probability distributions

## Marking in Code

All RNG/probability code should be marked:

```csharp
// TODO: Replace with probability engine when available
// PLACEHOLDER: Simple RNG - will use probability engine
```

## Related Documentation

- Update this document when probability engine is integrated
- Mark all game design docs that mention RNG/probability
- Create integration guide when engine is available
