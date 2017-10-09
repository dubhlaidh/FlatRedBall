﻿using System;
using System.Linq;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public static class ProjectCreator
    {
        public static ProjectBase LoadXnaProjectFor(ProjectBase masterProject, string fileName)
        {
            ProjectBase project = CreateProject(fileName);

            project.Load(fileName);

            project.MasterProjectBase = masterProject;

            project.IsContentProject = true;

            return project;

        }


        public static ProjectBase CreateProject(string fileName)
        {
            //Project coreVisualStudioProject = new Project(fileName);
            Project coreVisualStudioProject;

            try
            {
                try
                {
                    coreVisualStudioProject = new Project(fileName, null, null, new ProjectCollection());
                }
                catch (Microsoft.Build.Exceptions.InvalidProjectFileException exception)
                {
                    // This is a bug I haven't been able to figure out, but have asked about here:
                    // https://stackoverflow.com/questions/46384075/why-does-microsoft-build-framework-dll-15-3-not-load-csproj-15-1-does
                    // So I'm going to hack a fix by checking if this has to do with 15.0 vs the other versions:

                    var message = exception.Message;
                    if(exception.Message.Contains("\"15.0\"") && exception.Message.Contains("\"14.0\""))
                    {
                        coreVisualStudioProject = new Project(fileName, null, "14.0", new ProjectCollection());
                    }
                    else
                    {
                        throw exception;
                    }
                }

            }
            catch (Microsoft.Build.Exceptions.InvalidProjectFileException exception)
            {
                var exceptionMessage = exception.Message;

                bool isMissingMonoGame = exceptionMessage.Contains("MonoGame.Content.Builder.targets\" was not found");
                string message;
                if(isMissingMonoGame)
                {
                    message = $"Could not load the project {fileName}\nbecause MonoGame files are missing. Try installing MonoGame, then try opening the project in Glue again.\n\n";
                }
                else
                {
                    message = $"Could not load the project {fileName}\n" +
                        $"Usually this occurs if the Visual Studio XNA plugin is not installed\n\n";

                }

                throw new Exception(message, exception);
            }

            ProjectBase toReturn = CreatePlatformSpecificProject(coreVisualStudioProject, fileName);

#if GLUE
            // It may be null if the project is of an unknown type.  
            // We'll handle that problem outside of this function.
            if (toReturn != null)
            {
                toReturn.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
                // Saving seems to cause 2 file changes, so we're going to ignore 2, what a hack!
                toReturn.Saving += FlatRedBall.Glue.IO.FileWatchManager.IgnoreNextChangeOnFile;
            }
#endif
            return toReturn;
        }

        public static ProjectBase CreatePlatformSpecificProject(Project coreVisualStudioProject, string fileName)
        {
            ProjectBase toReturn = null;
            if (FileManager.GetExtension(fileName) == "contentproj")
            {
                toReturn = new XnaContentProject(coreVisualStudioProject);
            }

            string errorMessage = null;

            if (toReturn == null)
            {
                toReturn = TryGetProjectTypeFromDefineConstants(coreVisualStudioProject, out errorMessage);
            }


            if (toReturn == null)
            {
                // If we got here that means that the preprocessor defines don't match what
                // Glue expects.  This is probably bad - Glue generated code will likely not
                // compile, so let's warn the user
                EditorObjects.IoC.Container.Get<IGlueCommands>().DialogCommands.ShowMessageBox(errorMessage);

            }

            return toReturn;
        }

        private static ProjectBase TryGetProjectTypeFromDefineConstants(Project coreVisualStudioProject, out string message)
        {
            string preProcessorConstants = GetPreProcessorConstantsFromProject(coreVisualStudioProject);

            //string sasfd = ProjectManager.LibrariesPath;

            // Check for other platforms before checking for FRB_XNA because those projects
            // include FRB_XNA in them

            ProjectBase toReturn = null;

            if (preProcessorConstants.Contains("ANDROID"))
            {
                toReturn = new AndroidProject(coreVisualStudioProject);
            }
            else if(preProcessorConstants.Contains("IOS"))
            {
                toReturn = new IosMonogameProject(coreVisualStudioProject);
            }
            else if(preProcessorConstants.Contains("UWP"))
            {
                toReturn = new UwpProject(coreVisualStudioProject);
            }
            else if(preProcessorConstants.Contains("DESKTOP_GL"))
            {
                toReturn = new DesktopGlProject(coreVisualStudioProject);
            }
            

            // Do XNA_4 last, since every 
            // other project type has this 
            // preprocessor type, so every project
            // type would return true here
            else if (preProcessorConstants.Contains("XNA4"))
            {
                toReturn = new Xna4Project(coreVisualStudioProject);
            }

            message = null;
            if(toReturn == null)
            {
                message = $"Could not determine project type from preprocessor directives. The preprocessor directive string is \"{preProcessorConstants}\"";
            }

            return toReturn;
        }

        public static string GetPreProcessorConstantsFromProject(Project coreVisualStudioProject)
        {
            string preProcessorConstants = "";

            // Victor Chelaru October 20, 2012
            // We used to just look at the XML and had a broad way of determining the 
            // patterns.  I decided it was time to clean this up and make it more precise
            // so now we use the Properties from the project.
            foreach (var property in coreVisualStudioProject.Properties)
            {
                if (property.Name == "DefineConstants")
                {
                    preProcessorConstants += ";" + property.EvaluatedValue;
                }
            }
            return preProcessorConstants;
        }

    }
}
