using Xunit;

namespace REWApi.Tests.CSharp;

/// <summary>
/// Collection definition for REW Integration Tests.
/// This ensures all REW tests run SEQUENTIALLY, not in parallel,
/// because they share the same REW instance and baseline data.
/// </summary>
[CollectionDefinition("REW Integration Tests", DisableParallelization = true)]
public class REWTestCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
