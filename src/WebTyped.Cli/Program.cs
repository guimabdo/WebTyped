﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.FileSystemGlobbing;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebTyped.Cli {

	class Program {

		const string CONFIG_FILE_NAME = "webtyped.json";
		//List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
		static async Task<int> Main(string[] args) {

            if (args.Any())
            {
                if (args[0] == "watch")
                {
                    IDisposable subscriber = null;

                    //Watch config file
                    var configWatcher = new FileSystemWatcher("./", CONFIG_FILE_NAME)
                    {
                        //var configWatcher = new FileSystemWatcher(Directory.GetCurrentDirectory()) {
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = false,
                    };
                    //Console.WriteLine("Watching " + Directory.GetCurrentDirectory());
                    var configObservables = new List<IObservable<EventPattern<FileSystemEventArgs>>>();
                    configObservables.Add(Observable
                            .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                h => configWatcher.Changed += h,
                                h => configWatcher.Changed -= h));
                    configObservables.Add(Observable
                            .FromEventPattern<RenamedEventHandler, FileSystemEventArgs>(
                                h => configWatcher.Renamed += h,
                                h => configWatcher.Renamed -= h));
                    configObservables.Add(Observable
                            .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                h => configWatcher.Deleted += h,
                                h => configWatcher.Deleted -= h));
                    configObservables.Add(Observable
                            .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                                h => configWatcher.Created += h,
                                h => configWatcher.Created -= h));
                    Observable.Merge(configObservables)
                        .StartWith(new List<EventPattern<FileSystemEventArgs>>() { null })
                        .Throttle(new TimeSpan(0, 0, 1))
                        .Subscribe(async e =>
                        {
                            if (e == null)
                            {
                                Console.WriteLine($" \u001b[1;35m WebTyped is watching\u001b[0m");
                            }
                            else
                            {
                                Console.WriteLine($" \u001b[1;35m Config file has changed\u001b[0m");
                            }

                            if (subscriber != null)
                            {
                                subscriber.Dispose();
                            }
                            //return;
                            Config config;
                            try
                            {
                                config = await ReadConfigAsync();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(" \u001b[31mCould not read config file: \u001b[0m" + ex.Message);
                                return;
                            }
                            if (config == null)
                            {
                                Console.WriteLine(" \u001b[31mCould not read config file\u001b[0m");
                                return;
                            }
                            var matcher = new Matcher();
                            foreach (var val in config.Files)
                            {
                                matcher.AddInclude(val);
                            }

                            if (config.Assemblies != null)
                            {
                                foreach (var ass in config.Assemblies)
                                {
                                    matcher.AddInclude(ass);
                                }
                            }

                            string lastCheck = "";
                            //Observable.Timeout()
                            subscriber = Observable
                                .Interval(new TimeSpan(0, 0, 1))
                                .Subscribe(async t =>
                                {
                                    var csFiles = matcher.GetResultsInFullPath("./");
                                    var sb = new StringBuilder();
                                    foreach (var f in csFiles)
                                    {
                                        var fi = new FileInfo(f);
                                        sb.Append(f);
                                        sb.Append(fi.LastWriteTimeUtc);
                                    }
                                    string check = sb.ToString();
                                    if (check != lastCheck)
                                    {
                                        if (lastCheck != "")
                                        {
                                            Console.WriteLine(" Files changed.");
                                        }
                                        lastCheck = check;
                                        await Execute();
                                    }
                                });
                        });

                    //await Execute();
                    while (true) { }
                }

                //if (args[0] == "abstractions")
                //{
                //    return await Execute(async g => {
                //        var abstractions = await g.GenerateAbstractionsAsync();
                //        var json = JsonConvert.SerializeObject(abstractions, Formatting.Indented, new JsonSerializerSettings { 
                //            ContractResolver = new CamelCasePropertyNamesContractResolver()
                //        });

                //        Console.WriteLine(json);
                //     });
                //}

                //if (args[0] == "generate-with")
                //{
                //    return await Execute(async g => {
                //        var abstractions = await g.GenerateAbstractionsAsync();
                        
                //        var json = JsonConvert.SerializeObject(abstractions, new JsonSerializerSettings {
                //            ContractResolver = new CamelCasePropertyNamesContractResolver()
                //        });
                //        var tempDir = Path.GetTempPath();
                //        var filePath = Path.Combine(tempDir, "webtyped-abstractions.json");
                //        File.WriteAllText(filePath, json);
                //        Console.WriteLine("Abstractions=>" + filePath);
                //        //Console.WriteLine(Directory.GetCurrentDirectory());
                //    });
                //}

                Console.WriteLine($"Unknown argument {args[0]}");
                return 1;
            }
			if (!File.Exists(CONFIG_FILE_NAME)) {
				Console.WriteLine($"WebTyped configuration file not found (\u001b[31m{CONFIG_FILE_NAME}\u001b[0m)");
				return 1;
			}
			return await Execute();
		}

		private static void Watcher_Changed(object sender, FileSystemEventArgs e) {
			throw new NotImplementedException();
		}

		static async Task<Config> ReadConfigAsync() {
			var configText = await File.ReadAllTextAsync(CONFIG_FILE_NAME);
			var config = JsonConvert.DeserializeObject<Config>(configText);
			return config;
		}
		static async Task<HashSet<string>> GetFilePathsAsync() {
			var config = await ReadConfigAsync();
			var matcher = new Matcher();
			foreach (var val in config.Files) {
				matcher.AddInclude(val);
			}
			return matcher.GetResultsInFullPath(Directory.GetCurrentDirectory()).ToHashSet();
		}

		static async Task<int> Execute(/*Func<Generator,Task> func = null*/) {
            //if(func == null)
            //{
            //    func = g => g.WriteFilesAsync();
            //}
			var dtInit = DateTime.Now;
			var config = await ReadConfigAsync();
			if (config.Files == null || !config.Files.Any()) {
				Console.WriteLine("Source files not provided.");
				return 1;
			}
			if (config.OutDir == null) {
				config.OutDir = "webtyped";
			}
            //Find cs files
			var matcher = new Matcher();
			foreach (var val in config.Files) {
				matcher.AddInclude(val);
			}
			var csFiles = matcher.GetResultsInFullPath(Directory.GetCurrentDirectory());

            //Find dll files
            var assemblyMatcher = new Matcher();
            if (config.Assemblies != null)
            {
                foreach (var ass in config.Assemblies)
                {
                    assemblyMatcher.AddInclude(ass);
                }
            }
            var dllFiles = assemblyMatcher.GetResultsInFullPath(Directory.GetCurrentDirectory());
            
            Console.WriteLine();
			Console.WriteLine($" \u001b[1;36mWebTyped is processing files. Configs:\u001b[0m");
			config.GetType().GetProperties().ToList().ForEach(p => {
				var val = p.GetValue(config);
				Console.Write($"  \u001b[1;33m{p.Name}:");
				if (val is IEnumerable<string>) {
					Console.WriteLine();
					foreach (var x in (val as IEnumerable)) {
						Console.WriteLine($" \u001b[37m {x}");
					}
				} else {
					var strVal = "";
					if (val != null && val.GetType().IsEnum) {
						strVal = val.ToString();
					} else {
						strVal = JsonConvert.SerializeObject(p.GetValue(config));
					}

					Console.WriteLine($" \u001b[37m {strVal}");
				}
			});
			Console.WriteLine("\u001b[0m");


			var trees = new ConcurrentDictionary<string, SyntaxTree>();
			var tasks = new List<Task>();
			foreach (var csFile in csFiles) {
				tasks.Add(File.ReadAllTextAsync(csFile)
					.ContinueWith(tsk => {
						trees.TryAdd(csFile, CSharpSyntaxTree.ParseText(tsk.Result));
					}));
			}

			var options = config.ToOptions();

			foreach (var task in tasks) { await task; }
			var gen = new Generator(trees.Values, 
                config.Assemblies,
                config.Packages,
                config.ReferenceTypes,
                options);
            if (string.IsNullOrWhiteSpace(config.Generator))
            {
                await gen.WriteFilesAsync();
            }
            else
            {
                Console.WriteLine($" \u001b[37m Generating abstractions");
                var abstractions = await gen.GenerateAbstractionsAsync();

                var json = JsonConvert.SerializeObject(abstractions, new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                File.WriteAllText(".webtyped-abstractions", json);
                //var tempDir = Path.GetTempPath();
                //var filePath = Path.Combine(tempDir, "webtyped-abstractions.json");
                //File.WriteAllText(filePath, json);
                //Console.WriteLine("Abstractions=>" + filePath);
                //Console.WriteLine(Directory.GetCurrentDirectory());
            }
            //await func(gen);
            var dtEnd = DateTime.Now;
			Console.WriteLine($"Time: {Math.Truncate((dtEnd - dtInit).TotalMilliseconds)}ms");
			return 0;
		}
	}

	
}
