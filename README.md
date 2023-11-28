# Local Simplifier
From project location build project with
```dotnet build```

Then 
```dotnet run```

You can provide args to run command. Available args:
- ```-d, -directory``` provide path to directory with bundle input jsons
- ```-stu3, -fhir3``` enable _stu3_ version validation
- ```-r4, -fhir4``` enable _r4_ version validation

```/jsons``` folder is convenient to use for bundles, it contains several examples
```/profiles``` folder is used to store validation profiles. I included validation profile for bundle

Project can be run simply via terminal

For code changes I will recommend Visual Studio Code, fill Visual Studio for running this is overcomplicated
