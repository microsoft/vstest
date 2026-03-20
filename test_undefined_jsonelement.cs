using System;
using System.Text.Json;

class Program
{
    static void Main()
    {
        try
        {
            JsonElement elem = default;
            Console.WriteLine($"default JsonElement - ValueKind: {elem.ValueKind}");
            
            string rawText = elem.GetRawText();
            Console.WriteLine($"GetRawText() returned: '{rawText}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
