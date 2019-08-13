﻿using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynaHub.ViewModels
{
    /// <summary>
    /// The class holds all the info on the GH repo being browsed
    /// </summary>
    class GitHubInfo
    {
        internal static bool gotRepos = false;

        // Get all repositories of user [central storage]
        internal static async Task<IReadOnlyList<Repository>> GetUserReposAsync()
        {
            // Get users repos from GH
            IReadOnlyList<Repository> userRepos =
                await GitHubConnection.client.Repository.GetAllForCurrent();

            gotRepos = true;

            return userRepos;
        }

        // Initialise selection variable
        internal static Repository selectedRepo;

        // contents of the repo highest level
        internal static List<RepositoryContent> repoLevel = new List<RepositoryContent>();

        // List for all folders in repo to be queried
        internal static List<string> repoFolders = new List<string>();

        // Dictionary with both repo path and download_url
        internal static SortedDictionary<string, string> repoFiles =
            new SortedDictionary<string, string>();

        private static long repositoryID;

        // Recursive
        internal static async Task GetDynsFromFolders(RepositoryContent repoContent)
        {
            if (repoContent.Name.EndsWith(".dyn"))
            {
                repoFiles.Add(repoContent.Path, repoContent.DownloadUrl);
            }
            else if (repoContent.Type == "dir")
            {
                string folderPath = repoContent.Path;

                // Get from specific path in repo
                IReadOnlyList<RepositoryContent> subFolders =
                    await GitHubConnection.client.Repository.Content.GetAllContents(
                        repositoryID, folderPath);

                foreach (RepositoryContent content in subFolders)
                {
                    await GetDynsFromFolders(content);
                }
            }
        }

        internal static async Task<SortedDictionary<string, string>> GetRepoContentAsync(
            Repository repository, string lookingFor)
        {
            // Clear lists not to repeat if user changes selection
            ClearPrevious();

            repositoryID = repository.Id;

            // Get everything in repo at higher level of hierarchy
            IReadOnlyList<RepositoryContent> allContent =
                await GitHubConnection.client.Repository.Content.GetAllContents(repositoryID);

            foreach (RepositoryContent content in allContent)
            {
                if (content.Name.EndsWith(lookingFor))
                {
                    repoFiles.Add(content.Path, content.DownloadUrl);
                }
                else if (content.Type == "dir")
                {
                    await GetDynsFromFolders(content);
                }
            }

            return repoFiles;
        }

        private static void ClearPrevious()
        {
            repoLevel.Clear();
            repoFolders.Clear();
            repoFiles.Clear();
        }
    }
}
