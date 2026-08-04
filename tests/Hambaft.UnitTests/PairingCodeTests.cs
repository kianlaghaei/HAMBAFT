using FluentAssertions;
using Hambaft.Infrastructure.Security;

namespace Hambaft.UnitTests;

public sealed class PairingCodeTests
{
    [Fact]
    public void Pairing_code_is_returned_raw_but_only_hash_material_is_persistable()
    {
        var generator=new PairingCodeGenerator();var material=generator.Generate();
        material.RawCode.Should().HaveLength(8);material.Hash.Should().NotContain(material.RawCode);generator.Verify(material.RawCode,material.Hash).Should().BeTrue();generator.Verify("WRONG234",material.Hash).Should().BeFalse();
    }

    [Fact]
    public void Pairing_codes_are_securely_unique()
    {
        var generator=new PairingCodeGenerator();Enumerable.Range(0,100).Select(_=>generator.Generate().RawCode).Distinct().Should().HaveCount(100);
    }
}
