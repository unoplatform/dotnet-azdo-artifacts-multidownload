using Mono.Options;
using System;
using System.Threading.Tasks;

namespace dotnet_azdo_artifacts_multidownload
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var outputPath = "";
			var runLimit = 0;
			var pat = "";
			var sourceBranch = "";
			var targetBranchParam = "";
			var artifactName = "";
			var definitionName = "";        // Build.DefinitionName
			var projectName = "";           // System.TeamProject
			var serverUri = "";             // System.TeamFoundationCollectionUri
			var currentBuild = 0;           // Build.BuildId

			var p = new OptionSet() {
					{ "output-path=", s => outputPath = s },
					{ "pat=", s => pat = s },
					{ "run-limit=", s => runLimit = int.Parse(s) },
					{ "source-branch=", s => sourceBranch = s },		// Build.SourceBranchName	
					{ "target-branch=", s => targetBranchParam = s },   // System.PullRequest.TargetBranch
					{ "artifact-name=", s => artifactName = s },
					{ "definition-name=", s => definitionName = s },
					{ "project-name=", s => projectName = s },
					{ "server-uri=", s => serverUri = s },
					{ "current-build=", s => currentBuild = int.Parse(s) },
				};

			var list = p.Parse(args);

			var targetBranch = !string.IsNullOrEmpty(targetBranchParam) && targetBranchParam != "$(System.PullRequest.TargetBranch)" ? targetBranchParam : sourceBranch;

			var downloader = new AzureDevopsDownloader(pat, serverUri);
			var artifacts = await downloader.DownloadArtifacts(outputPath, projectName, definitionName, artifactName, sourceBranch, targetBranch, currentBuild, runLimit);
		}
	}
}
