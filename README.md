# Azure DevOps Artifacts Multi-download

This repository is the home for the Azure Azure Devops Artifacts Multi-download which can used as a dotnet CLI tool to download multiple artifacts from a given pipeline.

The original intent for this tool is to ensure that Azure Static WebApps contain multiple versions of the application to ensure a smooth transition between versions of an app.

## Usage

```
dotnet azdo-artifacts-multidownload [options]

        --output-path=          The path where to place downloaded artifacts
        --server-uri=           The Azure Devops organization url
        --project-name=         The Azure Devops project name
        --definition-name=      The Azure Devops build definition name
        --artifact-name=        The Azure Devops build artifact name
        --pat=                  An Azure Devops PAT (Usually $(System.AccessToken) when running inside a pipeline)
        --source-branch=        The branch to use to find previous builds (Usuall $(Build.SourceBranch) when running inside a pipeline)
        --current-build=        The first build to look for prior artifacts (Usually $(Build.BuildId) when running inside a pipeline)
        --run-limit=            The number of previous artifacts to get (Defaults to 1)
        --tags=                 An optional comma separated list of tags to filter builds
```

Example pipeline definition:

```    
- task: DotNetCoreCLI@2
  inputs:
    command: custom
    custom: tool
    arguments: install --tool-path . dotnet-azdo-artifacts-multidownload
  displayName: Install NBGV tool

 - script: azdo-artifacts-multidownload --pat=$(System.AccessToken) --source-branch="$(Build.SourceBranch)" --artifact-name="drop" --definition-name="$(Build.DefinitionName)" --project-name="$(System.TeamProject)" --server-uri="$(System.TeamFoundationCollectionUri)" --current-build="$(Build.BuildId)" --run-limit="2"
   displayName: Set Version
```
