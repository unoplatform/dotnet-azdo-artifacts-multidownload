﻿using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace dotnet_azdo_artifacts_multidownload
{
	class AzureDevopsDownloader
	{
		private readonly string DirectorySeparator = Path.DirectorySeparatorChar.ToString();

		private readonly string _pat;
		private readonly string _collectionUri;

		public AzureDevopsDownloader(string pat, string collectionUri)
		{
			_pat = pat;
			_collectionUri = collectionUri;
		}

		public async Task<string[]> DownloadArtifacts(string basePath,
									  string project,
									  string definitionName,
									  string artifactName,
									  string sourceBranchName,
									  string[] tags,
									  int buildId,
									  int downloadLimit)
		{
			Directory.CreateDirectory(basePath);

			var connection = new VssConnection(new Uri(_collectionUri), new VssBasicCredential(string.Empty, _pat));

			var client = await connection.GetClientAsync<BuildHttpClient>();

			if (!sourceBranchName.StartsWith("refs/heads/"))
			{
				sourceBranchName = "refs/heads/" + sourceBranchName;
			}

			Console.WriteLine($"Getting definitions (" +
				$"basePath:{basePath}, project:{project}, definition:{definitionName}, artifact:{artifactName}," +
				$" branch:{sourceBranchName}, buildId:{buildId}, limit:{downloadLimit}, tags:{string.Join(",", tags)})");

			var definitions = await client.GetDefinitionsAsync(project, name: definitionName);

			Console.WriteLine("Getting builds");
			var builds = await client.GetBuildsAsync(
				project,
				definitions: new[] { definitions.First().Id },
				branchName: sourceBranchName,
				top: downloadLimit,
				tagFilters: tags, 
				queryOrder: BuildQueryOrder.FinishTimeDescending,
				statusFilter: BuildStatus.Completed,
				resultFilter: BuildResult.Succeeded);

			var currentBuild = await client.GetBuildAsync(project, buildId);

			var suceededBuilds = builds
				.Distinct(new BuildComparer())
				.OrderBy(b => b.FinishTime)
				.Concat(new[] { currentBuild });

			string BuildArtifactPath(Build build)
				=> Path.Combine(basePath, "artifacts", $@"{build.LastChangedDate:yyyyMMdd-hhmmss}-{build.Id}");

			foreach (var build in suceededBuilds)
			{
				var fullPath = BuildArtifactPath(build);

				if (!Directory.Exists(fullPath))
				{
					var tempFile = Path.GetTempFileName();

					var artifacts = await client.GetArtifactsAsync(project, build.Id);

					if (artifacts.Any(a => a.Name == artifactName))
					{
						Console.WriteLine($"Getting artifact for build {build.Id}");
				
						using var downloadClient = new HttpClient();

						using (var stream = await client.GetArtifactContentZipAsync(project, build.Id, artifactName))
						{
							using (var f = File.OpenWrite(tempFile))
							{
								await stream.CopyToAsync(f);
							}
						}

						Console.WriteLine($"Extracting artifact for build {build.Id}");

						fullPath = fullPath.Replace(
							DirectorySeparator + DirectorySeparator,
							DirectorySeparator);

						using (var archive = ZipFile.OpenRead(tempFile))
						{
							foreach (var entry in archive.Entries)
							{
								var outPath = Path.Combine(fullPath, entry.FullName.Replace("/", DirectorySeparator));

								if (outPath.EndsWith(Path.DirectorySeparatorChar))
								{
									Directory.CreateDirectory(outPath);
								}
								else
								{
									var directoryName = Path.GetDirectoryName(outPath)!;

									if (!Directory.Exists(directoryName))
									{
										Directory.CreateDirectory(directoryName);
									}

									using (var stream = entry.Open())
									{
										using (var outStream = File.OpenWrite(outPath))
										{
											await stream.CopyToAsync(outStream);
										}
									}
								}
							}
						}
					}
					else
					{
						Console.WriteLine($"Skipping download artifact for build {build.Id} (The artifact {artifactName} cannot be found)");
					}
				}
				else
				{
					Console.WriteLine($"Skipping already downloaded build {build.Id} artifacts");
				}
			}

			return suceededBuilds.Select(BuildArtifactPath).ToArray();
		}

		private class BuildComparer : IEqualityComparer<Build>
		{
			public bool Equals(Build? x, Build? y) => x?.Id == y?.Id;
			public int GetHashCode(Build obj) => obj.Id.GetHashCode();
		}
	}
}
