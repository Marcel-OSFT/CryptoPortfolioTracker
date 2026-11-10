using System.Collections.ObjectModel;
using System.Linq;

public static class CollectionReorderHelper
{
    public async static void ApplyOrderedSequence<T>(ObservableCollection<T> collection, IEnumerable<T> newOrderedSequence,  Func<T, object> keySelector)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        var target = newOrderedSequence.ToList();
        var current = collection.ToList();

        // 1. Remove items that are no longer present
        var currentKeys = current.Select(keySelector).ToHashSet();
        var targetKeys = target.Select(keySelector).ToHashSet();

        foreach (var item in current.Where(x => !targetKeys.Contains(keySelector(x))).ToList())
            collection.Remove(item);

        // 2. Ensure all items exist in the collection
        foreach (var item in target.Where(x => !currentKeys.Contains(keySelector(x))).ToList())
            collection.Add(item);

        // 3. Reorder by Move only when the item changed index
        var orderedIds = target.Select(keySelector).ToList();
        var currentIds = collection.Select(keySelector).ToList();

        for (int i = 0; i < orderedIds.Count; i++)
        {
            var expectedKey = orderedIds[i];
            var item = collection.First(x => keySelector(x).Equals(expectedKey));
            var currentIndex = collection.IndexOf(item);

            if (currentIndex != i)
            {
                //collection.Move(currentIndex, i); // move gives a full repaint of the list, so we will do a remove and insert instead to avoid that

                collection.RemoveAt(currentIndex);

                await Task.Delay(200);
                collection.Insert(i < currentIndex ? i : i - 1, item);

            }
        }
    }
}