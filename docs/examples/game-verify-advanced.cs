using FMCore.Installer;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var progress = new Progress<InstallationProgress>(p => Console.WriteLine($"{p.Stage}: {p.CompletedFiles}/{p.TotalFiles}"));
var result = await new GameInstaller(".minecraft").VerifyAsync("1.16.5", new() { VerifyNativeFiles = true }, progress, ct);
Console.WriteLine($"完整 {result.IsValid}，原生 {result.NativeFilesChecked}，资源 {result.AssetsChecked}");
