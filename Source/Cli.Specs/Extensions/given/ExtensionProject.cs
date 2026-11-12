// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions;

/// <summary>
/// A temporary project with a model that references an extension, a lock, an allow-list and a cache, for specifications.
/// </summary>
sealed class ExtensionProject : IDisposable
{
    public ExtensionProject(string reference = "cratis/stripe", string? version = "2")
    {
        Root = Path.Combine(Path.GetTempPath(), "cratis-extension-" + Guid.NewGuid().ToString("N"));
        Model = Path.Combine(Root, "Model");
        Directory.CreateDirectory(Path.Combine(Root, ".cratis"));
        Directory.CreateDirectory(Model);
        File.WriteAllText(Path.Combine(Model, "payments.play"), ModelText(reference, version));
        Cache = new(Path.Combine(Root, "cache"));
        Registry = new();
    }

    static string ModelText(string reference, string? version) => string.Join(
        '\n',
        [
            "concept Money : String",
            "",
            "system Payments",
            $"  extension \"{reference}\"{(version is null ? string.Empty : $" version {version}")}",
            "",
            "module M",
            "  feature F",
            "    slice Automation S",
            "      event Settled",
            "        paymentIntentId String",
            "      reaction Settle",
            "        when Payments.PaymentSucceeded",
            "          paymentIntentId",
            "          produces Settled",
            "            for paymentIntentId",
            "            paymentIntentId = paymentIntentId",
            ""
        ]);

    public string Root { get; }

    public string Model { get; }

    public ExtensionCache Cache { get; }

    public InMemoryExtensionRegistry Registry { get; }

    public string LockPath => Path.Combine(Model, ExtensionLock.FileName);

    public string LockText => File.Exists(LockPath) ? File.ReadAllText(LockPath) : string.Empty;

    public void AllowPublisher(string publisher = "https://github.com/cratis") => new ExtensionAllowList([]).Add(Root, new(publisher, ExtensionPackageFactory.FingerprintOf(publisher)));

    public void Lock(string range, RegistryPackage package, string reference = "cratis/stripe") =>
        File.WriteAllText(LockPath, new ExtensionLock([new(reference, range, package.Resolved, package.Version, package.Digest, ExtensionHashes.Of(package.Contract), package.Signature?.Publisher ?? "vendored")]).Format());

    public ExtensionService Service(IPublisherPrompt? prompt = null, IExtensionRegistry? source = null)
    {
        var trust = new ExtensionTrust(ExtensionAllowList.Load(Root), Root, prompt ?? new FixedPublisherPrompt());
        var packages = new ExtensionPackages(Registry, source, new VendoredExtensionStore(Path.Combine(Model, "extensions")), trust);
        return new(packages, Cache);
    }

    public ExtensionWorkspace Workspace() => ExtensionWorkspace.Load(Model);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, true);
        }
        catch (IOException)
        {
        }
    }
}
