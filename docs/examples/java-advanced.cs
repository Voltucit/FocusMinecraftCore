using FMCore.Java;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var manager = new JavaManager(new() { MaxCandidates = 64, ProbeTimeout = TimeSpan.FromSeconds(5) });
var progress = new Progress<JavaDetectionProgress>(p => Console.WriteLine($"{p.Completed}/{p.Total}: {p.ErrorCode}"));
var result = await manager.DetectAsync(progress, ct);
Console.WriteLine($"找到 {result.Runtimes.Count}，问题 {result.Issues.Count}，截断 {result.IsTruncated}");
