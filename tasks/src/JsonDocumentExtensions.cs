using System.Text.Json;

namespace TodayInDestiny2.Tasks;

internal static class JsonDocumentExtensions
{
    static JsonElement dummyElement = new JsonElement();

    public static bool TryGetPropertyChain(
        this JsonDocument doc, out JsonElement lastElement, params string[] properties)
    {
        JsonElement currentElement = doc.RootElement;
        foreach (var propertyName in properties)
        {
            if (!currentElement.TryGetProperty(propertyName, out currentElement))
            {
                lastElement = dummyElement;
                return false;
            }
        }

        lastElement = currentElement;
        return true;
    }
}