using Mono.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace dotnet_azdo_artifacts_multidownload
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			var outputPath = "";
			var runLimit = 1;
			var pat = "";
			var sourceBranch = "";
			var artifactName = "";
			var definitionName = "";        // Build.DefinitionName
			var projectName = "";           // System.TeamProject
			var serverUri = "";             // System.TeamFoundationCollectionUri
			var currentBuild = 0;           // Build.BuildId
			var tags = "";					// Build.BuildId

			var p = new OptionSet() {
					{ "output-path=", s => outputPath = s },
					{ "pat=", s => pat = s },
					{ "run-limit=", s => runLimit = int.Parse(s) },
					{ "source-branch=", s => sourceBranch = s },   // Build.SourceBranch
					{ "artifact-name=", s => artifactName = s },
					{ "definition-name=", s => definitionName = s },
					{ "project-name=", s => projectName = s },
					{ "server-uri=", s => serverUri = s },
					{ "tags=", s => tags = s },
					{ "current-build=", s => currentBuild = int.Parse(s) },
				};

			var list = p.Parse(args);

			if (string.IsNullOrEmpty(outputPath)
				|| string.IsNullOrEmpty(pat)
				|| string.IsNullOrEmpty(sourceBranch)
				|| string.IsNullOrEmpty(artifactName)
				|| string.IsNullOrEmpty(definitionName)
				|| string.IsNullOrEmpty(projectName)
				|| string.IsNullOrEmpty(serverUri)
				)
			{
				Console.WriteLine("dotnet azdo-artifacts-multidownload [options]");
				Console.WriteLine();
				Console.WriteLine("\t--output-path=\t\tThe path where to place downloaded artifacts");
				Console.WriteLine("\t--server-uri=\t\tThe Azure Devops organization url");
				Console.WriteLine("\t--project-name=\t\tThe Azure Devops project name");
				Console.WriteLine("\t--definition-name=\tThe Azure Devops build definition name");
				Console.WriteLine("\t--artifact-name=\tThe Azure Devops build artifact name");
				Console.WriteLine("\t--pat=\t\t\tAn Azure Devops PAT (Usually $(System.AccessToken) when running inside a pipeline)");
				Console.WriteLine("\t--source-branch=\tThe branch to use to find previous builds (Usually $(Build.SourceBranch) when running inside a pipeline)");
				Console.WriteLine("\t--current-build=\tThe first build to look for prior artifacts (Usually $(Build.BuildId) when running inside a pipeline) ");
				Console.WriteLine("\t--run-limit=\t\tThe number of previous artifacts to get (Defaults to 1)");
				Console.WriteLine("\t--tags=\t\t\tAn optional comma separated list of tags to filter builds");

				return 1;
			}
			else
			{
				var downloader = new AzureDevopsDownloader(pat, serverUri);
				await downloader.DownloadArtifacts(basePath: Path.GetFullPath(outputPath),
										  project: projectName,
										  definitionName: definitionName,
										  artifactName: artifactName,
										  sourceBranchName: sourceBranch,
										  tags: tags.Split(","),
										  buildId: currentBuild,
										  downloadLimit: runLimit);

				return 0;
			}
		}
	}
}
