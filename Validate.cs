extern alias stu3;
extern alias r4;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

class Validate
{
    static string jsonsFolderPath = Environment.CurrentDirectory + "/jsons";
    static readonly string lineSeparator = "----------------------------------------------------------------------------------";

    static bool useSTU3 = false;
    static bool useR4 = false;
    static bool printConsole = false;

    static void Main(string[] args)
    {
        ParseArguments(args);

        var fhir3Report = new Report();
        var fhir4Report = new Report();
        var filesSource = new FileIterator(jsonsFolderPath);
        
        foreach (FileContent data in filesSource)
        {
            Print($"{lineSeparator}\nProcessing file: {data.FileName}");
            if (useSTU3)
            {
                Print("fhir3:");
                fhir3Report.filesProceed++;
                var errors = ProcessFile(data, stu3::Hl7.Fhir.Model.ModelInfo.ModelInspector);
                if (errors.Count > 0)
                {
                    fhir3Report.filesWithErrors++;
                    fhir3Report.validationResults.Add(data.FileName, errors);
                }
            }
            if (useR4)
            {
                Print("fhir4:");
                fhir4Report.filesProceed++;
                var errors = ProcessFile(data, r4::Hl7.Fhir.Model.ModelInfo.ModelInspector);
                if (errors.Count > 0)
                {
                    fhir4Report.filesWithErrors++;
                    fhir4Report.validationResults.Add(data.FileName, errors);
                }
            }
            Print($"Processed file: {data.FileName}\n{lineSeparator}");
        }

        var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, };

        if (useSTU3)
        {
            string reportFileName = "report_fhir3.json";
            fhir3Report.CountTotalErrors();
            string reportJson = JsonSerializer.Serialize(fhir3Report, jsonSerializerOptions);
            File.WriteAllText(reportFileName, reportJson);
            Console.WriteLine($"Validation report has been generated: {reportFileName}");
        }

        if (useR4)
        {
            string reportFileName = "report_fhir4.json";
            fhir4Report.CountTotalErrors();
            string reportJson = JsonSerializer.Serialize(fhir4Report, jsonSerializerOptions);
            File.WriteAllText(reportFileName, reportJson);
            Console.WriteLine($"Validation report has been generated: {reportFileName}");
        }

    }

    private static List<string> ProcessFile(FileContent file, Hl7.Fhir.Introspection.ModelInspector modelInspector)
    {
        var options = new JsonSerializerOptions().ForFhir(modelInspector);
        string jsonContent = file.Content;

        var validationResults = new List<string>();
        try
        {
            // first step of validation is happenening while deserialization
            Bundle bundle = JsonSerializer.Deserialize<Bundle>(jsonContent, options)!;

            if (bundle != null)
            {
                IEnumerable<ValidationResult> results = bundle.Validate(new ValidationContext(true))!;
                validationResults.AddRange(results.Select(result => result.ErrorMessage).ToList()!);
                PrintResults(results, bundle);
            }
        }
        catch (DeserializationFailedException dex)
        {
            var deserializationErrors = dex.Message.Split([") ("], StringSplitOptions.TrimEntries);
            var length = deserializationErrors.Length;
            // update message format for boundary elements 
            if (length > 0)
            {
                deserializationErrors[0] = deserializationErrors[0].Substring(deserializationErrors[0].IndexOf('(') + 1);
                deserializationErrors[length - 1] = deserializationErrors[length - 1].Substring(0, deserializationErrors[length - 1].IndexOf(')'));
            }
            validationResults.AddRange(deserializationErrors);
            Print(string.Join(Environment.NewLine, deserializationErrors));
        }
        catch (Exception ex)
        {
            var errorMessage = "Unexpected exception type: " + ex.GetType().Name + " - " + ex.Message;
            validationResults.Add(errorMessage);
            Print(errorMessage);
        }
        return validationResults;
    }

    private static void PrintResults(IEnumerable<ValidationResult> results, Hl7.Fhir.Model.Resource resource)
    {
        if (printConsole)
        {
            if (results.Any())
            {
                results.ToList().ForEach(r => Print($"  {r} {r.MemberNames} {r.GetType} {r.ErrorMessage}"));
            }
            else
            {
                Print($" - Resource with id '{resource.Id}' validated successfully");
            }
        }
    }

    private static void Print(string message)
    {
        if (printConsole)
            Console.WriteLine(message);
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
                case "-p":
                case "--print":
                    printConsole = true;
                    break;
                default:
                    Console.WriteLine($"Error: Unknown argument {args[i]}");
                    return;
            }
        }
    }

}