using ConformanceApp;

#if (Framework == net8.0)
Console.WriteLine("legacy framework");
#else
Console.WriteLine("modern framework");
#endif

#if (UseAnalytics)
ConformanceApp.Analytics.Initialize();
#endif
Console.WriteLine("done");
