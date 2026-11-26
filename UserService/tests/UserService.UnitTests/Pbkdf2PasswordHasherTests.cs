using UserService.Infrastructure.Security;

namespace UserService.UnitTests;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("secret-pass");

        Assert.True(_hasher.Verify("secret-pass", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("secret-pass");

        Assert.False(_hasher.Verify("other-pass", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Соль случайная: одинаковые пароли не дают одинаковый хеш
        Assert.NotEqual(_hasher.Hash("secret-pass"), _hasher.Hash("secret-pass"));
    }

    [Fact]
    public void Hash_FitsIntoPasswordHashColumn()
    {
        Assert.True(_hasher.Hash("secret-pass").Length <= 256);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("v1.abc.def.ghi")]
    [InlineData("v1.1000.%%%.%%%")]
    // старый формат — SHA-256 без соли
    [InlineData("A665A45920422F9D417E4867EFDC4FB8A04A1F3FFF1FA07E998E86F7F7A27AE3")]
    public void Verify_MalformedOrLegacyHash_ReturnsFalse(string hash)
    {
        Assert.False(_hasher.Verify("123", hash));
    }
}
