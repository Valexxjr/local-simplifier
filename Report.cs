class Report
{
    public int filesProceed = 0;
    public int filesWithErrors = 0;

    public int totalErrorCount;

    public Dictionary<string, List<string>> validationResults = [];

    public void CountTotalErrors()
    {
        totalErrorCount = validationResults.SelectMany(kv => kv.Value).Count();
    }

}