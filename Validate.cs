using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Validation;
using Hl7.Fhir.Specification.Source;
using Hl7.FhirPath.Sprache;

class Validate
{
    // default path to jsons folder
    static string jsonsFolderPath = Path.GetDirectoryName(Environment.CurrentDirectory) + "..\\..\\..\\jsons";
    static readonly string lineSeparator = "----------------------------------------------------------------------------------";
    

    static bool printInfo = false;
    static bool printWarn = false;

    static void Main(string[] args)
    {

        ParseArguments(args);

        string[] filePaths = Directory.GetFiles(jsonsFolderPath);

        var resolver = ZipSource.CreateValidationSource();
        var directoryResolver = new DirectorySource("profiles");

        var settings = ValidationSettings.CreateDefault();
        // add support for custom profiles
        settings.ResourceResolver = new CachedResolver(
            new MultiResolver(resolver, directoryResolver)
        );

        var validator = new Validator(settings);

        foreach (string filePath in filePaths)
        {
            Console.WriteLine($"{lineSeparator}\nProcessing file: {filePath}");
            ProcessFile(filePath, validator);
            Console.WriteLine($"Processed file: {filePath}\n{lineSeparator}");
        }
    }
    private static void ProcessFile(string filePath, Validator validator)
    {
        string jsonContent = File.ReadAllText(filePath);

        try
        {
            var parser = new FhirJsonParser();
            var bundle = parser.Parse<Bundle>(jsonContent);

            var outcome = validator.Validate(bundle);
            WriteOutcome(outcome, bundle);
        }
        catch (Exception ex)
        {
            string str = ex switch
            {
                FormatException => $"Json format exception: {ex.Message}",
                _ => "Unexpected exception type: " + ex.GetType().Name + " - " + ex.Message
            };
            Console.WriteLine(str);
        }

    }

    private static void WriteOutcome(OperationOutcome outcome, Resource resource)
    {
        Console.WriteLine($"Validation of resource with id '{resource.Id}' {(outcome.Success ? "is successful" : "has failed:")}");
        if (!outcome.Success)
        {
            outcome.Issue.Where(i =>
            {
                if (i.Severity == OperationOutcome.IssueSeverity.Information && !printInfo)
                    return false;
                if (i.Severity == OperationOutcome.IssueSeverity.Warning && !printWarn)
                    return false;
                return true;
            }).ToList().ForEach(i => Console.WriteLine($"  {i}"));
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

                case "-i":
                case "--info":
                    printInfo = true;
                    break;

                case "-w":
                case "--warning":
                    printWarn = true;
                    break;

                default:
                    Console.WriteLine($"Error: Unknown argument {args[i]}");
                    return;
            }
        }
    }

}