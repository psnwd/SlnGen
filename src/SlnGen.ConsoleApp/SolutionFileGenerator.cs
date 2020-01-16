// Copyright (c) Jeff Kluge.
//
// Licensed under the MIT license.

using Microsoft.Build.Definition;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Evaluation.Context;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using Microsoft.Build.Logging;
using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SlnGen.ConsoleApp
{
    public sealed class SolutionFileGenerator : IDisposable
    {
        private static readonly ProjectLoadSettings DefaultProjectLoadSettings =
            ProjectLoadSettings.IgnoreEmptyImports
            | ProjectLoadSettings.IgnoreInvalidImports
            | ProjectLoadSettings.IgnoreMissingImports
            | ProjectLoadSettings.DoNotEvaluateElementsWithFalseCondition;

        private static readonly EvaluationContext SharedEvaluationContext = EvaluationContext.Create(EvaluationContext.SharingPolicy.Shared);
        private readonly LoggingService _loggingService;
        private readonly ProgramArguments _programArguments;
        private readonly ProjectCollection _projectCollection;

        private SolutionFileGenerator(ProgramArguments programArguments)
        {
            _programArguments = programArguments ?? throw new ArgumentNullException(nameof(programArguments));

            _projectCollection = new ProjectCollection(
                globalProperties: null,
                loggers: GetLoggers(),
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
                onlyLogCriticalEvents: false,
                loadProjectsReadOnly: true);

            _loggingService = new LoggingService(_projectCollection);
        }

        public static int Generate(ProgramArguments programArguments)
        {
            using (SolutionFileGenerator solutionFileGenerator = new SolutionFileGenerator(programArguments))
            {
                return solutionFileGenerator.Generate();
            }
        }

        public void Dispose()
        {
            _loggingService?.Dispose();

            _projectCollection?.Dispose();
        }

        private ProjectInstance CreateProjectInstance(string projectFullPath, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            return Project.FromFile(
                    projectFullPath,
                    new ProjectOptions
                    {
                        EvaluationContext = SharedEvaluationContext,
                        GlobalProperties = globalProperties,
                        LoadSettings = DefaultProjectLoadSettings,
                        ProjectCollection = projectCollection,
                    })
                .CreateProjectInstance(
                    ProjectInstanceSettings.ImmutableWithFastItemLookup,
                    SharedEvaluationContext);
        }

        private int Generate()
        {
            _loggingService.LogMessageHigh("Loading project references...");

            var sw = Stopwatch.StartNew();

            IDictionary<string, string> globalProperties = new Dictionary<string, string>
            {
                ["ExcludeRestorePackageImports"] = "true",
                ["DesignTimeBuild"] = "true",
                ["BuildingProject"] = "false",
                ["Platform"] = "x64",
            };

            ICollection<ProjectGraphEntryPoint> entryProjects = _programArguments.GetProjects(_loggingService).Select(i => new ProjectGraphEntryPoint(i, globalProperties)).ToList();

            _ = new ProjectGraph(entryProjects, _projectCollection, CreateProjectInstance);

            sw.Stop();

            _loggingService.LogMessageNormal($"Loaded {_projectCollection.LoadedProjects.Count} project(s) in {sw.ElapsedMilliseconds:N2}ms");

            Project project = _projectCollection.LoadedProjects.First();

            string solutionFileFullPath = SlnGenUtility.GenerateSolutionFile(
                _projectCollection,
                solutionFileFullPath: null,
                projectFileFullPath: entryProjects.First().ProjectFile,
                customProjectTypeGuids: SlnProject.GetCustomProjectTypeGuids(project.GetItems("SlnGenCustomProjectTypeGuid").Select(i => new MSBuildProjectItem(i))),
                folders: _programArguments.Folders,
                solutionItems: SlnFile.GetSolutionItems(project.GetItems("SlnGenSolutionItem").Select(i => new MSBuildProjectItem(i)), _loggingService),
                logger: _loggingService);

            if (_programArguments.LaunchVisualStudio)
            {
                SlnGenUtility.LaunchVisualStudio(_programArguments.DevEnvFullPath, _programArguments.UseShellExecute, solutionFileFullPath, _loggingService);
            }

            return _loggingService.HasLoggedErrors ? 1 : 0;
        }

        private IEnumerable<ILogger> GetLoggers()
        {
            if (_programArguments.ConsoleLoggerParameters.IsNullOrWhitespace())
            {
                yield return new ConsoleLogger
                {
                    Parameters = "Verbosity=Normal;ForceNoAlign=true;Summary",
                };
            }
            else
            {
                yield return new ConsoleLogger
                {
                    Parameters = _programArguments.ConsoleLoggerParameters,
                };
            }

            if (!_programArguments.FileLoggerParameters.IsNullOrWhitespace())
            {
                yield return new FileLogger
                {
                    Parameters = _programArguments.FileLoggerParameters.Contains("LogFile", StringComparison.OrdinalIgnoreCase) ? _programArguments.FileLoggerParameters : $"LogFile=slngen.log;{_programArguments.FileLoggerParameters}",
                };
            }

            if (!_programArguments.BinaryLogger.IsNullOrWhitespace())
            {
                yield return new BinaryLogger
                {
                    Parameters = _programArguments.BinaryLogger.Contains("LogFile", StringComparison.OrdinalIgnoreCase) ? _programArguments.BinaryLogger : $"LogFile=slngen.binlog;{_programArguments.FileLoggerParameters}",
                };
            }
        }
    }
}