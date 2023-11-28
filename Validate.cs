extern alias stu3;
extern alias r4;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

class Validate
{
    // default path to jsons folder
    static string jsonsFolderPath = Path.GetDirectoryName(Environment.CurrentDirectory) + "..\\..\\..\\jsons";
    static readonly string lineSeparator = "----------------------------------------------------------------------------------";

    static bool useSTU3 = false;
    static bool useR4 = false;

    static void Main(string[] args)
    {

        ParseArguments(args);

        string[] filePaths = Directory.GetFiles(jsonsFolderPath);

        foreach (string filePath in filePaths)
        {
            Console.WriteLine($"{lineSeparator}\nProcessing file: {filePath}");
            if (useSTU3) {
                Console.WriteLine("fhir3:");
                ProcessFile(filePath, stu3::Hl7.Fhir.Model.ModelInfo.ModelInspector);
            }
            if (useR4) {
                Console.WriteLine("fhir4:");
                ProcessFile(filePath, r4::Hl7.Fhir.Model.ModelInfo.ModelInspector);
            }
            Console.WriteLine($"Processed file: {filePath}\n{lineSeparator}");
        }
    }
    private static void ProcessFile(string filePath, Hl7.Fhir.Introspection.ModelInspector modelInspector)
    {
        var options = new JsonSerializerOptions().ForFhir(modelInspector);
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
                DeserializationFailedException => $"Deserialization exception:\n{FormatDeserilizationExceptionMessage(ex.Message)}",
                _ => "Unexpected exception type: " + ex.GetType().Name + " - " + ex.Message
            };
            Console.WriteLine(str);
        }

    }

    private static void WriteOutcome(IEnumerable<ValidationResult> results, Hl7.Fhir.Model.Resource resource)
    {
        if (results.Any())
        {
            results.ToList().ForEach(r => Console.WriteLine($"  {r} {r.MemberNames} {r.GetType} {r.ErrorMessage}"));
        } else {
            Console.WriteLine($" - Resource with id '{resource.Id}' validated successfully");
        }
    }

    private static string FormatDeserilizationExceptionMessage(string message) {
        string[] errorParts = message.Split([") ("], StringSplitOptions.None);

        // Format each part with parentheses and new line
        return string.Join(Environment.NewLine, errorParts.Select(part => $"({part})"));
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
                case "-stu3":
                case "--fhir3":
                    useSTU3 = true;
                    break;
                case "-r4":
                case "--fhir4":
                    useR4 = true;
                    break;
                default:
                    Console.WriteLine($"Error: Unknown argument {args[i]}");
                    return;
            }
        }
    }

}