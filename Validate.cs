using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

class Validate
{
    // default path to jsons folder
    static string jsonsFolderPath = Path.GetDirectoryName(Environment.CurrentDirectory) + "..\\..\\..\\jsons";
    static readonly string lineSeparator = "----------------------------------------------------------------------------------";

    static void Main(string[] args)
    {

        ParseArguments(args);

        string[] filePaths = Directory.GetFiles(jsonsFolderPath);

        foreach (string filePath in filePaths)
        {
            Console.WriteLine($"{lineSeparator}\nProcessing file: {filePath}");
            ProcessFile(filePath);
            Console.WriteLine($"Processed file: {filePath}\n{lineSeparator}");
        }
    }
    private static void ProcessFile(string filePath)
    {
        var options = new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);
        string jsonContent = File.ReadAllText(filePath);
        try
        {
            // first step of validation is happenening while deserialization
            Bundle bundle = JsonSerializer.Deserialize<Bundle>(jsonContent, options)!;

            if (bundle != null) {
                IEnumerable<ValidationResult> results = bundle.Validate(new ValidationContext(true))!;
                WriteOutcome(results, bundle);
            }
        }
        catch (Exception ex)
        {
            string str = ex switch
            {
                DeserializationFailedException => $"Deserialization exception:\n{ex.Message}",
                _ => "Unexpected exception type: " + ex.GetType().Name + " - " + ex.Message
            };
            Console.WriteLine(str);
        }

    }

    private static void WriteOutcome(IEnumerable<ValidationResult> results, Resource resource)
    {
        if (results.Any())
        {
            results.ToList().ForEach(r => Console.WriteLine($"  {r} {r.MemberNames} {r.GetType} {r.ErrorMessage}"));
        } else {
            Console.WriteLine($" - Resource with id '{resource.Id}' validated successfully");
        }
    }

    private static void ParseArguments(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-d":
                case "--directory":
                    if (i + 1 < args.Length)
                    {
                        jsonsFolderPath = args[i + 1];
                        i++;
                    }
                    else
                    {
                        Console.WriteLine("Error: Directory path not provided after -d or --directory flag.");
                        return;
                    }
                    break;

                default:
                    Console.WriteLine($"Error: Unknown argument {args[i]}");
                    return;
            }
        }
    }

}