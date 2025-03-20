public static class RandomUtilities
{
    public static readonly Random Random = Random.Shared;

    public static bool GetRandomBool()
    {
        return Random.Next(0, 2) == 0;
    }

    public static T GetRandomElement<T>(params (T Item, int Value)[] items)
    {
        var total = items.Sum(x => x.Value);
        var randomValue = Random.Next(1, total + 1);

        foreach (var (Item, Value) in items)
        {
            randomValue -= Value;

            if (randomValue <= 0)
                return Item;
        }

        return items[0].Item;
    }
}